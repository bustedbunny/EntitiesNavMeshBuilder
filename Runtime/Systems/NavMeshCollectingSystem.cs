using System.Reflection;
using EntitiesNavMeshBuilder.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    [UpdateAfter(typeof(NavMeshInstanceIDInitializerSystem))]
    public partial struct NavMeshCollectorSystem : ISystem
    {
        private NativeList<NavMeshBuildSource> _list;
        private NativeList<Bounds> _boundsList;
        private int _offset;
        private EntityQuery _query;


        public void OnCreate(ref SystemState state)
        {
            _list = new(1000, Allocator.Persistent);
            _boundsList = new(1000, Allocator.Persistent);

            state.EntityManager.CreateSingleton<NavMeshCollection>();
            SystemAPI.SetSingleton(new NavMeshCollection { list = _list });

            // need to obtain offset to have burst compatible way to inject instance id
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fieldInfo = typeof(NavMeshBuildSource).GetField("m_InstanceID", flags);
            _offset = UnsafeUtility.GetFieldOffset(fieldInfo);

            _query = SystemAPI.QueryBuilder().WithAll<NavMeshSourceData, LocalToWorld>().Build();
            _query.AddChangedVersionFilter(typeof(NavMeshSourceData));
            _query.AddChangedVersionFilter(typeof(LocalToWorld));
        }

        public void OnDestroy(ref SystemState state)
        {
            _list.Dispose();
        }

        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            if (_query.IsEmpty)
            {
                return;
            }

            ref var navMeshCollection = ref SystemAPI.GetSingletonRW<NavMeshCollection>().ValueRW;

            _list.Clear();
            _boundsList.Clear();

            foreach (var (id, ltw) in SystemAPI.Query<NavMeshSourceData, LocalToWorld>().WithAll<NavMeshPart>())
            {
                var buildSource = new NavMeshBuildSource
                {
                    transform = ltw.Value,
                    size = id.meshBounds.size,
                    shape = id.shape,
                };

                var dst = (byte*)UnsafeUtility.AddressOf(ref buildSource) + _offset;
                *(int*)dst = id.instanceId;
                _list.Add(buildSource);
                _boundsList.Add(id.meshBounds);
            }

            navMeshCollection.version = state.GlobalSystemVersion;
            navMeshCollection.worldBounds = CalculateWorldBounds();
        }

        private Bounds CalculateWorldBounds()
        {
            // Use the unscaled matrix for the NavMeshSurface
            var worldToLocal = math.inverse(float4x4.identity);

            var result = new Bounds();
            for (var i = 0; i < _list.Length; i++)
            {
                var src = _list[i];
                var bounds = _boundsList[i];
                var transform = (float4x4)src.transform;
                if (src.shape is NavMeshBuildSourceShape.Mesh or NavMeshBuildSourceShape.Terrain)
                {
                    result.Encapsulate(GetWorldBounds(math.mul(worldToLocal, transform), bounds));
                }
                else if (src.shape is NavMeshBuildSourceShape.Box
                         or NavMeshBuildSourceShape.Sphere
                         or NavMeshBuildSourceShape.Capsule
                         or NavMeshBuildSourceShape.ModifierBox)
                {
                    var worldBounds = GetWorldBounds(math.mul(worldToLocal, transform), new(float3.zero, src.size));
                    result.Encapsulate(worldBounds);
                }
            }

            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);
            return result;
        }

        private static Bounds GetWorldBounds(float4x4 mat, Bounds bounds)
        {
            ref var ltw = ref UnsafeUtility.As<float4x4, LocalToWorld>(ref mat);
            var worldPos = math.mul(mat, new float4(bounds.center, 1f)).xyz;
            var boundsSize = bounds.size;
            var worldSize = math.abs(ltw.Right) * boundsSize.x + math.abs(ltw.Up) * boundsSize.y +
                            math.abs(ltw.Forward) * boundsSize.z;
            return new(worldPos, worldSize);
        }
    }
}