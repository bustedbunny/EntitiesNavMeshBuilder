using EntitiesNavMeshBuilder.Data;
using EntitiesNavMeshBuilder.Utility;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // [UpdateBefore(typeof(NavMeshCollectorSystem))]
    public partial struct ColliderNavMeshInitializerSystem : ISystem
    {
        private EntityQuery _toAddQuery;

        public void OnCreate(ref SystemState state)
        {
            _toAddQuery = SystemAPI.QueryBuilder()
                .WithAll<PhysicsCollider, NavMeshPart, ColliderNavMeshPart>()
                .WithNone<NavMeshSourceData>().Build();
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

                        Debug.LogWarning($"Compound collider is not supported at this moment.");

                        // var compound = (CompoundCollider*)collider.ColliderPtr;
                        // var compoundBuffer = state.EntityManager.AddBuffer<CompoundNavMeshData>(e);
                        //
                        // for (var i = 0; i < compound->Children.Length; i++)
                        // {
                        //     ref var childAccessor = ref compound->Children[i];
                        //     var childData = GetData(childAccessor.Collider, out var posRot);
                        //
                        //     if (math.all(posRot.pos == float3.zero) && posRot.rot.Equals(quaternion.identity))
                        //     {
                        //         data = childData;
                        //     }
                        //     else
                        //     {
                        //         compoundBuffer.Add(new()
                        //         {
                        //             value = childData,
                        //             pos = posRot.pos,
                        //             rot = posRot.rot
                        //         });
                        //     }
                        // }
                    }
                    else
                    {
                        data = GetData(collider.ColliderPtr, out _);
                    }
                }
            }
        }

        private static unsafe NavMeshSourceData GetData(Collider* ptr, out PhysicsShapeGenerator.PosRot posRot)
        {
            var data = new NavMeshSourceData();
            var shapeData = PhysicsShapeGenerator.GenerateShapeData(ptr, out data.aabb, out posRot);
            data.shape = shapeData.shape;
            data.data = shapeData.size;
            return data;
        }
    }
}