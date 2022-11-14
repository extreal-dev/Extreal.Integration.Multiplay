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
    public class NgoServer : INgoServer
    {
        /// <inheritdoc/>
        public IObservable<Unit> OnServerStarted => onServerStarted;
        private readonly Subject<Unit> onServerStarted = new Subject<Unit>();

        /// <inheritdoc/>
        public IObservable<Unit> OnServerStopping => onServerStopping;
        private readonly Subject<Unit> onServerStopping = new Subject<Unit>();

        /// <inheritdoc/>
        public IObservable<ulong> OnClientConnected => onClientConnected;
        private readonly Subject<ulong> onClientConnected = new Subject<ulong>();

        /// <inheritdoc/>
        public IObservable<ulong> OnClientDisconnecting => onClientDisconnecting;
        private readonly Subject<ulong> onClientDisconnecting = new Subject<ulong>();

        /// <inheritdoc/>
        public IObservable<ulong> OnClientRemoving => onClientRemoving;
        private readonly Subject<ulong> onClientRemoving
            = new Subject<ulong>();

        /// <inheritdoc/>
        public bool IsRunning => networkManager != null && networkManager.IsServer;

        /// <inheritdoc/>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
            => IsRunning ? networkManager.ConnectedClients : null;

        private readonly NetworkManager networkManager;

        private VisibilityDelegate checkObjectVisibility;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoServer));

        /// <summary>
        /// Creates a new NgoServer with given networkManager.
        /// </summary>
        /// <param name="networkManager">NetworkManager to be used as a server.</param>
        /// <exception cref="ArgumentNullException">If networkManager is null.</exception>
        public NgoServer(NetworkManager networkManager)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }

            this.networkManager = networkManager;

            if (this.networkManager.NetworkConfig.ConnectionApproval)
            {
                this.networkManager.ConnectionApprovalCallback = (_, response) => response.Approved = true;
            }
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

            if (IsRunning)
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
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is already running.</exception>
        /// <exception cref="OperationCanceledException">If 'token' is canceled.</exception>
        public async UniTask StartServerAsync(CancellationToken token = default)
        {
            if (IsRunning)
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

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        public async UniTask StopServerAsync()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to stop server because it is not running");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug("The server will stop");
            }

            onServerStopping.OnNext(Unit.Default);
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <inheritdoc/>
        public void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback)
        {
            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                networkManager.ConnectionApprovalCallback = connectionApprovalCallback;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        public bool RemoveClient(ulong clientId)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to remove client because the server is not running");
            }

            if (!networkManager.ConnectedClientsIds.Contains(clientId))
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn($"Unable to remove client with client id {clientId} because it does not exist");
                }
                return false;
            }

            onClientRemoving.OnNext(clientId);
            networkManager.DisconnectClient(clientId);

            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        public void SendMessageToClients
        (
            List<ulong> clientIds,
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to send message because the server is not running");
            }
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
            if (Logger.IsWarn() && notExistedClientIds.Count != 0)
            {
                Logger.LogWarn($"{nameof(clientIds)} contains some ids that does not exist: "
                                + string.Join(", ", notExistedClientIds));
            }

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, existedClientIds, messageStream, networkDelivery);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        public void SendMessageToAllClients
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to send message because the server is not running");
            }
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

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to register named message handler because server is not running");
            }
            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, messageHandler);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void UnregisterMessageHandler(string messageName)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to unregister named message handler because server is not running");
            }
            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
        }

        /// <inheritdoc/>
        public void SetVisibilityDelegate(VisibilityDelegate visibilityDelegate)
            => checkObjectVisibility = visibilityDelegate;

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
        public GameObject SpawnWithServerOwnership
        (
            GameObject prefab,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null,
            bool worldPositionStays = true
        )
            => SpawnInternal(prefab, position, rotation, parent, worldPositionStays, SpawnType.ServerOwnership);

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
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

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">If 'prefab' is null.</exception>
        /// <exception cref="ArgumentException">If 'prefab' does not be attached NetworkObject component to .</exception>
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

            return networkObject.gameObject;
        }

        #region Callbacks
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
        #endregion

        private enum SpawnType
        {
            ServerOwnership,
            ClientOwnership,
            PlayerObject
        }
    }
}
