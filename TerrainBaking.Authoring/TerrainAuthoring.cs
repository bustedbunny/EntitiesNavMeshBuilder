using Unity.Physics.Authoring;
using UnityEngine;

namespace TerrainBaking.Authoring
{
    [RequireComponent(typeof(Terrain))]
    public class TerrainAuthoring : MonoBehaviour
    {
        public PhysicsMaterialTemplate physicsTemplate;
    }
}