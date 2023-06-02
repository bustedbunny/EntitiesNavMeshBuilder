using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Rendering;
using TerrainCollider = Unity.Physics.TerrainCollider;

namespace TerrainBaking.Authoring
{
    public class TerrainBaker : Baker<Terrain>
    {
        public override void Bake(Terrain authoring)
        {
            DependsOn(authoring.terrainData);
            var data = authoring.terrainData;

            var resolution = data.heightmapResolution;
            var size = new int2(resolution, resolution);
            float3 scale = data.heightmapScale;

            var colliderHeights = new NativeArray<float>(resolution * resolution, Allocator.Temp);
            var terrainHeights = data.GetHeights(0, 0, resolution, resolution);

            for (var j = 0; j < size.y; j++)
            {
                for (var i = 0; i < size.x; i++)
                {
                    var h = terrainHeights[i, j];
                    colliderHeights[j + i * size.x] = h;
                }
            }

            BakingUtility.AdditionalCompanionComponentTypes.Add(typeof(Terrain));
            BakingUtility.AdditionalCompanionComponentTypes.Add(typeof(UnityEngine.TerrainCollider));


            var collider = new PhysicsCollider
            {
                Value = TerrainCollider.Create(colliderHeights, size, scale, TerrainCollider.CollisionMethod.Triangles)
            };
            // collider.ColliderPtr->SetCollisionFilter(new CollisionFilter
            // {
            //     BelongsTo = 0,
            //     CollidesWith = 0,
            //     GroupIndex = 0
            // });
            AddBlobAsset(ref collider.Value, out var hash);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, collider);
            AddSharedComponent(entity, new PhysicsWorldIndex());
            AddBuffer<PhysicsColliderKeyEntityPair>(entity);


            AddComponentObject(entity, authoring);
            AddComponentObject(entity, GetComponent<UnityEngine.TerrainCollider>());
            // GenerateMesh(colliderHeights, size, scale, out var mesh);
            AddComponentObject(entity, new TerrainMesh { data = data });

            colliderHeights.Dispose();
        }

        private static void GenerateMesh(NativeArray<float> heights, int2 size, float3 scale, out Mesh mesh)
        {
            mesh = new();

            var count = (size.x * size.y + 2) * 6;

            const NativeArrayOptions arrayOptions = NativeArrayOptions.UninitializedMemory;
            var triangles = new NativeArray<float3>(count, Allocator.Temp, arrayOptions);
            var indices = new NativeArray<uint>(count, Allocator.Temp, arrayOptions);

            for (var i = 0; i < size.x - 1; i++)
            {
                for (var j = 0; j < size.y - 1; j++)
                {
                    var nextI = i + 1;
                    var nextJ = j + 1;
                    var v0 = new float3(i, heights[i + size.x * j], j) * scale;
                    var v1 = new float3(nextI, heights[nextI + size.x * j], j) * scale;
                    var v2 = new float3(i, heights[i + size.x * nextJ], nextJ) * scale;
                    var v3 = new float3(nextI, heights[nextI + size.x * nextJ], nextJ) * scale;

                    var current = 2 * (i * size.x * 3 + j * 3);
                    triangles[current] = v0;
                    triangles[current + 1] = v1;
                    triangles[current + 2] = v2;
                    indices[current] = (uint)current + 2;
                    indices[current + 1] = (uint)current + 1;
                    indices[current + 2] = (uint)current;

                    current += 3;

                    triangles[current] = v1;
                    triangles[current + 1] = v2;
                    triangles[current + 2] = v3;
                    indices[current] = (uint)current;
                    indices[current + 1] = (uint)current + 1;
                    indices[current + 2] = (uint)current + 2;
                }
            }

            var triangleDescription = new VertexAttributeDescriptor(VertexAttribute.Position);
            mesh.SetVertexBufferParams(count, triangleDescription);
            mesh.SetVertexBufferData(triangles, 0, 0, triangles.Length);

            mesh.SetIndexBufferParams(count, IndexFormat.UInt32);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Length);

            var meshDesc = new SubMeshDescriptor(0, indices.Length);
            mesh.SetSubMesh(0, meshDesc);
            mesh.RecalculateBounds();
        }
    }
}