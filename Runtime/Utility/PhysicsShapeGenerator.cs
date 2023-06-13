using System;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.AI;
using BoxCollider = Unity.Physics.BoxCollider;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace EntitiesNavMeshBuilder.Utility
{
    public static unsafe class PhysicsShapeGenerator
    {
        // box
        // capsule
        // mesh
        // terrain
        // sphere
        // modifier box
        public struct PosRot
        {
            public float3 pos;
            public quaternion rot;
        }

        public static NavMeshBuildSource GenerateShapeData(Collider* collider, out Aabb bounds, out PosRot transform)
        {
            switch (collider->Type)
            {
                case ColliderType.Box:
                    return Box((BoxCollider*)collider, out bounds, out transform);
                case ColliderType.Sphere:
                    return Sphere((SphereCollider*)collider, out bounds, out transform);
                case ColliderType.Capsule:
                    return Capsule((CapsuleCollider*)collider, out bounds, out transform);
                default:
                    throw new InvalidOperationException($"{collider->Type} is not supported.");
            }
        }

        private static NavMeshBuildSource Box(BoxCollider* collider, out Aabb bounds, out PosRot posRot)
        {
            bounds = collider->CalculateAabb();
            posRot.pos = collider->Center;
            posRot.rot = collider->Orientation;
            return new()
            {
                size = collider->Size,
                shape = NavMeshBuildSourceShape.Box,
            };
        }

        private static NavMeshBuildSource Sphere(SphereCollider* collider, out Aabb bounds, out PosRot posRot)
        {
            bounds = collider->CalculateAabb();
            var diameter = collider->Radius * 2f;
            posRot.pos = collider->Center;
            posRot.rot = quaternion.identity;
            return new()
            {
                size = new float3(diameter),
                shape = NavMeshBuildSourceShape.Sphere,
            };
        }

        private static NavMeshBuildSource Capsule(CapsuleCollider* collider, out Aabb bounds, out PosRot posRot)
        {
            var diameter = collider->Radius * 2f;
            var height = math.distance(collider->Vertex0, collider->Vertex1) + diameter;
            var size = new float3(diameter, height, 0f);
            var capsuleDirection = math.normalize(collider->Vertex1 - collider->Vertex0);
            posRot.pos = collider->Vertex0 + capsuleDirection * height / 2f;
            posRot.rot = quaternion.LookRotation(capsuleDirection, math.up());
            bounds = collider->CalculateAabb();
            return new()
            {
                size = size,
                shape = NavMeshBuildSourceShape.Capsule,
            };
        }
    }
}