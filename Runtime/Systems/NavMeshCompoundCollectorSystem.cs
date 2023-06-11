// using EntitiesNavMeshBuilder.Data;
// using Unity.Burst;
// using Unity.Burst.Intrinsics;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace EntitiesNavMeshBuilder.Systems
// {
//     public partial struct NavMeshCompoundCollectorSystem : ISystem
//     {
//         private NativeReference<bool> _shouldRebuild;
//         private EntityQuery _checkQuery;
//
//         public void OnCreate(ref SystemState state)
//         {
//             _checkQuery = SystemAPI.QueryBuilder().WithAll<CompoundNavMeshData>().Build();
//             _checkQuery.AddChangedVersionFilter(typeof(CompoundNavMeshData));
//             _shouldRebuild = new(Allocator.Persistent);
//         }
//
//         public void OnDestroy(ref SystemState state)
//         {
//             _shouldRebuild.Dispose();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             state.Dependency = new CheckJob
//             {
//                 compoundTh = SystemAPI.GetBufferTypeHandle<CompoundNavMeshData>(true),
//                 lastSystemVersion = state.LastSystemVersion,
//                 shouldRebuild = _shouldRebuild
//             }.Schedule(_checkQuery, state.Dependency);
//         }
//
//         [BurstCompile]
//         private struct CheckJob : IJobChunk
//         {
//             [ReadOnly] public BufferTypeHandle<CompoundNavMeshData> compoundTh;
//             public uint lastSystemVersion;
//
//             public NativeReference<bool> shouldRebuild;
//
//             public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
//                 in v128 chunkEnabledMask)
//             {
//                 if (chunk.DidChange(ref compoundTh, lastSystemVersion))
//                 {
//                     shouldRebuild.Value = true;
//                 }
//             }
//         }
//     }
// }

