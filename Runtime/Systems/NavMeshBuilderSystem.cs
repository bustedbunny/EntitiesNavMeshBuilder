using System.Collections.Generic;
using EntitiesNavMeshBuilder.Data;
using EntitiesNavMeshBuilder.Utility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace EntitiesNavMeshBuilder.Systems
{
    [UpdateInGroup(typeof(NavMeshSystemGroup))]
    public unsafe partial class NavMeshBuilderSystem : SystemBase
    {
        private readonly List<NavMeshBuildSource> _sourceList = new(1000);
        private NavMeshData _navMeshData;
        private NavMeshDataInstance _instance;

        // optimizations
        private uint _globalVersion;
        private NativeArray<uint> _versions;
        private AsyncOperation[] _operations;


        private NativeArray<NavMeshBuildSettings> _settingsArray;
        private NavMeshData[] _dataArray;
        private NativeArray<NavMeshDataInstance> _instanceArray;

        protected override void OnStartRunning()
        {
            var settingsCount = NavMesh.GetSettingsCount();
            _settingsArray = new(settingsCount, Allocator.Persistent);
            _instanceArray = new(settingsCount, Allocator.Persistent);
            _versions = new(settingsCount, Allocator.Persistent);
            _dataArray = new NavMeshData[settingsCount];
            _operations = new AsyncOperation[settingsCount];
            for (var i = 0; i < _settingsArray.Length; i++)
            {
                _settingsArray[i] = NavMesh.GetSettingsByIndex(i);
                _dataArray[i] = NavMeshBuilder.BuildNavMeshData(_settingsArray[i], _sourceList, default,
                    float3.zero,
                    quaternion.identity);
                _instance = NavMesh.AddNavMeshData(_dataArray[i], float3.zero, quaternion.identity);
            }
        }

        protected override void OnDestroy()
        {
            if (_dataArray is not null)
            {
                foreach (var navMeshData in _dataArray)
                {
                    if (navMeshData != null)
                    {
                        Object.Destroy(navMeshData);
                    }
                }
            }

            _settingsArray.Dispose();
            _instanceArray.Dispose();
            _versions.Dispose();

            if (_instance.valid)
            {
                _instance.Remove();
            }
        }


        private Bounds _worldBounds;

        protected override void OnUpdate()
        {
            EntityManager.CompleteDependencyBeforeRO<NavMeshCollection>();
            EntityManager.CompleteDependencyBeforeRO<CompoundNavMeshCollection>();
            var collection = SystemAPI.GetSingleton<NavMeshCollection>();
            var compoundCollection = SystemAPI.GetSingleton<CompoundNavMeshCollection>();

            var metadata = collection.metadata.Value;
            var compoundMetadata = compoundCollection.metadata.Value;
            if (metadata.version > _globalVersion || compoundMetadata.version > _globalVersion)
            {
                _globalVersion = math.max(metadata.version, compoundMetadata.version);
                _sourceList.Clear();
                var sources = collection.sources;
                var compounds = compoundCollection.sources;
                _sourceList.AddRangeNative(sources.GetUnsafeReadOnlyPtr(), sources.Length);
                _sourceList.AddRangeNative(compounds.GetUnsafeReadOnlyPtr(), compounds.Length);

                _worldBounds = metadata.worldBounds;
                _worldBounds.Encapsulate(compoundMetadata.worldBounds);
            }

            for (var i = 0; i < _settingsArray.Length; i++)
            {
                var version = _versions[i];
                if (_globalVersion <= version)
                {
                    continue;
                }

                var operation = _operations[i];
                if (operation is not null && !operation.isDone)
                {
                    continue;
                }

                _versions[i] = metadata.version;

                var settings = _settingsArray[i];
                var data = _dataArray[i];
                _operations[i] =
                    NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, _sourceList, _worldBounds);
            }
        }
    }
}