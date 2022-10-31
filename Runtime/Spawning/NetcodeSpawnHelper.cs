using System;
using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.NetworkObject;

namespace Extreal.Integration.Multiplay.NGO
{
    public static class NetcodeSpawnHelper
    {
        private static VisibilityDelegate checkObjectVisibility;

        public static void SetVisibilityDelegate(VisibilityDelegate visibilityDelegate)
            => checkObjectVisibility = visibilityDelegate;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NetcodeSpawnHelper));

        public static GameObject SpawnWithServerOwnership
        (
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(0, prefab, position, rotation, parent, worldPositionStays, SpawnType.ServerOwnership);

        public static GameObject SpawnWithClientOwnership
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(ownerClientId, prefab, position, rotation, parent, worldPositionStays, SpawnType.ClientOwnership);

        public static GameObject SpawnAsPlayerObject
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(ownerClientId, prefab, position, rotation, parent, worldPositionStays, SpawnType.PlayerObject);

        private static GameObject SpawnInternal
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool worldPositionStays,
            SpawnType spawnType
        )
        {
            var instance = CreateInstance(prefab, position, rotation, parent, worldPositionStays);
            var isValid = TrySpawnNetworkObject(instance, spawnType, ownerClientId);
            return isValid ? instance : null;
        }

        private static GameObject CreateInstance
        (
            GameObject prefab,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool worldPositionStays
        )
        {
            if (prefab == null)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn($"Null is accepted as {nameof(prefab)}");
                }
                return null;
            }

            var go = UnityEngine.Object.Instantiate(prefab, position.GetValueOrDefault(), rotation.GetValueOrDefault());
            go.transform.SetParent(parent, worldPositionStays);

            return go;
        }

        private static bool TrySpawnNetworkObject(GameObject instance, SpawnType spawnType, ulong ownerClientId = 0)
        {
            if (instance == null)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("Instantiated object was null");
                }
                return false;
            }

            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                UnityEngine.Object.Destroy(instance);
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("GameObject without NetworkObject cannot be spawned");
                }
                return false;
            }

            networkObject.CheckObjectVisibility = checkObjectVisibility ?? (_ => true);

            switch (spawnType)
            {
                case SpawnType.ServerOwnership:
                {
                    networkObject.Spawn();
                    break;
                }
                case SpawnType.ClientOwnership:
                {
                    networkObject.SpawnWithOwnership(ownerClientId);
                    break;
                }
                case SpawnType.PlayerObject:
                {
                    networkObject.SpawnAsPlayerObject(ownerClientId);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(spawnType), "Undefined SpawnType was input");
                }
            }

            return true;
        }

        private enum SpawnType
        {
            ServerOwnership,
            ClientOwnership,
            PlayerObject
        }
    }
}
