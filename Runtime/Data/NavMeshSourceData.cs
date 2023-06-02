using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshSourceData : IComponentData
    {
        public int instanceId;
        public Bounds meshBounds;
        public NavMeshBuildSourceShape shape;
    }
}