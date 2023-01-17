using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;
using static Unity.Netcode.NetworkObject;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that handles NetworkManager as a server.
    /// </summary>
    public class NgoServer : IDisposable
    {
        /// <summary>
        /// Invokes immediately after this server starts.
        /// </summary>
        public IObservable<Unit> OnServerStarted => onServerStarted;
        private readonly Subject<Unit> onServerStarted = new Subject<Unit>();

        /// <summary>
        /// Invokes just before this server stops.
        /// </summary>
        public IObservable<Unit> OnServerStopping => onServerStopping;
        private readonly Subject<Unit> onServerStopping = new Subject<Unit>();

        /// <summary>
        /// <para>Invokes immediately after the client connects to this server.</para>
        /// Arg: ID of the connected client
        /// </summary>
        public IObservable<ulong> OnClientConnected => onClientConnected;
        private readonly Subject<ulong> onClientConnected = new Subject<ulong>();

        /// <summary>
        /// <para>Invokes just before the client disconnects from this server.</para>
        /// Arg: ID of the disconnecting client
        /// </summary>
        public IObservable<ulong> OnClientDisconnecting => onClientDisconnecting;
        private readonly Subject<ulong> onClientDisconnecting = new Subject<ulong>();

        /// <summary>
        /// <para>Invokes just before removing the client.</para>
        /// Arg: ID of the removing client
        /// </summary>
        public IObservable<ulong> OnClientRemoving => onClientRemoving;
        private readonly Subject<ulong> onClientRemoving
            = new Subject<ulong>();

        /// <summary>
        /// Gets information on connected clients.
        /// </summary>
        /// <value>
        /// <para>Dictionary of the connected clients.</para>
        /// Key: Client id, Value: Client information
        /// </value>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
            => networkManager.IsServer ? networkManager.ConnectedClients : emptyConnectedClients;
        private readonly Dictionary<ulong, NetworkClient> emptyConnectedClients = new Dictionary<ulong, NetworkClient>();

        private readonly NetworkManager networkManager;

        private VisibilityDelegate checkObjectVisibility;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoServer));

        /// <summary>
        /// Creates a new NgoServer with given networkManager.
        /// </summary>
        /// <param name="networkManager">NetworkManager to be used as a server.</param>
        /// <exception cref="ArgumentNullException">If 'networkManager' is null.</exception>
        public NgoServer(NetworkManager networkManager)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }

            this.networkManager = networkManager;

            this.networkManager.OnServerStarted += OnServerStartedEventHandler;
            this.networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            this.networkManager.OnClientDisconnectCallback += OnClientDisconnectEventHandler;
        }

        /// <summary>
        /// Finalizes NgoServer.
        /// </summary>
        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NgoServer)}");
            }

            if (networkManager.IsServer)
            {
                StopServerAsync().Forget();
            }
            networkManager.ConnectionApprovalCallback = null;
            networkManager.OnServerStarted -= OnServerStartedEventHandler;
            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectEventHandler;

            onServerStarted.Dispose();
            onServerStopping.Dispose();
            onClientConnected.Dispose();
            onClientDisconnecting.Dispose();
            onClientRemoving.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously starts this server.
        /// </summary>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <exception cref="InvalidOperationException">If this server is already running.</exception>
        /// <exception cref="OperationCanceledException">If 'token' is canceled.</exception>
        /// <returns>UniTask of this method.</returns>
        public async UniTask StartServerAsync(CancellationToken token = default)
        {
            if (networkManager.IsServer)
            {
                throw new InvalidOperationException("This server is already running");
            }

            _ = networkManager.StartServer();

            try
            {
                await UniTask.WaitUntil(() => networkManager.IsListening, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                networkManager.Shutdown();
                throw new OperationCanceledException("The operation to start server was canceled");
            }
        }

        /// <summary>
        /// Asynchronously Stops this server.
        /// </summary>
        /// <returns>UniTask of this method.</returns>
        public async UniTask StopServerAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug("The server will stop");
            }

            onServerStopping.OnNext(Unit.Default);
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <summary>
        /// Sets ConnectionApprovalCallback of NetworkManager.
        /// The callback is called when the client attempts to connect to this server.
        /// </summary>
        /// <param name="connectionApprovalCallback">Callback to be set to ConnectionApprovalCallback.</param>
        public void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback)
            => networkManager.ConnectionApprovalCallback = connectionApprovalCallback;

        /// <summary>
        /// Remove the client.
        /// </summary>
        /// <param name="clientId">Id of the client to be removed.</param>
        /// <exception cref="InvalidOperationException">If this server is not running.</exception>
        /// <returns>True if the client is successfully removed, false otherwise.</returns>
        public bool RemoveClient(ulong clientId)
        {
            IfServerIsNotRunningThenThrowException();

            if (!networkManager.ConnectedClientsIds.Contains(clientId))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Unable to remove client with client id {clientId} because it does not exist");
                }
                return false;
            }

            onClientRemoving.OnNext(clientId);
            networkManager.DisconnectClient(clientId);

            return true;
        }

        /// <summary>
        /// Sends a message to the clients.
        /// </summary>
        /// <param name="clientIds">IDs of the clients to whom the message is sent.</param>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <exception cref="InvalidOperationException">If this server is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        /// <returns>True if the message is successfully sent, false otherwise.</returns>
        public void SendMessageToClients
        (
            List<ulong> clientIds,
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            IfServerIsNotRunningThenThrowException();

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }
            if (!messageStream.IsInitialized)
            {
                throw new ArgumentException($"{nameof(messageStream)} must be initialized");
            }

            var existedClientIds = new List<ulong>();
            var notExistedClientIds = new List<ulong>();
            foreach (var clientId in clientIds)
            {
                if (networkManager.ConnectedClientsIds.Contains(clientId))
                {
                    existedClientIds.Add(clientId);
                }
                else
                {
                    notExistedClientIds.Add(clientId);
                }
            }
            if (Logger.IsDebug() && notExistedClientIds.Count != 0)
            {
                Logger.LogDebug($"{nameof(clientIds)} contains some ids that does not exist: "
                                + string.Join(", ", notExistedClientIds));
            }

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, existedClientIds, messageStream, networkDelivery);
        }

        /// <summary>
        /// Sends a message to the all clients.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <exception cref="InvalidOperationException">If this server is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        public void SendMessageToAllClients
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            IfServerIsNotRunningThenThrowException();

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }
            if (!messageStream.IsInitialized)
            {
                throw new ArgumentException($"{nameof(messageStream)} must be initialized");
            }

            networkManager.CustomMessagingManager.SendNamedMessageToAll(messageName, messageStream, networkDelivery);
        }

        /// <summary>
        /// Registers a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageHandler">Message handler to be registered.</param>
        /// <exception cref="InvalidOperationException">If this server is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler)
        {
            IfServerIsNotRunningThenThrowException();

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, messageHandler);
        }

        /// <summary>
        /// Unregisters a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <exception cref="InvalidOperationException">If this server is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void UnregisterMessageHandler(string messageName)
        {
            IfServerIsNotRunningThenThrowException();

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
        }

        /// <summary>
        /// Sets VisibilityDelegate.
        /// </summary>
        /// <param name="visibilityDelegate">Used as CheckObjectVisibility for the spawned NetworkObject.</param>
        public void SetVisibilityDelegate(VisibilityDelegate visibilityDelegate)
            => checkObjectVisibility = visibilityDelegate;

        /// <summary>
        /// Spawns NetworkObject owned by the server.
        /// </summary>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
        /// <returns>Instantiated GameObject.</returns>
        public GameObject SpawnWithServerOwnership
        (
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(prefab, position, rotation, parent, worldPositionStays, SpawnType.ServerOwnership);

        /// <summary>
        /// Spawns NetworkObject owned by the client.
        /// </summary>
        /// <param name="ownerClientId">ID of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
        /// <returns>Instantiated GameObject.</returns>
        public GameObject SpawnWithClientOwnership
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(prefab, position, rotation, parent, worldPositionStays, SpawnType.ClientOwnership, ownerClientId);

        /// <summary>
        /// Spawns NetworkObject as a player object.
        /// </summary>
        /// <param name="ownerClientId">ID of client that owns the spawned NetworkObject.</param>
        /// <param name="prefab">GameObject to be spawned.</param>
        /// <param name="position">Initial position of the GameObject when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the GameObject when it is spawned.</param>
        /// <param name="parent">Parent to be set to the GameObject.</param>
        /// <param name="worldPositionStays">If true, the world position, scale and rotation retain the same values as before. Otherwise, the local ones retain.</param>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
        /// <returns>Instantiated GameObject.</returns>
        public GameObject SpawnAsPlayerObject
        (
            ulong ownerClientId,
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(prefab, position, rotation, parent, worldPositionStays, SpawnType.PlayerObject, ownerClientId);

        private GameObject SpawnInternal
        (
            GameObject prefab,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool worldPositionStays,
            SpawnType spawnType,
            ulong ownerClientId = default
        )
        {
            if (!networkManager.IsListening)
            {
                throw new InvalidOperationException("Unable to spawn objects because this server is not listening");
            }
            var networkObject = CreateInstanceAsNetworkObject(prefab, position, rotation, parent, worldPositionStays);
            return SpawnNetworkObject(networkObject, spawnType, ownerClientId);
        }

        private static NetworkObject CreateInstanceAsNetworkObject
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
                throw new ArgumentNullException(nameof(prefab));
            }

            var instance = UnityEngine.Object.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));
            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new ArgumentException("GameObject without NetworkObject cannot be spawned");
            }

            instance.transform.SetParent(parent, worldPositionStays);

            return networkObject;
        }

        private GameObject SpawnNetworkObject(NetworkObject networkObject, SpawnType spawnType, ulong ownerClientId = default)
        {
            networkObject.CheckObjectVisibility = checkObjectVisibility ?? (_ => true);

            if (spawnType == SpawnType.ServerOwnership)
            {
                networkObject.Spawn();
            }
            if (spawnType == SpawnType.ClientOwnership)
            {
                networkObject.SpawnWithOwnership(ownerClientId);
            }
            if (spawnType == SpawnType.PlayerObject)
            {
                networkObject.SpawnAsPlayerObject(ownerClientId);
            }

            return networkObject.gameObject;
        }

        private void IfServerIsNotRunningThenThrowException()
        {
            if (!networkManager.IsServer)
            {
                throw new InvalidOperationException("This server is not running");
            }
        }

        private void OnServerStartedEventHandler()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The server has started");
            }

            onServerStarted.OnNext(Unit.Default);
        }

        private void OnClientConnectedEventHandler(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client with client id {clientId} has connected");
            }

            onClientConnected.OnNext(clientId);
        }

        private void OnClientDisconnectEventHandler(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client with client id {clientId} will disconnect");
            }

            onClientDisconnecting.OnNext(clientId);
        }

        private enum SpawnType
        {
            ServerOwnership,
            ClientOwnership,
            PlayerObject
        }
    }
}
