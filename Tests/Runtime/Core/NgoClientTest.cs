using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Multiplay.NGO.Test.Sub;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoClient = new NgoClient(networkManager);

            ngoClient.OnConnected += OnConnectedEventHandler;
            ngoClient.OnDisconnecting += OnDisconnectingEventHandler;
            ngoClient.OnUnexpectedDisconnected += OnUnexpectedDisconnectedEventHandler;

            onConnected = false;
            onDisconnectingEventHandler = false;
            onUnexpectedDisconnected = false;

            clientMassagingHub = new ClientMessagingHub(ngoClient);

            clientMassagingHub.OnMessageReceived += OnMessageReceivedEventHandler;

            onMessageReceived = false;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            clientMassagingHub.OnMessageReceived -= OnMessageReceivedEventHandler;
            ngoClient.OnConnected -= OnConnectedEventHandler;
            ngoClient.OnDisconnecting -= OnDisconnectingEventHandler;
            ngoClient.OnUnexpectedDisconnected -= OnUnexpectedDisconnectedEventHandler;

            clientMassagingHub.Dispose();
            ngoClient.Dispose();

            await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
            UnityEngine.Object.Destroy(networkManager.gameObject);
        });

        [Test]
        public void NewNgoClientWithNetworkManagerNull()
        => Assert.That(() => _ = new NgoClient(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains(nameof(networkManager)));

        [UnityTest]
        public IEnumerator ConnectToServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();

            Assert.IsFalse(onConnected);
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(onConnected);
            Assert.IsTrue(ngoClient.IsRunning);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectToServerWithConnectionData() => UniTask.ToCoroutine(async () =>
        {
            var failedConnectionData = new ConnectionData();
            failedConnectionData.SetData(new byte[] { 1, 2, 3, 4 });
            var failedConnectionParameter = new ConnectionParameter(failedConnectionData, 1);

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
            var successConnectionParameter = new ConnectionParameter(successConnectionData);
            await ngoClient.ConnectAsync(successConnectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectToServerUsingHost() => UniTask.ToCoroutine(async () =>
        {
            var result = networkManager.StartHost();
            Assert.IsTrue(result);

            var connectionParameter = new ConnectionParameter();

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
            var connectionParameter = new ConnectionParameter();
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
        public IEnumerator ConnectToServerWithTimeoutException() => UniTask.ToCoroutine(async () =>
        {
            var unityTransport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            unityTransport.ConnectionData.Port -= 1;
            var connectionParameter = new ConnectionParameter(connectionTimeoutSeconds: 1);

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

            var unityTransport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
            unityTransport.ConnectionData.Port -= 1;
            var connectionParameter = new ConnectionParameter();

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
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
        });

        [UnityTest]
        public IEnumerator SendToServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
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
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            ngoClient.UnregisterMessageHandler("TestMessage");
        });

        private static async UniTaskVoid UniTaskCancelInOneFrameAsync(CancellationTokenSource cts)
        {
            await UniTask.Yield();
            cts.Cancel();
        }

        private void OnConnectedEventHandler()
            => onConnected = true;

        private void OnDisconnectingEventHandler()
            => onDisconnectingEventHandler = true;

        private void OnUnexpectedDisconnectedEventHandler()
            => onUnexpectedDisconnected = true;

        private void OnMessageReceivedEventHandler()
            => onMessageReceived = true;
    }
}
