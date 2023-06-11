using Unity.Entities;
using Unity.Mathematics;

namespace EntitiesNavMeshBuilder.Data
{
    [InternalBufferCapacity(5)]
    public struct CompoundNavMeshData : IBufferElementData
    {
        public NavMeshSourceData value;
        public float3 pos;
        public quaternion rot;
    }
}