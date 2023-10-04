using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshSourceData : IComponentData
    {
        public NavMeshBuildSourceShape shape;
        public Aabb aabb;

        // InstanceId / size
        public float3 data;

        public int area;

        public int InstanceId
        {
            get => UnsafeUtility.As<float, int>(ref data.x);
            set
            {
                ref var instanceId = ref UnsafeUtility.As<float, int>(ref data.x);
                instanceId = value;
            }
        }
    }
}