using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TerrainBaking
{
    public class TerrainMesh : IComponentData
    {
        public TerrainData data;
    }

    // [UpdateInGroup(typeof(PresentationSystemGroup))]
    // [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ClientSimulation |
    //                    WorldSystemFilterFlags.LocalSimulation)]
    // public partial class DrawTerrainMeshSystem : SystemBase
    // {
    //     private Material _material;
    //
    //     protected override void OnCreate()
    //     {
    //         _material = Resources.Load<Material>("New Material");
    //     }
    //
    //     protected override void OnUpdate()
    //     {
    //         foreach (var (terrainMesh, ltw) in SystemAPI.Query<TerrainMesh, LocalToWorld>())
    //         {
    //             Graphics.DrawMesh(terrainMesh.mesh, ltw.Value, _material, 0);
    //         }
    //     }
    // }
}