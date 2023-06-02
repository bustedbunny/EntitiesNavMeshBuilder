using EntitiesNavMeshBuilder.Data;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    [UpdateBefore(typeof(NavMeshCollectorSystem))]
    public partial struct NavMeshInstanceIDInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<MaterialMeshInfo, RenderMeshArray, NavMeshPart>().WithNone<NavMeshSourceData>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_toAddQuery.IsEmptyIgnoreFilter)
            {
                state.EntityManager.AddComponent<NavMeshSourceData>(_toAddQuery);
            }

            foreach (var (mmi, idRef, rma) in SystemAPI
                         .Query<MaterialMeshInfo, RefRW<NavMeshSourceData>, RenderMeshArray>()
                         .WithChangeFilter<MaterialMeshInfo>().WithChangeFilter<RenderMeshArray>())
            {
                var mesh = rma.GetMesh(mmi);
                var meshId = mesh.GetInstanceID();
                ref var id = ref idRef.ValueRW;
                id.instanceId = meshId;
                id.meshBounds = mesh.bounds;
                id.shape = NavMeshBuildSourceShape.Mesh;
            }
        }
    }
}