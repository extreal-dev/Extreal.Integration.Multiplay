using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.Retry;
using Extreal.Core.Common.System;
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
    public class NgoClient : DisposableBase
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
        /// Invokes just before retrying to connect to the server.
        /// </summary>
        public IObservable<int> OnConnectRetrying => onConnectRetrying;
        private readonly Subject<int> onConnectRetrying = new Subject<int>();

        /// <summary>
        /// Invokes immediately after finishing retrying to connect to the server.
        /// </summary>
        public IObservable<bool> OnConnectRetried => onConnectRetried;
        private readonly Subject<bool> onConnectRetried = new Subject<bool>();

        /// <summary>
        /// Invokes immediately after the connection approval is rejected from the server.
        /// </summary>
        public IObservable<Unit> OnConnectionApprovalRejected => onConnectionApprovalRejected;
        private readonly Subject<Unit> onConnectionApprovalRejected = new Subject<Unit>();

        private readonly NetworkManager networkManager;
        private readonly Dictionary<Type, IConnectionSetter> connectionSetters
            = new Dictionary<Type, IConnectionSetter>
                {
                    {typeof(UnityTransport), new UnityTransportConnectionSetter()},
                    {typeof(UNetTransport), new UNetTransportConnectionSetter()}
                };

        private readonly IRetryStrategy retryStrategy;
        private RetryHandler<bool> connectRetryHandler;
        private readonly CompositeDisposable connectRetryHandlerDisposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NgoClient));

        /// <summary>
        /// Creates a new NgoClient with given networkManager.
        /// </summary>
        /// <param name="networkManager">NetworkManager to be used as a client.</param>
        /// <param name="retryStrategy">Retry strategy to use for connecting to the server</param>
        /// <exception cref="ArgumentNullException">If 'networkManager' is null.</exception>
        public NgoClient(NetworkManager networkManager, IRetryStrategy retryStrategy = null)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException(nameof(networkManager));
            }

            this.networkManager = networkManager;

            this.networkManager.OnClientConnectedCallback += OnClientConnectedEventHandler;
            this.networkManager.OnClientDisconnectCallback += OnClientDisconnectedEventHandlerAsync;

            this.retryStrategy = retryStrategy ?? new NoRetryStrategy();
        }

        /// <inheritdoc/>
        protected override void ReleaseManagedResources()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(NgoClient)}");
            }

            networkManager.OnClientConnectedCallback -= OnClientConnectedEventHandler;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectedEventHandlerAsync;

            if (networkManager.IsClient)
            {
                DisconnectAsync().Forget();
            }

            onConnected.Dispose();
            onDisconnecting.Dispose();
            onUnexpectedDisconnected.Dispose();
            onConnectionApprovalRejected.Dispose();

            DisposeRetryHandler(false);
        }

        /// <summary>
        /// Sets ConnectionSetter.
        /// </summary>
        /// <param name="connectionSetter">Connection setter of NetworkTransport to be set to.</param>
        /// <exception cref="ArgumentNullException">If 'connectionSetter' is null.</exception>
        public void AddConnectionSetter(IConnectionSetter connectionSetter)
        {
            if (connectionSetter == null)
            {
                throw new ArgumentNullException(nameof(connectionSetter));
            }

            connectionSetters[connectionSetter.TargetType] = connectionSetter;
        }

        /// <summary>
        /// Asynchronously connects to the server.
        /// </summary>
        /// <param name="ngoConfig">Connection config to be used in connection.</param>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <exception cref="ArgumentNullException">If 'ngoConfig' is null.</exception>
        /// <exception cref="InvalidOperationException">NetworkTransport in NetworkManager is null or ConnectionSetter for the real type of 'networkTransport' is not set.</exception>
        /// <exception cref="TimeoutException">If 'ngoConfig.TimeoutSeconds' seconds passes without connection.</exception>
        /// <exception cref="OperationCanceledException">If 'token' is canceled.</exception>
        /// <returns>
        /// <para>UniTask of this method.</para>
        /// True if the connection operation is successful, false otherwise.
        /// </returns>
        public async UniTask<bool> ConnectAsync(NgoConfig ngoConfig, CancellationToken token = default)
        {
            Func<UniTask<bool>> connectAsync = async () =>
            {
                if (networkManager.IsClient)
                {
                    if (Logger.IsDebug())
                    {
                        Logger.LogDebug("Unable to connect to the server again while this client is already running");
                    }

                    return false;
                }

                if (ngoConfig == null)
                {
                    throw new ArgumentNullException(nameof(ngoConfig));
                }

                var networkTransport = networkManager.NetworkConfig.NetworkTransport;
                if (networkTransport == null)
                {
                    throw new InvalidOperationException($"{nameof(NetworkTransport)} in {nameof(NetworkManager)} must not be null");
                }

                if (!connectionSetters.ContainsKey(networkTransport.GetType()))
                {
                    throw new InvalidOperationException($"ConnectionSetter of {networkTransport.GetType().Name} is not added");
                }

                connectionSetters[networkTransport.GetType()].Set(networkTransport, ngoConfig);

                if (ngoConfig.ConnectionData != null)
                {
                    networkManager.NetworkConfig.ConnectionData = ngoConfig.ConnectionData;
                }

                _ = networkManager.StartClient();

                try
                {
                    await UniTask
                        .WaitUntil(() => networkManager.IsConnectedClient, cancellationToken: token)
                        .Timeout(ngoConfig.Timeout);
                }
                catch (TimeoutException)
                {
                    await ShutdownAsync();
                    throw new TimeoutException("The connection timed-out");
                }
                catch (OperationCanceledException)
                {
                    await ShutdownAsync();
                    throw new OperationCanceledException("The connection operation was canceled");
                }

                return true;
            };

            DisposeRetryHandler(true);
            connectRetryHandler = RetryHandler<bool>.Of(connectAsync, e => e is TimeoutException, retryStrategy, token);
            connectRetryHandler.OnRetrying.Subscribe(onConnectRetrying.OnNext).AddTo(connectRetryHandlerDisposables);
            connectRetryHandler.OnRetried.Subscribe(onConnectRetried.OnNext).AddTo(connectRetryHandlerDisposables);

            return await connectRetryHandler.HandleAsync();
        }

        private void DisposeRetryHandler(bool clearOnly)
        {
            connectRetryHandler?.Dispose();
            if (clearOnly)
            {
                connectRetryHandlerDisposables.Clear();
            }
            else
            {
                connectRetryHandlerDisposables.Dispose();
            }
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
            await ShutdownAsync();
        }

        private async UniTask ShutdownAsync()
        {
            networkManager.Shutdown();
            await UniTask.WaitWhile(() => networkManager.ShutdownInProgress);
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <exception cref="InvalidOperationException">If this client is not running.</exception>
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
            IfClientIsNotRunningThenThrowException();

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
        /// <exception cref="InvalidOperationException">If this client is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler)
        {
            IfClientIsNotRunningThenThrowException();

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
        /// <exception cref="InvalidOperationException">If this client is not running.</exception>
        /// <exception cref="ArgumentNullException">If 'messageName' is null.</exception>
        public void UnregisterMessageHandler(string messageName)
        {
            IfClientIsNotRunningThenThrowException();

            if (messageName == null)
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
        }

        private void IfClientIsNotRunningThenThrowException()
        {
            if (!networkManager.IsClient)
            {
                throw new InvalidOperationException("This client is not running");
            }
        }

        private void OnClientConnectedEventHandler(ulong serverId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"The client has connected to the server");
            }

            onConnected.OnNext(Unit.Default);
        }

        private async void OnClientDisconnectedEventHandlerAsync(ulong serverId)
        {
            if (networkManager.IsConnectedClient)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The client unexpectedly disconnected from the server");
                }

                onUnexpectedDisconnected.OnNext(Unit.Default);
                await ReconnectAsync();
            }
            else
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The connection was rejected by the server");
                }

                onConnectionApprovalRejected.OnNext(Unit.Default);
            }
        }

        [SuppressMessage("Design", "CC0004")]
        private async UniTask ReconnectAsync()
        {
            if (retryStrategy is NoRetryStrategy)
            {
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug("Reconnection start");
            }

            await ShutdownAsync();

            Exception exception = null;
            try
            {
                await connectRetryHandler.HandleAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug("Reconnection finished", exception);
            }
        }
    }
}
