using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.Retry;
using Extreal.Core.Logging;
using Extreal.Integration.Multiplay.NGO.Test.Sub;
using NUnit.Framework;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoClientTest
    {
        private NgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingManager clientMessagingManager;

        private bool onConnected;
        private bool onDisconnectingEventHandler;
        private bool onUnexpectedDisconnected;
        private bool onApprovalRejected;
        private bool onMessageReceived;
        private int onConnectRetrying;
        private bool onConnectRetried;
        private bool isInvokeOnConnectRetried;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            networkManager.NetworkConfig.ConnectionApproval = true;

            InitializeClient();
        });

        private void InitializeClient(IRetryStrategy retryStrategy = null)
        {
            DisposeClient();

            ngoClient = new NgoClient(networkManager, retryStrategy);

            _ = ngoClient.OnConnected
                .Subscribe(_ => onConnected = true)
                .AddTo(disposables);

            _ = ngoClient.OnDisconnecting
                .Subscribe(_ => onDisconnectingEventHandler = true)
                .AddTo(disposables);

            _ = ngoClient.OnUnexpectedDisconnected
                .Subscribe(_ => onUnexpectedDisconnected = true)
                .AddTo(disposables);

            _ = ngoClient.OnConnectionApprovalRejected
                .Subscribe(_ => onApprovalRejected = true)
                .AddTo(disposables);

            _ = ngoClient.OnConnectRetrying
                .Subscribe(retryCount => onConnectRetrying = retryCount)
                .AddTo(disposables);

            _ = ngoClient.OnConnectRetried
                .Subscribe(retryResult =>
                {
                    isInvokeOnConnectRetried = true;
                    onConnectRetried = retryResult;
                })
                .AddTo(disposables);

            onConnected = false;
            onDisconnectingEventHandler = false;
            onUnexpectedDisconnected = false;
            onConnectRetrying = 0;
            onConnectRetried = false;
            isInvokeOnConnectRetried = false;

            clientMessagingManager = new ClientMessagingManager(ngoClient);

            _ = clientMessagingManager.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);

            onMessageReceived = false;
        }

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            DisposeClient();

            if (networkManager != null)
            {
                await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
                UnityEngine.Object.Destroy(networkManager.gameObject);
            }
        });

        private void DisposeClient()
        {
            clientMessagingManager?.Dispose();
            ngoClient?.Dispose();
            disposables.Clear();
        }

        [OneTimeTearDown]
        public void OneTimeDispose()
            => disposables.Dispose();

        [Test]
        public void NewNgoClientWithNetworkManagerNull()
            => Assert.That(() => _ = new NgoClient(null),
                    Throws.TypeOf<ArgumentNullException>()
                        .With.Message.Contains(nameof(networkManager)));

        [Test]
        public void AddConnectionSetter()
            => ngoClient.AddConnectionSetter(new UnityTransportConnectionSetter());

        [Test]
        public void AddConnectionSetterNull()
            => Assert.That(() => ngoClient.AddConnectionSetter(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("connectionSetter"));

        [UnityTest]
        public IEnumerator ConnectSuccess() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();

            Assert.IsFalse(onConnected);
            var result = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(result);
            Assert.IsTrue(onConnected);
            Assert.IsTrue(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator ConnectSuccessForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(new CountingRetryStrategy());

            var ngoConfig = new NgoConfig();

            Assert.IsFalse(onConnected);
            var result = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(result);
            Assert.IsTrue(onConnected);
            Assert.IsTrue(networkManager.IsConnectedClient);
            Assert.AreEqual(0, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectWithConnectionData() => UniTask.ToCoroutine(async () =>
        {
            var failedNgoConfig = new NgoConfig(connectionData: new byte[] { 1, 2, 3, 4 }, timeout: TimeSpan.FromSeconds(1));

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(failedNgoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);
            Assert.IsTrue(onApprovalRejected);
            Assert.IsFalse(networkManager.IsConnectedClient);

            var successNgoConfig = new NgoConfig(connectionData: new byte[] { 3, 7, 7, 6 });
            _ = await ngoClient.ConnectAsync(successNgoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator ConnectTwice() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            var result = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsFalse(result);
            Assert.IsTrue(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator ConnectWithNgoConfigNull() => UniTask.ToCoroutine(async () =>
        {
            const NgoConfig nullNgoConfig = null;

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(nullNgoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentNullException), exception.GetType());
            Assert.IsTrue(exception.Message.Contains("ngoConfig"));
        });

        [UnityTest]
        public IEnumerator ConnectWithNetworkTransportNull() => UniTask.ToCoroutine(async () =>
        {
            networkManager.NetworkConfig.NetworkTransport = null;

            var nullNgoConfig = new NgoConfig();

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(nullNgoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual($"{nameof(NetworkTransport)} in {nameof(NetworkManager)} must not be null", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectWithNotHandledNetworkTransport() => UniTask.ToCoroutine(async () =>
        {
            var networkTransportMock = new GameObject().AddComponent<NetworkTransportMock>();
            networkManager.NetworkConfig.NetworkTransport = networkTransportMock;

            var ngoConfig = new NgoConfig();

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(ngoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual($"ConnectionSetter of {nameof(NetworkTransportMock)} is not added", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectWithTimeoutException() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig(port: 7776, timeout: TimeSpan.FromSeconds(1));

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(ngoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectWithTimeoutExceptionForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(new CountingRetryStrategy(5));

            var ngoConfig = new NgoConfig(port: 7776, timeout: TimeSpan.FromSeconds(1));

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(ngoConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsTrue(isInvokeOnConnectRetried);
            Assert.IsFalse(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectWithOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var ngoConfig = new NgoConfig(port: 7776);

            Exception exception = null;
            try
            {
                UniTaskCancelInOneFrameAsync(cancellationTokenSource).Forget();
                _ = await ngoClient.ConnectAsync(ngoConfig, cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
            Assert.AreEqual("The connection operation was canceled", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectWithOperationCanceledExceptionForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(new CountingRetryStrategy(5, _ => TimeSpan.FromSeconds(10)));

            var cancellationTokenSource = new CancellationTokenSource();
            UniTask.Create(async () =>
            {
                await UniTask.WaitUntil(() => onConnectRetrying == 1);
                await UniTask.Delay(TimeSpan.FromSeconds(3));
                cancellationTokenSource.Cancel();
            }).Forget();

            var ngoConfig = new NgoConfig(port: 7776, timeout: TimeSpan.FromMilliseconds(100));

            Exception exception = null;
            try
            {
                _ = await ngoClient.ConnectAsync(ngoConfig, cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
            Assert.AreEqual("The retry was canceled", exception.Message);
        });

        [UnityTest]
        public IEnumerator DisconnectFromServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            Assert.IsFalse(onDisconnectingEventHandler);
            await ngoClient.DisconnectAsync();
            Assert.IsTrue(onDisconnectingEventHandler);
            Assert.IsFalse(onUnexpectedDisconnected);
            Assert.IsFalse(networkManager.IsClient);
            Assert.IsFalse(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator StopServerBeforeDisconnect() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            clientMessagingManager.SendRestartServer();

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
            Assert.IsFalse(networkManager.IsClient);
            Assert.IsFalse(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator StopServerBeforeDisconnectForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(new CountingRetryStrategy());

            var ngoConfig = new NgoConfig(timeout: TimeSpan.FromMilliseconds(1000));
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            clientMessagingManager.SendRestartServer();

            onConnected = false;
            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
            Assert.IsFalse(networkManager.IsClient);
            Assert.IsFalse(networkManager.IsConnectedClient);

            await UniTask.WaitUntil(() => onConnected);
            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsTrue(0 < onConnectRetrying);
            Assert.IsTrue(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator StopServerBeforeDisconnectWithTimeoutExceptionForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(new CountingRetryStrategy(1));

            var ngoConfig = new NgoConfig(timeout: TimeSpan.FromMilliseconds(1000));
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            clientMessagingManager.SendRestartServer();

            onConnected = false;
            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
            Assert.IsFalse(networkManager.IsClient);
            Assert.IsFalse(networkManager.IsConnectedClient);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsFalse(onConnected);
            Assert.IsTrue(0 < onConnectRetrying);
            Assert.IsFalse(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator SendMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            clientMessagingManager.SendHelloWorld();

            await UniTask.WaitUntil(() => onMessageReceived);

            Assert.AreEqual(MessageName.HelloWorldToAllClients, clientMessagingManager.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMessagingManager.ReceivedMessageText);
        });

        [Test]
        public void SendMessageWithoutConnect()
            => Assert.That(() => ngoClient.SendMessage("TestMessage", new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp)),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This client is not running"));

        [UnityTest]
        public IEnumerator SendMessageWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            const string nullMessageName = null;
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            Assert.That(() => ngoClient.SendMessage(nullMessageName, messageStream),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator SendMessageWithMessageStreamNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            const string messageName = "TestMessage";
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => ngoClient.SendMessage(messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream is not initialized"));
        });

        [Test]
        public void RegisterMessageHandlerWithoutConnect()
            => Assert.That(() => ngoClient.RegisterMessageHandler("TestMessage", (_, _) => { }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This client is not running"));

        [UnityTest]
        public IEnumerator RegisterMessageHandlerWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            const string nullMessageName = null;
            Assert.That(() => ngoClient.RegisterMessageHandler(nullMessageName, (_, _) => { }),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [Test]
        public void UnregisterMessageHandlerWithoutConnect()
            => Assert.That(() => ngoClient.UnregisterMessageHandler("TestMessage"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This client is not running"));

        [UnityTest]
        public IEnumerator UnregisterMessageHandlerWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            const string nullMessageName = null;
            Assert.That(() => ngoClient.UnregisterMessageHandler(nullMessageName),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator UnregisterMessageHandlerWithoutRegister() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            ngoClient.UnregisterMessageHandler("TestMessage");
        });

        private static async UniTaskVoid UniTaskCancelInOneFrameAsync(CancellationTokenSource cts)
        {
            await UniTask.Yield();
            cts.Cancel();
        }
    }
}
