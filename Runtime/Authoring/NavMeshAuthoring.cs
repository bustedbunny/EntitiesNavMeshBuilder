using EntitiesNavMeshBuilder.Data;
using Unity.Entities;
using UnityEngine;

namespace EntitiesNavMeshBuilder.Authoring
{
    public class NavMeshAuthoring : MonoBehaviour
    {
        public class NavMeshBaker : Baker<NavMeshAuthoring>
        {
            public override void Bake(NavMeshAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new NavMeshPart());
            }
        }
    }
}