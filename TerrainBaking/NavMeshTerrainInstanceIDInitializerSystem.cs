using EntitiesNavMeshBuilder.Data;
using EntitiesNavMeshBuilder.Systems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.AI;

namespace TerrainBaking
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    [UpdateBefore(typeof(NavMeshCollectorSystem))]
    public partial struct NavMeshTerrainInstanceIDInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<NavMeshPart, NavMeshTerrainData>().WithNone<NavMeshSourceData>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_toAddQuery.IsEmptyIgnoreFilter)
            {
                state.EntityManager.AddComponent<NavMeshSourceData>(_toAddQuery);
            }

            foreach (var (idRef, terrain, link, lt, ltwRef) in SystemAPI
                         .Query<RefRW<NavMeshSourceData>, NavMeshTerrainData, CompanionLink, LocalTransform,
                             RefRW<LocalToWorld>>()
                         .WithChangeFilter<NavMeshTerrainData>())
            {
                var terrainData = terrain.data;
                var terrainId = terrainData.GetInstanceID();
                ref var id = ref idRef.ValueRW;
                id.instanceId = terrainId;
                id.meshBounds = terrainData.bounds;
                id.shape = NavMeshBuildSourceShape.Terrain;

                link.Companion.transform.SetLocalPositionAndRotation(lt.Position, lt.Rotation);
                link.Companion.transform.localScale = new(lt.Scale, lt.Scale, lt.Scale);

                ltwRef.ValueRW.Value = lt.ToMatrix();
            }
        }
    }
}