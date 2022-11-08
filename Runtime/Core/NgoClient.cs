using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public class NgoClient : INgoClient
    {
        public bool IsRunning => networkManager != null && networkManager.IsClient;
        public bool IsConnected => networkManager != null && networkManager.IsConnectedClient;

        public event Action OnConnected;
        public event Action OnDisconnecting;
        public event Action OnUnexpectedDisconnected;

        private readonly NetworkManager networkManager;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoClient));

        public NgoClient(NetworkManager networkManager)
        {
#pragma warning disable IDE0016
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }
#pragma warning restore IDE0016

            this.networkManager = networkManager;
            this.networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            this.networkManager.OnClientDisconnectCallback += OnClientDisconnectedEventHandler;
        }

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NgoClient)}");
            }

            if (IsRunning)
            {
                DisconnectAsync().Forget();
            }
            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectedEventHandler;
        }

        public async UniTask ConnectAsync(ConnectionParameter connectionParameter, CancellationToken token = default)
        {
            if (networkManager.IsHost)
            {
                throw new InvalidOperationException("This client is already running as a host");
            }
            if (IsConnected)
            {
                throw new InvalidOperationException("This client is already connected to the server");
            }
            if (connectionParameter is null)
            {
                throw new ArgumentNullException(nameof(connectionParameter));
            }

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
                throw new OperationCanceledException("The connection operation was canceled");
            }
        }

        public async UniTask DisconnectAsync()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to disconnect because client is not running");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug("The client will disconnect from server");
            }

            OnDisconnecting?.Invoke();
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        public void SendMessage
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to send message to server because client is not running");
            }
            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }
            if (!messageStream.IsInitialized)
            {
                throw new ArgumentException($"{nameof(messageStream)} is not initialized");
            }

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, NetworkManager.ServerClientId, messageStream, networkDelivery);
        }

        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate namedMessageHandler)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to register named message handler because client is not running");
            }
            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, namedMessageHandler);
        }

        public void UnregisterMessageHandler(string messageName)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to unregister named message handler because client is not running");
            }

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
        }

        #region Callbacks
        private void OnClientConnectedEventHandler(ulong serverId)
        {
            if (!IsConnected)
            {
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has connected to server");
            }

            OnConnected?.Invoke();
        }

        private void OnClientDisconnectedEventHandler(ulong serverId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has disconnected from server");
            }

            OnUnexpectedDisconnected?.Invoke();
        }
        #endregion
    }
}
