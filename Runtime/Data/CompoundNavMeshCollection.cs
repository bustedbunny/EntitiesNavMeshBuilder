using Unity.Collections;
using Unity.Entities;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Data
{
    public struct CompoundNavMeshCollection : IComponentData
    {
        public NativeList<NavMeshBuildSource> sources;
        public NativeReference<NavMeshCollectionMetadata> metadata;
    }
}