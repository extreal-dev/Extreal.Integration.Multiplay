using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using static Extreal.Integration.Multiplay.NGO.INetcodeServer;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public class NetcodeServer : INetcodeServer
    {
        public bool IsRunning => networkManager != null && networkManager.IsServer;

        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
        {
            get
            {
                if (networkManager != null)
                {
                    return networkManager.ConnectedClients;
                }
                return null;
            }
        }

        public event Action OnServerStarted;
        public event Action OnBeforeStopServer;

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        public event Action<ulong> OnFirstClientConnected;
        public event Action<ulong> OnLastClientDisconnected;

        public event Action<ulong, string> OnBeforeRejectClient;

        private readonly NetworkManager networkManager;
        private readonly INetworkTransportInitializer networkTransportInitializer;

        private ConnectionApprovalDelegate connectionApproval;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NetcodeServer));

        public NetcodeServer
        (
            NetworkManager networkManager,
            NetworkTransport networkTransport,
            NetcodeConfig netcodeConfig,
            INetworkTransportInitializer networkTransportInitializer
        )
        {
            this.networkManager = networkManager;
            this.networkTransportInitializer = networkTransportInitializer;

            var networkConfig = networkManager.NetworkConfig;
            networkConfig.NetworkTransport = networkTransport;
            networkConfig.ConnectionApproval = netcodeConfig.ConnectionApproval;
            networkConfig.EnableSceneManagement = netcodeConfig.EnableSceneManagement;
            networkConfig.ForceSamePrefabs = netcodeConfig.ForceSamePrefabs;
            networkConfig.RecycleNetworkIds = netcodeConfig.RecycleNetworkIds;

            networkManager.OnServerStarted += OnServerStartedEventHandler;
            networkManager.ConnectionApprovalCallback += ConnectionApprovalEventHandler;
            networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectEventHandler;
        }

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NetcodeServer)}");
            }

            StopServer();
            networkManager.OnServerStarted -= OnServerStartedEventHandler;
            networkManager.ConnectionApprovalCallback -= ConnectionApprovalEventHandler;
            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectEventHandler;
            GC.SuppressFinalize(this);
        }

        public async UniTask<bool> StartServer(ConnectionConfig connectionConfig, CancellationToken token = default)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("This server is already running");
            }

            if (connectionConfig is null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            networkTransportInitializer.Initialize(networkManager, connectionConfig);
            _ = networkManager.StartServer();

            try
            {
                await UniTask.WaitUntil(() => IsRunning, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("The operation to start server cancelled");
            }
            catch (Exception e)
            {
                throw new Exception("An unexpected error occurred during the connection", e);
            }

            return true;
        }

        public void StopServer()
        {
            if (IsRunning)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("The server will stop");
                }

                OnBeforeStopServer?.Invoke();
                networkManager.Shutdown();
            }
            else
            {
                throw new InvalidOperationException("Unable to stop server because it is not running");
            }
        }

        public void SetConnectionApproval(ConnectionApprovalDelegate connectionApproval)
            => this.connectionApproval = connectionApproval;

        public async UniTask RejectClient(ulong clientId, string message)
        {
            if (IsRunning)
            {
                OnBeforeRejectClient?.Invoke(clientId, message);
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                networkManager.DisconnectClient(clientId);
            }
            else
            {
                throw new InvalidOperationException("Unable to reject client because the server is not running");
            }
        }

        public void SendMessageToClient
        (
            string messageName,
            ulong clientId,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (IsRunning)
            {
                networkManager.CustomMessagingManager.SendNamedMessage(messageName, clientId, messageStream, networkDelivery);
            }
            else
            {
                throw new InvalidOperationException("Unable to send message because the server is not running");
            }
        }

        public void SendMessageToAllClients
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (IsRunning)
            {
                networkManager.CustomMessagingManager.SendNamedMessageToAll(messageName, messageStream, networkDelivery);
            }
            else
            {
                throw new InvalidOperationException("Unable to send message because the server is not running");
            }
        }

        public void SendMessageToAllClientsExcept
        (
            string messageName,
            ulong clientIdToIgnore,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (IsRunning)
            {
                var clientIds = networkManager.ConnectedClientsIds.Where(id => id != clientIdToIgnore).ToList();
                networkManager.CustomMessagingManager.SendNamedMessage(messageName, clientIds, messageStream, networkDelivery);
            }
            else
            {
                throw new InvalidOperationException("Unable to send message because the server is not running");
            }
        }

        public void RegisterNamedMessage(string messageName, HandleNamedMessageDelegate namedMessageHandler)
        {
            if (IsRunning)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, namedMessageHandler);
            }
            else
            {
                throw new InvalidOperationException("Unable to register named message handler because server is not running");
            }
        }

        public void UnregisterNamedMessage(string messageName)
        {
            if (IsRunning)
            {
                networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
            }
            else
            {
                throw new InvalidOperationException("Unable to unregister named message handler because server is not running");
            }
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

        private void ConnectionApprovalEventHandler(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
        {
            var approved = true;
            if (connectionApproval != null)
            {
                approved = connectionApproval.Invoke(request.ClientNetworkId, request.Payload);
            }

            response.Approved = approved;
        }

        private void OnClientConnectedEventHandler(ulong clientId)
        {
            if (ConnectedClients.Count == 1)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The first client with client id {clientId} has connected");
                }

                OnFirstClientConnected?.Invoke(clientId);
            }
            else if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client with client id {clientId} has connected");
            }

            OnClientConnected?.Invoke(clientId);
        }

        private void OnClientDisconnectEventHandler(ulong clientId)
        {
            if (Logger.IsDebug() && ConnectedClients.Count != 1)
            {
                Logger.LogDebug($"The client with client id {clientId} will disconnect");
            }

            OnClientDisconnected?.Invoke(clientId);

            if (ConnectedClients.Count == 1)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The last client with client id {clientId} will disconnect");
                }

                OnLastClientDisconnected?.Invoke(clientId);
            }
        }
        #endregion
    }
}
