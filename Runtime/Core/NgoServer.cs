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
    public class NgoServer : INgoServer
    {
        public bool IsRunning => networkManager != null && networkManager.IsServer;
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
            => IsRunning ? networkManager.ConnectedClients : null;

        public event Action OnServerStarted;
        public event Action OnServerStopping;

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnecting;
        public event Action<ulong, string> OnClientRemoving;

        private readonly NetworkManager networkManager;

        private Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoServer));

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

        public void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback)
        {
            this.connectionApprovalCallback = connectionApprovalCallback;
            if (networkManager.NetworkConfig.ConnectionApproval)
            {
                networkManager.ConnectionApprovalCallback = connectionApprovalCallback;
            }
        }

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
