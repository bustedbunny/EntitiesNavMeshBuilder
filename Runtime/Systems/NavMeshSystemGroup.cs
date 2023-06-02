using Unity.Entities;
using Unity.Transforms;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class NavMeshSystemGroup : ComponentSystemGroup { }
}