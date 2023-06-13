using EntitiesNavMeshBuilder.Data;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // [UpdateBefore(typeof(NavMeshCollectorSystem))]
    public partial struct MeshNavMeshInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<MaterialMeshInfo, RenderMeshArray, NavMeshPart, MeshNavMeshPart>()
                .WithNone<NavMeshSourceData>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_toAddQuery.IsEmptyIgnoreFilter)
            {
                state.EntityManager.AddComponent<NavMeshSourceData>(_toAddQuery);
            }

            foreach (var (mmi, idRef, rma) in SystemAPI
                         .Query<MaterialMeshInfo, RefRW<NavMeshSourceData>, RenderMeshArray>()
                         .WithAll<MeshNavMeshPart>()
                         .WithChangeFilter<MaterialMeshInfo, RenderMeshArray>())
            {
                var mesh = rma.GetMesh(mmi);
                ref var id = ref idRef.ValueRW;
                id.InstanceId = mesh.GetInstanceID();
                var meshBounds = mesh.bounds;
                id.aabb = new()
                {
                    Min = meshBounds.min,
                    Max = meshBounds.max
                };
                id.shape = NavMeshBuildSourceShape.Mesh;
            }
        }
    }
}