using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Multiplay.NGO.Test.Sub;
using NUnit.Framework;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoClientTest
    {
        private INgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingHub clientMassagingHub;

        private bool onConnected;
        private bool onDisconnectingEventHandler;
        private bool onUnexpectedDisconnected;
        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            var networkTransportInitializer = new UnityTransportInitializer();
            ngoClient = new NgoClient(networkManager, networkTransportInitializer);

            _ = ngoClient.OnConnected
                .Subscribe(_ => onConnected = true)
                .AddTo(disposables);

            _ = ngoClient.OnDisconnecting
                .Subscribe(_ => onDisconnectingEventHandler = true)
                .AddTo(disposables);

            _ = ngoClient.OnUnexpectedDisconnected
                .Subscribe(_ => onUnexpectedDisconnected = true)
                .AddTo(disposables);

            onConnected = false;
            onDisconnectingEventHandler = false;
            onUnexpectedDisconnected = false;

            clientMassagingHub = new ClientMessagingHub(ngoClient);

            _ = clientMassagingHub.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);

            onMessageReceived = false;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            clientMassagingHub.Dispose();
            ngoClient.Dispose();
            disposables.Clear();

            await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
            UnityEngine.Object.Destroy(networkManager.gameObject);
        });

        [OneTimeTearDown]
        public void OneTimeDispose()
            => disposables.Dispose();

        [Test]
        public void NewNgoClientWithNetworkManagerNull()
        => Assert.That(() => _ = new NgoClient(null, new UnityTransportInitializer()),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains(nameof(networkManager)));

        [Test]
        public void NewNgoClientWithNetworkTransportInitializerNull()
        => Assert.That(() => _ = new NgoClient(networkManager, null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("networkTransportInitializer"));

        [UnityTest]
        public IEnumerator ConnectToServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);

            Assert.IsFalse(onConnected);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(onConnected);
            Assert.IsTrue(ngoClient.IsRunning);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectToServerWithConnectionData() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var failedConnectionData = new ConnectionData();
            failedConnectionData.SetData(new byte[] { 1, 2, 3, 4 });
            var failedConnectionParameter = new ConnectionParameter(connectionConfig, failedConnectionData, 1);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(failedConnectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);
            Assert.IsTrue(onUnexpectedDisconnected);
            Assert.IsFalse(ngoClient.IsConnected);
            Assert.IsFalse(ngoClient.IsRunning);

            var successConnectionData = new ConnectionData();
            successConnectionData.SetData(new byte[] { 3, 7, 7, 6 });
            var successConnectionParameter = new ConnectionParameter(connectionConfig, successConnectionData);
            await ngoClient.ConnectAsync(successConnectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectToServerUsingHost() => UniTask.ToCoroutine(async () =>
        {
            var result = networkManager.StartHost();
            Assert.IsTrue(result);

            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(connectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual("This client is already running as a host", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectToServerTwice() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(connectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual("This client is already connected to the server", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectToServerWithConnectionParameterNull() => UniTask.ToCoroutine(async () =>
        {
            const ConnectionParameter nullConnectionParameter = null;

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(nullConnectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentNullException), exception.GetType());
            Assert.IsTrue(exception.Message.Contains("connectionParameter"));
        });

        [UnityTest]
        public IEnumerator ConnectToServerWithConnectionConfigNull() => UniTask.ToCoroutine(async () =>
        {
            const ConnectionConfig nullConnectionConfig = null;
            var connectionParameter = new ConnectionParameter(nullConnectionConfig);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(connectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentException), exception.GetType());
            Assert.AreEqual($"The connectionConfig in {nameof(connectionParameter)} must not be null", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectWithInvalidIpAddressForm() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = new ConnectionConfig("256:0:0:1");
            var connectionParameter = new ConnectionParameter(connectionConfig);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(connectionParameter);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentException), exception.GetType());
            Assert.AreEqual($"Address in {nameof(connectionConfig)} is invalid", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectToServerWithTimeoutException() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = new ConnectionConfig(port: 7776);
            var connectionParameter = new ConnectionParameter(connectionConfig, connectionTimeoutSeconds: 1);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(connectionParameter);
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
        public IEnumerator ConnectToServerWithOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var connectionConfig = new ConnectionConfig(port: 7776);
            var connectionParameter = new ConnectionParameter(connectionConfig);

            Exception exception = null;
            try
            {
                UniTaskCancelInOneFrameAsync(cancellationTokenSource).Forget();
                await ngoClient.ConnectAsync(connectionParameter, cancellationTokenSource.Token);
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
        public IEnumerator DisconnectFromServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            Assert.IsFalse(onDisconnectingEventHandler);
            await ngoClient.DisconnectAsync();
            Assert.IsTrue(onDisconnectingEventHandler);
            Assert.IsFalse(onUnexpectedDisconnected);
            Assert.IsFalse(ngoClient.IsRunning);
            Assert.IsFalse(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator DisconnectFromServerWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await ngoClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual("Unable to disconnect because client is not running", exception.Message);
        });

        [UnityTest]
        public IEnumerator StopServerBeforeDisconnect() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
        });

        [UnityTest]
        public IEnumerator SendToServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            clientMassagingHub.SendHelloWorldToServer();

            await UniTask.WaitUntil(() => onMessageReceived);

            Assert.AreEqual(MessageName.HELLO_WORLD_TO_ALL_CLIENTS, clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);
        });

        [UnityTest]
        public IEnumerator SendToServerWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            const string nullMessageName = null;
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            Assert.That(() => ngoClient.SendMessage(nullMessageName, messageStream),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator SendToServerWithMessageStreamNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            const string messageName = "TestMessage";
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => ngoClient.SendMessage(messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream is not initialized"));
        });

        [Test]
        public void SendToServerWithoutConnect()
            => Assert.That(() => clientMassagingHub.SendHelloWorldToServer(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to send message to server because client is not running"));

        [UnityTest]
        public IEnumerator RegisterNamedMessageWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            const string nullMessageName = null;
            Assert.That(() => ngoClient.RegisterMessageHandler(nullMessageName, (_, _) => { return; }),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [Test]
        public void RegisterNamedMessageWithoutConnect()
            => Assert.That(() => ngoClient.RegisterMessageHandler("TestMessage", (_, _) => { return; }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to register named message handler because client is not running"));

        [UnityTest]
        public IEnumerator UnregisterNamedMessageWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            const string nullMessageName = null;
            Assert.That(() => ngoClient.UnregisterMessageHandler(nullMessageName),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [Test]
        public void UnregisterNamedMessageWithoutConnect()
            => Assert.That(() => ngoClient.UnregisterMessageHandler("TestMessage"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to unregister named message handler because client is not running"));

        [UnityTest]
        public IEnumerator UnregisterNamedMassageWithoutRegister() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            ngoClient.UnregisterMessageHandler("TestMessage");
        });

        private static async UniTaskVoid UniTaskCancelInOneFrameAsync(CancellationTokenSource cts)
        {
            await UniTask.Yield();
            cts.Cancel();
        }
    }
}
