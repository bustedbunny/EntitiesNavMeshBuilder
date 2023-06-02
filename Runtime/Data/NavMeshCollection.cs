using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshCollection : IComponentData
    {
        public NativeList<NavMeshBuildSource> list;
        public Bounds worldBounds;
        public uint version;
    }
}