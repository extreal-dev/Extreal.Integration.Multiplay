using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UniRx;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that handles NetworkManager as a client.
    /// </summary>
    public class NgoClient : INgoClient
    {
        /// <inheritdoc/>
        public IObservable<Unit> OnConnected => onConnected;
        private readonly Subject<Unit> onConnected = new Subject<Unit>();

        /// <inheritdoc/>
        public IObservable<Unit> OnDisconnecting => onDisconnecting;
        private readonly Subject<Unit> onDisconnecting = new Subject<Unit>();

        /// <inheritdoc/>
        public IObservable<Unit> OnUnexpectedDisconnected => onUnexpectedDisconnected;
        private readonly Subject<Unit> onUnexpectedDisconnected = new Subject<Unit>();

        /// <inheritdoc/>
        public IObservable<Unit> OnApprovalRejected => onApprovalRejected;
        private readonly Subject<Unit> onApprovalRejected = new Subject<Unit>();

        /// <inheritdoc/>
        public bool IsRunning => networkManager != null && networkManager.IsClient;

        /// <inheritdoc/>
        public bool IsConnected => networkManager != null && networkManager.IsConnectedClient;

        private readonly NetworkManager networkManager;
        private readonly Dictionary<Type, INetworkTransportConfiger> networkTransportConfigers
            = new Dictionary<Type, INetworkTransportConfiger>
                {
                    {typeof(UnityTransport), new UnityTransportConfiger()},
                    {typeof(UNetTransport), new UNetTransportConfiger()}
                };

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoClient));

        /// <summary>
        /// Creates a new NgoClient with given networkManager.
        /// </summary>
        /// <param name="networkManager">NetworkManager to be used as a client.</param>
        /// <exception cref="ArgumentNullException">If 'networkManager' is null.</exception>
        public NgoClient(NetworkManager networkManager)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }

            this.networkManager = networkManager;

            this.networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            this.networkManager.OnClientDisconnectCallback += OnClientDisconnectedEventHandler;
        }

        /// <summary>
        /// Finalizes NgoClient.
        /// </summary>
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

            onConnected.Dispose();
            onDisconnecting.Dispose();
            onUnexpectedDisconnected.Dispose();
            onApprovalRejected.Dispose();
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">If 'networkTransportConfiger' is null.</exception>
        public void SetNetworkTransportConfiger(INetworkTransportConfiger networkTransportConfiger)
        {
            if (networkTransportConfiger == null)
            {
                throw new ArgumentNullException(nameof(networkTransportConfiger));
            }

            networkTransportConfigers[networkTransportConfiger.GetTargetType] = networkTransportConfiger;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this client is already running/connected, NetworkTransport in NetworkManager is null or The configer of the real type of 'networkTransport' is not set.</exception>
        /// <exception cref="ArgumentNullException">If 'connectionConfig' is null.</exception>
        /// <exception cref="TimeoutException">If 'connectionConfig.TimeoutSeconds' seconds passes without connection.</exception>
        /// <exception cref="OperationCanceledException">If 'token' is canceled.</exception>
        public async UniTask ConnectAsync(ConnectionConfig connectionConfig, CancellationToken token = default)
        {
            if (networkManager.IsHost)
            {
                throw new InvalidOperationException("This client is already running as a host");
            }
            if (IsConnected)
            {
                throw new InvalidOperationException("This client is already connected to the server");
            }
            if (connectionConfig == null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            var networkTransport = networkManager.NetworkConfig.NetworkTransport;
            if (networkTransport == null)
            {
                throw new InvalidOperationException($"{nameof(NetworkTransport)} in {nameof(NetworkManager)} must not be null");
            }

            if (!networkTransportConfigers.ContainsKey(networkTransport.GetType()))
            {
                throw new InvalidOperationException($"The configer of {networkTransport.GetType().Name} is not set");
            }

            networkTransportConfigers[networkTransport.GetType()].SetConfig(networkTransport, connectionConfig);

            if (connectionConfig.ConnectionData != null)
            {
                networkManager.NetworkConfig.ConnectionData = connectionConfig.ConnectionData;
            }

            _ = networkManager.StartClient();

            try
            {
                await UniTask
                    .WaitUntil(() => IsConnected, cancellationToken: token)
                    .Timeout(TimeSpan.FromSeconds(connectionConfig.TimeoutSeconds));
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

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this client is not yet running.</exception>
        public async UniTask DisconnectAsync()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Unable to disconnect because this client is not running");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug("This client will disconnect from the server");
            }

            onDisconnecting.OnNext(Unit.Default);
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this client is not yet connected to the server.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        public void SendMessage
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to send message to server because this client is not running");
            }
            if (messageName is null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }
            if (!messageStream.IsInitialized)
            {
                throw new ArgumentException($"{nameof(messageStream)} is not initialized");
            }

            networkManager.CustomMessagingManager.SendNamedMessage(messageName, NetworkManager.ServerClientId, messageStream, networkDelivery);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this client is not yet connected to the server.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to register named message handler because this client is not running");
            }
            if (messageName is null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, messageHandler);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">If this client is not yet connected to the server.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void UnregisterMessageHandler(string messageName)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unable to unregister named message handler because this client is not running");
            }
            if (messageName is null)
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
                Logger.LogDebug($"The client has connected to the server");
            }

            onConnected.OnNext(Unit.Default);
        }

        private void OnClientDisconnectedEventHandler(ulong serverId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client unexpectedly has disconnected from the server");
            }

            if (IsConnected)
            {
                onUnexpectedDisconnected.OnNext(Unit.Default);
            }
            else
            {
                onApprovalRejected.OnNext(Unit.Default);
            }
        }
        #endregion
    }
}
