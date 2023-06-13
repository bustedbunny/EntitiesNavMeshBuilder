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
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    [UpdateBefore(typeof(NavMeshBuilderSystem))]
    public partial struct NavMeshCompoundCollectorSystem : ISystem
    {
        private EntityQuery _compoundQuery;

        private NativeReference<bool> _shouldRebuild;
        private NativeList<NavMeshBuildSource> _sources;
        private NativeReference<NavMeshCollectionMetadata> _meta;
        private NativeList<Aabb> _aabbs;

        public void OnCreate(ref SystemState state)
        {
            _compoundQuery = SystemAPI.QueryBuilder().WithAll<CompoundNavMeshData>().Build();
            _shouldRebuild = new(Allocator.Persistent);

            _sources = new(100, Allocator.Persistent);
            _aabbs = new(100, Allocator.Persistent);
            _meta = new(Allocator.Persistent);

            var singleton = state.EntityManager.CreateSingleton<CompoundNavMeshCollection>();
            SystemAPI.SetComponent(singleton, new CompoundNavMeshCollection
            {
                sources = _sources,
                metadata = _meta
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            _shouldRebuild.Dispose();
            _sources.Dispose();
            _meta.Dispose();
            _aabbs.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<CompoundNavMeshCollection>();

            state.Dependency = new InitJob
            {
                shouldChange = _shouldRebuild,
                data = _sources,
                aabbs = _aabbs
            }.Schedule(state.Dependency);

            state.Dependency = new CheckJob
            {
                compoundTh = SystemAPI.GetBufferTypeHandle<CompoundNavMeshData>(true),
                ltwTh = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                lastSystemVersion = state.LastSystemVersion,
                shouldRebuild = _shouldRebuild
            }.Schedule(_compoundQuery, state.Dependency);

            state.Dependency = new CollectJob
            {
                shouldRebuild = _shouldRebuild,
                compoundTh = SystemAPI.GetBufferTypeHandle<CompoundNavMeshData>(true),
                ltwTh = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                sources = _sources,
                aabbs = _aabbs
            }.Schedule(_compoundQuery, state.Dependency);

            state.Dependency = new CalculateWorldBoundsJob
            {
                metadata = _meta,
                sources = _sources,
                aabbs = _aabbs,
                systemVersion = state.GlobalSystemVersion,
                changeTracker = _shouldRebuild
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct InitJob : IJob
        {
            public NativeReference<bool> shouldChange;
            public NativeList<NavMeshBuildSource> data;
            public NativeList<Aabb> aabbs;

            public void Execute()
            {
                data.Clear();
                aabbs.Clear();
                shouldChange.Value = false;
            }
        }

        [BurstCompile]
        private struct CheckJob : IJobChunk
        {
            [ReadOnly] public BufferTypeHandle<CompoundNavMeshData> compoundTh;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> ltwTh;
            public uint lastSystemVersion;

            [NativeDisableParallelForRestriction] public NativeReference<bool> shouldRebuild;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                if (chunk.DidChange(ref compoundTh, lastSystemVersion) || chunk.DidChange(ref ltwTh, lastSystemVersion))
                {
                    shouldRebuild.Value = true;
                }
            }
        }

        [BurstCompile]
        private unsafe struct CollectJob : IJobChunk
        {
            [ReadOnly] public NativeReference<bool> shouldRebuild;

            [ReadOnly] public BufferTypeHandle<CompoundNavMeshData> compoundTh;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> ltwTh;


            [NativeDisableParallelForRestriction] public NativeList<NavMeshBuildSource> sources;
            [NativeDisableParallelForRestriction] public NativeList<Aabb> aabbs;

            private static readonly float3 One = new(1f, 1f, 1f);

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                if (!shouldRebuild.Value)
                {
                    return;
                }

                var buffers = chunk.GetBufferAccessor(ref compoundTh);
                var ltws = (LocalToWorld*)chunk.GetNativeArray(ref ltwTh).GetUnsafeReadOnlyPtr();


                for (var i = 0; i < chunk.Count; i++)
                {
                    var compounds = buffers[i].AsNativeArray();
                    var compoundsPtr = (CompoundNavMeshData*)compounds.GetUnsafeReadOnlyPtr();
                    ref var ltw = ref ltws[i];

                    FillData(ref ltw, compounds.Length, compoundsPtr);
                }
            }

            private void FillData(ref LocalToWorld ltw, int length, CompoundNavMeshData* data)
            {
                var sourcesTmp = stackalloc NavMeshBuildSource[length];
                var aabbsTmp = stackalloc Aabb[length];

                for (var i = 0; i < length; i++)
                {
                    ref var compound = ref data[i];

                    var compoundTf = float4x4.TRS(compound.pos, compound.rot, One);
                    var unscaledLtw = float4x4.TRS(ltw.Position, ltw.Rotation, One);

                    sourcesTmp[i] = new()
                    {
                        transform = math.mul(unscaledLtw, compoundTf),
                        size = compound.value.data,
                        shape = compound.value.shape,
                    };
                    aabbsTmp[i] = compound.value.aabb;
                }

                sources.AddRange(sourcesTmp, length);
                aabbs.AddRange(aabbsTmp, length);
            }
        }
    }
}