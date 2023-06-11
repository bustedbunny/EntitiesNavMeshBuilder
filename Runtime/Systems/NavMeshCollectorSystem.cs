using System.Reflection;
using EntitiesNavMeshBuilder.Data;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    [UpdateAfter(typeof(MeshNavMeshInitializerSystem))]
    public partial struct NavMeshCollectorSystem : ISystem
    {
        private NativeList<NavMeshBuildSource> _list;
        private NativeList<Aabb> _boundsList;
        private int _offset;
        private EntityQuery _query;
        private NativeReference<NavMeshCollectionData> _collectionData;
        private NativeReference<bool> _changeTracker;


        public void OnCreate(ref SystemState state)
        {
            _list = new(1000, Allocator.Persistent);
            _collectionData = new(Allocator.Persistent);
            _boundsList = new(1000, Allocator.Persistent);
            _changeTracker = new(Allocator.Persistent);

            state.EntityManager.CreateSingleton<NavMeshCollection>();
            SystemAPI.SetSingleton(new NavMeshCollection
            {
                sources = _list,
                data = _collectionData
            });

            // need to obtain offset to have burst compatible way to inject instance id
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fieldInfo = typeof(NavMeshBuildSource).GetField("m_InstanceID", flags);
            _offset = UnsafeUtility.GetFieldOffset(fieldInfo);

            _query = SystemAPI.QueryBuilder().WithAll<NavMeshSourceData, LocalToWorld>().Build();
            _query.AddChangedVersionFilter(typeof(NavMeshSourceData));
            _query.AddChangedVersionFilter(typeof(LocalToWorld));
            state.RequireForUpdate(_query);
        }

        public void OnDestroy(ref SystemState state)
        {
            _list.Dispose();
            _collectionData.Dispose();
            _boundsList.Dispose();
            _changeTracker.Dispose();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<NavMeshCollection>();

            var count = _query.CalculateEntityCountWithoutFiltering();

            state.Dependency = new ResizeJob
            {
                list1 = _list,
                list2 = _boundsList,
                count = count,
                changeTracker = _changeTracker
            }.Schedule(state.Dependency);


            var firstEntityIndices =
                _query.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, state.Dependency, out var dep);
            state.Dependency = new CollectSourcesJob
            {
                dataTh = SystemAPI.GetComponentTypeHandle<NavMeshSourceData>(true),
                ltwTh = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                firstIndices = firstEntityIndices,
                sources = _list,
                aabbs = _boundsList,
                idOffset = _offset,
                changeTracker = _changeTracker
            }.ScheduleParallel(_query, dep);

            state.Dependency = new CalculateWorldBoundsJob
            {
                collectionData = _collectionData,
                sources = _list,
                aabbs = _boundsList,
                systemVersion = state.GlobalSystemVersion,
                changeTracker = _changeTracker
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
            public NativeList<NavMeshBuildSource> list1;
            public NativeList<Aabb> list2;
            public int count;
            public NativeReference<bool> changeTracker;

            public void Execute()
            {
                // if structure changed - force update
                changeTracker.Value = list1.Length != count;

                if (list1.Capacity < count)
                {
                    list1.SetCapacity(count);
                }

                if (list2.Capacity < count)
                {
                    list1.SetCapacity(count);
                }


                list1.ResizeUninitialized(count);
                list2.ResizeUninitialized(count);
            }
        }

        [BurstCompile]
        private struct CalculateWorldBoundsJob : IJob
        {
            public NativeReference<NavMeshCollectionData> collectionData;
            [ReadOnly] public NativeList<NavMeshBuildSource> sources;
            [ReadOnly] public NativeList<Aabb> aabbs;
            public uint systemVersion;
            [ReadOnly] public NativeReference<bool> changeTracker;

            public void Execute()
            {
                if (!changeTracker.Value)
                {
                    return;
                }

                collectionData.Value = new()
                {
                    version = systemVersion,
                    worldBounds = CalculateWorldBounds()
                };
            }

            private Bounds CalculateWorldBounds()
            {
                var result = GetWorldBounds(sources[0].transform, aabbs[0]);

                for (var i = 1; i < sources.Length; i++)
                {
                    result.Include(GetWorldBounds(sources[i].transform, aabbs[i]));
                }

                // Inflate the bounds a bit to avoid clipping co-planar sources
                result.Expand(0.1f);

                var bounds = default(Bounds);
                bounds.SetMinMax(result.Min, result.Max);
                return bounds;
            }

            private static Aabb GetWorldBounds(float4x4 mat, Aabb bounds)
            {
                ref var ltw = ref UnsafeUtility.As<float4x4, LocalToWorld>(ref mat);
                var rot = ltw.Rotation;

                var halfExtentsInA = bounds.Extents * 0.5f;

                var x = math.rotate(rot, new(halfExtentsInA.x, 0, 0));
                var y = math.rotate(rot, new(0, halfExtentsInA.y, 0));
                var z = math.rotate(rot, new(0, 0, halfExtentsInA.z));

                var halfExtentsInB = math.abs(x) + math.abs(y) + math.abs(z);
                var centerInB = math.transform(mat, bounds.Center);

                return new()
                {
                    Min = centerInB - halfExtentsInB,
                    Max = centerInB + halfExtentsInB
                };
            }
        }

        [BurstCompile]
        private unsafe struct CollectSourcesJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<NavMeshSourceData> dataTh;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> ltwTh;

            [ReadOnly] public NativeArray<int> firstIndices;

            [NativeDisableParallelForRestriction] public NativeList<NavMeshBuildSource> sources;
            [NativeDisableParallelForRestriction] public NativeList<Aabb> aabbs;

            public int idOffset;
            [NativeDisableParallelForRestriction] public NativeReference<bool> changeTracker;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                changeTracker.Value = true;

                var dataPtr = (NavMeshSourceData*)chunk.GetNativeArray(ref dataTh).GetUnsafeReadOnlyPtr();
                var ltwPtr = (LocalToWorld*)chunk.GetNativeArray(ref ltwTh).GetUnsafeReadOnlyPtr();

                var firstIndex = firstIndices[unfilteredChunkIndex];
                var sourcesPtr = sources.GetUnsafePtr() + firstIndex;
                var aabbsPtr = aabbs.GetUnsafePtr() + firstIndex;

                if (chunk.Has<ColliderNavMeshPart>())
                {
                    for (var i = 0; i < chunk.Count; i++)
                    {
                        ref var data = ref dataPtr[i];
                        ref var ltw = ref ltwPtr[i];

                        sourcesPtr[i] = new()
                        {
                            shape = data.shape,
                            transform = float4x4.TRS(ltw.Position, ltw.Rotation, new(1f, 1f, 1f)),
                            size = data.data
                        };

                        aabbsPtr[i] = data.aabb;
                    }
                }
                else
                {
                    for (var i = 0; i < chunk.Count; i++)
                    {
                        ref var data = ref dataPtr[i];
                        ref var ltw = ref ltwPtr[i];
                        var navMeshBuildSource = new NavMeshBuildSource
                        {
                            shape = data.shape,
                            transform = ltw.Value,
                        };
                        var dst = (byte*)UnsafeUtility.AddressOf(ref navMeshBuildSource) + idOffset;
                        *(int*)dst = UnsafeUtility.As<float, int>(ref data.data.x);
                        sourcesPtr[i] = navMeshBuildSource;

                        aabbsPtr[i] = new()
                        {
                            Min = math.transform(ltw.Value, data.aabb.Min),
                            Max = math.transform(ltw.Value, data.aabb.Max),
                        };
                    }
                }
            }
        }
    }
}