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
    public class NgoClient : IDisposable
    {
        /// <summary>
        /// Invokes immediately after connecting to the server.
        /// </summary>
        public IObservable<Unit> OnConnected => onConnected;
        private readonly Subject<Unit> onConnected = new Subject<Unit>();

        /// <summary>
        /// Invokes just before disconnection from the server.
        /// </summary>
        public IObservable<Unit> OnDisconnecting => onDisconnecting;
        private readonly Subject<Unit> onDisconnecting = new Subject<Unit>();

        /// <summary>
        /// Invokes immediately after an unexpected disconnection from the server.
        /// </summary>
        public IObservable<Unit> OnUnexpectedDisconnected => onUnexpectedDisconnected;
        private readonly Subject<Unit> onUnexpectedDisconnected = new Subject<Unit>();

        /// <summary>
        /// Invokes immediately after the connection approval is rejected from the server.
        /// </summary>
        public IObservable<Unit> OnApprovalRejected => onApprovalRejected;
        private readonly Subject<Unit> onApprovalRejected = new Subject<Unit>();

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

            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectedEventHandler;

            if (networkManager.IsClient)
            {
                DisconnectAsync().Forget();
            }

            onConnected.Dispose();
            onDisconnecting.Dispose();
            onUnexpectedDisconnected.Dispose();
            onApprovalRejected.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets NetworkTransportConfiger.
        /// </summary>
        /// <param name="networkTransportConfiger">Configer of NetworkTransport to be set to.</param>
        /// <exception cref="ArgumentNullException">If 'networkTransportConfiger' is null.</exception>
        public void AddNetworkTransportConfiger(INetworkTransportConfiger networkTransportConfiger)
        {
            if (networkTransportConfiger == null)
            {
                throw new ArgumentNullException(nameof(networkTransportConfiger));
            }

            networkTransportConfigers[networkTransportConfiger.TargetType] = networkTransportConfiger;
        }

        /// <summary>
        /// Asynchronously connects to the server.
        /// </summary>
        /// <param name="connectionConfig">Connection config to be used in connection.</param>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <exception cref="ArgumentNullException">If 'connectionConfig' is null.</exception>
        /// <exception cref="InvalidOperationException">NetworkTransport in NetworkManager is null or The configer of the real type of 'networkTransport' is not set.</exception>
        /// <exception cref="TimeoutException">If 'connectionConfig.TimeoutSeconds' seconds passes without connection.</exception>
        /// <exception cref="OperationCanceledException">If 'token' is canceled.</exception>
        /// <returns>
        /// <para>UniTask of this method.</para>
        /// True if the connection operation is successful, false otherwise.
        /// </returns>
        public async UniTask<bool> ConnectAsync(NgoConfig connectionConfig, CancellationToken token = default)
        {
            if (networkManager.IsClient || networkManager.IsServer || networkManager.IsHost)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("Cannot start Client while an instance is already running");
                }
                return false;
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
                    .WaitUntil(() => networkManager.IsConnectedClient, cancellationToken: token)
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

            return true;
        }

        /// <summary>
        /// Asynchronously disconnects from the server.
        /// </summary>
        /// <returns>UniTask of this method.</returns>
        public async UniTask DisconnectAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug("This client will disconnect from the server");
            }

            onDisconnecting.OnNext(Unit.Default);
            networkManager.Shutdown();

            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        /// <exception cref="ArgumentException">If 'messageStream' is not initialized.</exception>
        /// <returns>True if the message is successfully sent, false otherwise.</returns>
        public void SendMessage
        (
            string messageName,
            FastBufferWriter messageStream,
            NetworkDelivery networkDelivery = NetworkDelivery.Reliable
        )
        {
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

        /// <summary>
        /// Registers a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageHandler">Message handler to be registered.</param>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler)
        {
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
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void UnregisterMessageHandler(string messageName)
        {
            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
        }

        private void OnClientConnectedEventHandler(ulong serverId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has connected to the server");
            }

            onConnected.OnNext(Unit.Default);
        }

        private void OnClientDisconnectedEventHandler(ulong serverId)
        {
            if (networkManager.IsConnectedClient)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The client unexpectedly disconnected from the server");
                }

                onUnexpectedDisconnected.OnNext(Unit.Default);
            }
            else
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The connection was rejected by the server");
                }

                onApprovalRejected.OnNext(Unit.Default);
            }
        }
    }
}
