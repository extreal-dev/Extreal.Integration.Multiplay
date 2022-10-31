using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public class NetcodeClient : INetcodeClient
    {
        public bool IsRunning => networkManager != null && networkManager.IsClient;
        public bool IsConnected => networkManager != null && networkManager.IsConnectedClient;

        public event Action<ulong> OnConnected;
        public event Action<ulong> OnDisconnected;
        public event Action OnBeforeDisconnect;

        private readonly NetworkManager networkManager;
        private readonly INetworkTransportInitializer networkTransportInitializer;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NetcodeClient));

        public NetcodeClient
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

            networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectedEventHandler;
        }

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NetcodeClient)}");
            }

            Disconnect();
            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectedEventHandler;
            GC.SuppressFinalize(this);
        }

        public async UniTask<bool> ConnectAsync(ConnectionParameter connectionParameter, CancellationToken token = default)
        {
            if (networkManager.IsHost)
            {
                throw new InvalidOperationException("This client is already running as a host client");
            }

            if (IsConnected)
            {
                throw new InvalidOperationException("This client is already connected to the server");
            }

            if (connectionParameter is null)
            {
                throw new ArgumentNullException(nameof(connectionParameter));
            }

            var connectionConfig = connectionParameter.ConnectionConfig;
            if (connectionConfig is null)
            {
                throw new ArgumentException($"'{nameof(connectionParameter.ConnectionConfig)}' in '{nameof(connectionParameter)}' must not be null");
            }

            networkTransportInitializer.Initialize(networkManager, connectionConfig);
            if (connectionParameter.ConnectionData is not null)
            {
                networkManager.NetworkConfig.ConnectionData = connectionParameter.ConnectionData.Serialize();
            }

            _ = networkManager.StartClient();

            try
            {
                await UniTask
                    .WaitUntil(() => IsConnected, cancellationToken: token)
                    .Timeout(TimeSpan.FromSeconds(connectionParameter.ConnectionTimeoutSeconds));
            }
            catch (TimeoutException)
            {
                networkManager.Shutdown();
                throw new TimeoutException("The connection timed-out");
            }
            catch (OperationCanceledException)
            {
                networkManager.Shutdown();
                throw new OperationCanceledException("The connection operation cancelled");
            }
            catch (Exception e)
            {
                networkManager.Shutdown();
                throw new Exception("An unexpected error occurred during the connection", e);
            }

            return true;
        }

        public void Disconnect()
        {
            if (IsRunning)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("The client will disconnect from server");
                }

                OnBeforeDisconnect?.Invoke();
                networkManager.Shutdown();
            }
            else
            {
                throw new InvalidOperationException("Unable to disconnect because client is not running");
            }
        }

        public void SendMessageToServer
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (IsConnected)
            {
                networkManager.CustomMessagingManager.SendNamedMessage(messageName, NetworkManager.ServerClientId, messageStream, networkDelivery);
            }
            else
            {
                throw new InvalidOperationException("Unable to send message to server because client is not running");
            }
        }

        public void RegisterNamedMessage(string messageName, HandleNamedMessageDelegate namedMessageHandler)
        {
            if (IsConnected)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, namedMessageHandler);
            }
            else
            {
                throw new InvalidOperationException("Unable to register named message handler because client is not running");
            }
        }

        public void UnregisterNamedMessage(string messageName)
        {
            if (IsConnected)
            {
                networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
            }
            else
            {
                throw new InvalidOperationException("Unable to unregister named message handler because client is not running");
            }
        }

        #region Callbacks
        private void OnClientConnectedEventHandler(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has connected to server with client id {clientId}");
            }

            OnConnected?.Invoke(clientId);
        }

        private void OnClientDisconnectedEventHandler(ulong clientId)
        {
            if (!IsConnected)
            {
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has disconnected from server");
            }

            OnDisconnected?.Invoke(clientId);
        }
        #endregion
    }
}
