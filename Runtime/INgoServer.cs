using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;
using static Unity.Netcode.NetworkObject;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for the mock to be used as NgoServer when testing.
    /// </summary>
    public interface INgoServer : IDisposable
    {
        /// <summary>
        /// Invokes immediately after this server starts.
        /// </summary>
        IObservable<Unit> OnServerStarted { get; }

        /// <summary>
        /// Invokes just before this server stops.
        /// </summary>
        IObservable<Unit> OnServerStopping { get; }

        /// <summary>
        /// <para>Invokes immediately after the client connects to this server.</para>
        /// Arg: ID of the connected client
        /// </summary>
        IObservable<ulong> OnClientConnected { get; }

        /// <summary>
        /// <para>Invokes just before the client disconnects from this server.</para>
        /// Arg: ID of the disconnecting client
        /// </summary>
        IObservable<ulong> OnClientDisconnecting { get; }

        /// <summary>
        /// <para>Invokes just before removing the client.</para>
        /// Arg: ID of the removing client
        /// </summary>
        IObservable<ulong> OnClientRemoving { get; }

        /// <summary>
        /// Gets if this server is running or not.
        /// </summary>
        /// <value>True if this server is running, false otherwise.</value>
        bool IsRunning { get; }

        /// <summary>
        /// Gets information on connected clients.
        /// </summary>
        /// <value>
        /// <para>Dictionary of the connected clients.</para>
        /// Key: Client id, Value: Client information
        /// </value>
        IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients { get; }

        /// <summary>
        /// Asynchronously starts this server.
        /// </summary>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <returns>UniTask of this method.</returns>
        UniTask StartServerAsync(CancellationToken token = default);

        /// <summary>
        /// Asynchronously Stops this server.
        /// </summary>
        /// <returns>UniTask of this method.</returns>
        UniTask StopServerAsync();

        /// <summary>
        /// Sets ConnectionApprovalCallback of NetworkManager.
        /// The callback is called when the client attempts to connect to this server.
        /// </summary>
        /// <param name="connectionApprovalCallback">Callback to be set to ConnectionApprovalCallback.</param>
        void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback);

        /// <summary>
        /// Remove the client.
        /// </summary>
        /// <param name="clientId">Id of the client to be removed.</param>
        /// <returns>True if the client is successfully removed, false otherwise.</returns>
        bool RemoveClient(ulong clientId);

        /// <summary>
        /// Sends a message to the clients.
        /// </summary>
        /// <param name="clientIds">IDs of the clients to whom the message is sent.</param>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <returns>True if the message is successfully sent, false otherwise.</returns>
        void SendMessageToClients(List<ulong> clientIds, string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Sends a message to the all clients.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        void SendMessageToAllClients(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Registers a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageHandler">Message handler to be registered.</param>
        void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler);

        /// <summary>
        /// Unregisters a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        void UnregisterMessageHandler(string messageName);

        /// <summary>
        /// Sets VisibilityDelegate.
        /// </summary>
        /// <param name="visibilityDelegate">Used as CheckObjectVisibility for the spawned NetworkObject.</param>
        void SetVisibilityDelegate(VisibilityDelegate visibilityDelegate);

        /// <summary>
        /// Spawns NetworkObject owned by the server.
        /// </summary>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
        GameObject SpawnWithServerOwnership
        (
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        );

        /// <summary>
        /// Spawns NetworkObject owned by the client.
        /// </summary>
        /// <param name="ownerClientId">ID of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
        GameObject SpawnWithClientOwnership
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        );

        /// <summary>
        /// Spawns NetworkObject as a player object.
        /// </summary>
        /// <param name="ownerClientId">ID of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <returns>Instantiated GameObject.</returns>
        GameObject SpawnAsPlayerObject
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        );
    }
}
