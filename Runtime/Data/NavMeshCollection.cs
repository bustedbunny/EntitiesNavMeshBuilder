using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshCollection : IComponentData
    {
        public NativeList<NavMeshBuildSource> sources;
        public NativeReference<NavMeshCollectionData> data;
    }

    public struct NavMeshCollectionData
    {
        public Bounds worldBounds;
        public uint version;
    }
}