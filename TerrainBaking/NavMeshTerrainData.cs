using Unity.Entities;
using UnityEngine;

namespace TerrainBaking
{
    public class NavMeshTerrainData : IComponentData
    {
        public TerrainData data;
    }
}