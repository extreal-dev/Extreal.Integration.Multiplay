using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that handles NetworkManager as a server.
    /// </summary>
    public class NgoServer : INgoServer
    {
        /// <inheritdoc/>
        public bool IsRunning => networkManager != null && networkManager.IsServer;

        /// <inheritdoc/>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
            => IsRunning ? networkManager.ConnectedClients : null;

        /// <inheritdoc/>
        public event Action OnServerStarted;

        /// <inheritdoc/>
        public event Action OnServerStopping;

        /// <inheritdoc/>
        public event Action<ulong> OnClientConnected;

        /// <inheritdoc/>
        public event Action<ulong> OnClientDisconnecting;

        /// <inheritdoc/>
        public event Action<ulong, string> OnClientRemoving;

        private readonly NetworkManager networkManager;

        private Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoServer));

        /// <summary>
        /// Creates a new NgoServer with given networkManager.
        /// </summary>
        /// <param name="networkManager">NetworkManager to be used as a server.</param>
        /// <exception cref="ArgumentNullException">If networkManager is null.</exception>
        public NgoServer(NetworkManager networkManager)
        {
#pragma warning disable IDE0016
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }
#pragma warning restore IDE0016

            this.networkManager = networkManager;

            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                networkManager.ConnectionApprovalCallback = (_, response) => response.Approved = true;
            }
            networkManager.OnServerStarted += OnServerStartedEventHandler;
            networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectEventHandler;
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

            try
            {
                await UniTask.WaitUntil(() => networkManager.StartServer(), cancellationToken: token);
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

            OnServerStopping?.Invoke();
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <inheritdoc/>
        public void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback)
        {
            this.connectionApprovalCallback = connectionApprovalCallback;
            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                networkManager.ConnectionApprovalCallback = connectionApprovalCallback;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        public bool RemoveClient(ulong clientId, string message)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to reject client because the server is not running");
            }

            if (!networkManager.ConnectedClientsIds.Contains(clientId))
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn($"Unable to reject client with client id {clientId} because it does not exist");
                }
                return false;
            }

            OnClientRemoving?.Invoke(clientId, message);
            networkManager.DisconnectClient(clientId);

            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this server is not yet running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        public bool SendMessageToClients
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

            if (clientIds.Except(networkManager.ConnectedClientsIds).Count() != 0)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn($"{nameof(clientIds)} contains some ids that does not exist");
                }
                return false;
            }

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, clientIds, messageStream, networkDelivery);

            return true;
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

        #region Callbacks
        private void OnServerStartedEventHandler()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The server has started");
            }

            OnServerStarted?.Invoke();
        }

        private void OnClientConnectedEventHandler(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client with client id {clientId} has connected");
            }

            OnClientConnected?.Invoke(clientId);
        }

        private void OnClientDisconnectEventHandler(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client with client id {clientId} will disconnect");
            }

            OnClientDisconnecting?.Invoke(clientId);
        }
        #endregion
    }
}
