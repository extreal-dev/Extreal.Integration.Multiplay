using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.NetworkObject;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that spawns the NetworkObject.
    /// </summary>
    public static class NgoSpawnHelper
    {
        private static VisibilityDelegate checkObjectVisibility;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoSpawnHelper));

        /// <summary>
        /// Sets VisibilityDelegate.
        /// </summary>
        /// <param name="visibilityDelegate">Used as CheckObjectVisibility for the spawned NetworkObject.</param>
        public static void SetVisibilityDelegate(VisibilityDelegate visibilityDelegate)
            => checkObjectVisibility = visibilityDelegate;

        /// <summary>
        /// Spawns NetworkObject owned by the server.
        /// </summary>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
        public static GameObject SpawnWithServerOwnership
        (
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(0, prefab, position, rotation, parent, worldPositionStays, SpawnType.ServerOwnership);

        /// <summary>
        /// Spawns NetworkObject owned by the client.
        /// </summary>
        /// <param name="ownerClientId">Id of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
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

        /// <summary>
        /// Spawns NetworkObject as a player object.
        /// </summary>
        /// <param name="ownerClientId">Id of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
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
            if (!TryCreateInstance(prefab, position, rotation, parent, worldPositionStays, out var instance))
            {
                return null;
            }
            return SpawnNetworkObject(instance, spawnType, ownerClientId);
        }

        private static bool TryCreateInstance
        (
            GameObject prefab,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool worldPositionStays,
            out GameObject instance
        )
        {
            if (prefab == null)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn($"Null is accepted as {nameof(prefab)}");
                }
                instance = null;
                return false;
            }

            instance = Object.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));
            instance.transform.SetParent(parent, worldPositionStays);

            return true;
        }

        private static GameObject SpawnNetworkObject(GameObject instance, SpawnType spawnType, ulong ownerClientId = 0)
        {
            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("GameObject without NetworkObject cannot be spawned");
                }
                Object.Destroy(instance);
                return null;
            }

            networkObject.CheckObjectVisibility = checkObjectVisibility ?? (_ => true);

#pragma warning disable CC0120
#pragma warning disable IDE0010
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
            }
#pragma warning restore IDE0010
#pragma warning restore CC0120

            return instance;
        }

        private enum SpawnType
        {
            ServerOwnership,
            ClientOwnership,
            PlayerObject
        }
    }
}
