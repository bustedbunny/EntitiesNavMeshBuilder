using EntitiesNavMeshBuilder.Data;
using EntitiesNavMeshBuilder.Utility;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Collider = Unity.Physics.Collider;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // [UpdateBefore(typeof(NavMeshCompoundCollectorSystem))]
    public partial struct ColliderNavMeshInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<PhysicsCollider, NavMeshPart, ColliderNavMeshPart>()
                .WithNone<NavMeshSourceData, CompoundNavMeshData>().Build();
            state.RequireForUpdate(_toAddQuery);
        }


        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            if (!_toAddQuery.IsEmptyIgnoreFilter)
            {
                var entities = _toAddQuery.ToEntityArray(state.WorldUpdateAllocator);
                state.EntityManager.AddComponent<NavMeshSourceData>(_toAddQuery);

                foreach (var e in entities)
                {
                    ref var data = ref SystemAPI.GetComponentRW<NavMeshSourceData>(e).ValueRW;
                    var collider = SystemAPI.GetComponent<PhysicsCollider>(e);

                    if (collider.ColliderPtr->Type is ColliderType.Compound)
                    {
                        state.EntityManager.RemoveComponent<NavMeshSourceData>(e);

                        var compound = (CompoundCollider*)collider.ColliderPtr;
                        var compoundBuffer = state.EntityManager.AddBuffer<CompoundNavMeshData>(e);

                        for (var i = 0; i < compound->Children.Length; i++)
                        {
                            ref var childAccessor = ref compound->Children[i];
                            var childData = GetData(childAccessor.Collider, in data, out var posRot);
                            compoundBuffer.Add(new()
                            {
                                value = childData,
                                pos = posRot.pos,
                                rot = posRot.rot
                            });
                        }
                    }
                    else
                    {
                        data = GetData(collider.ColliderPtr, in data, out _);
                    }
                }
            }
        }

        private static unsafe NavMeshSourceData GetData(Collider* ptr, in NavMeshSourceData data, out PhysicsShapeGenerator.PosRot posRot)
        {
            var newData = new NavMeshSourceData();
            var shapeData = PhysicsShapeGenerator.GenerateShapeData(ptr, out newData.aabb, out posRot);
            newData.shape = shapeData.shape;
            newData.data = shapeData.size;
            newData.area = data.area;
            return newData;
        }
    }
}