using Unity.Entities;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshPart : IComponentData { }

    public struct MeshNavMeshPart : IComponentData { }

    public struct TerrainNavMeshPart : IComponentData { }

    public struct ColliderNavMeshPart : IComponentData { }
}