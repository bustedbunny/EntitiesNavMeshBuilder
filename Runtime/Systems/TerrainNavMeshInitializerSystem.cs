using EntitiesNavMeshBuilder.Data;
using TerrainBaking;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // [UpdateBefore(typeof(NavMeshCollectorSystem))]
    public partial struct TerrainNavMeshInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<NavMeshTerrainData, NavMeshPart, TerrainNavMeshPart>()
                .WithNone<NavMeshSourceData>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_toAddQuery.IsEmptyIgnoreFilter)
            {
                state.EntityManager.AddComponent<NavMeshSourceData>(_toAddQuery);
            }

            foreach (var (terr, idRef) in SystemAPI
                         .Query<NavMeshTerrainData, RefRW<NavMeshSourceData>>()
                         .WithAll<TerrainNavMeshPart>()
                         .WithChangeFilter<NavMeshTerrainData>())
            {
                var mesh = terr.data;
                ref var id = ref idRef.ValueRW;
                id.InstanceId = mesh.GetInstanceID();
                var meshBounds = mesh.bounds;
                id.aabb = new()
                {
                    Min = meshBounds.min,
                    Max = meshBounds.max
                };
                id.shape = NavMeshBuildSourceShape.Terrain;
            }
        }
    }
}