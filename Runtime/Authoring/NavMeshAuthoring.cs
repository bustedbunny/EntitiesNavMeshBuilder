using EntitiesNavMeshBuilder.Data;
using Unity.Entities;
using UnityEngine;

namespace EntitiesNavMeshBuilder.Authoring
{
    public class NavMeshAuthoring : MonoBehaviour
    {
        public NavMeshSourceType type;
        public int area;

        public class NavMeshBaker : Baker<NavMeshAuthoring>
        {
            public override void Bake(NavMeshAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new NavMeshPart
                {
                    area = authoring.area
                });
                if (authoring.type is NavMeshSourceType.Mesh)
                {
                    AddComponent(entity, new MeshNavMeshPart());
                }
                else if (authoring.type is NavMeshSourceType.Collider)
                {
                    AddComponent(entity, new ColliderNavMeshPart());
                }
                else if (authoring.type is NavMeshSourceType.Terrain)
                {
                    AddComponent(entity, new TerrainNavMeshPart());
                }
            }
        }

        public enum NavMeshSourceType
        {
            None = 0,
            Mesh = 100,
            Collider = 200,
            Terrain = 300
        }
    }
}