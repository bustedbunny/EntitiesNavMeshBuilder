using EntitiesNavMeshBuilder.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [BurstCompile]
    public unsafe struct CalculateWorldBoundsJob : IJob
    {
        public NativeReference<NavMeshCollectionMetadata> metadata;
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

            metadata.Value = new()
            {
                version = systemVersion,
                worldBounds = CalculateWorldBounds()
            };
        }

        private Bounds CalculateWorldBounds()
        {
            var sourcesPtr = sources.GetUnsafeReadOnlyPtr();
            var aabbPtr = aabbs.GetUnsafeReadOnlyPtr();

            var result = GetWorldBounds(sourcesPtr[0].transform, ref aabbPtr[0]);

            for (var i = 1; i < sources.Length; i++)
            {
                result.Include(GetWorldBounds(sources[i].transform, ref aabbPtr[i]));
            }

            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);

            var bounds = default(Bounds);
            bounds.SetMinMax(result.Min, result.Max);
            return bounds;
        }

        private static Aabb GetWorldBounds(float4x4 mat, ref Aabb bounds)
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
}