using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class NgoServerTest
    {
        private INgoServer ngoServer;
        private NetworkManager networkManager;
        private ServerMessagingHub serverMessagingHub;

        private bool onServerStarted;
        private bool onServerStopping;

        private bool onClientConnected;
        private ulong connectedClientId;

        private bool onClientDisconnecting;
        private ulong disconnectingClientId;

        private bool onClientRemoving;
        private ulong removingClientId;
        private string clientRemovingMessage;

        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoServer = new NgoServer(networkManager);

            onServerStarted = false;
            onServerStopping = false;
            onClientConnected = false;
            connectedClientId = 0;
            onClientDisconnecting = false;
            disconnectingClientId = 0;
            onClientRemoving = false;
            removingClientId = 0;
            clientRemovingMessage = null;

            _ = ngoServer.OnServerStarted
                .Subscribe(_ => onServerStarted = true)
                .AddTo(disposables);

            _ = ngoServer.OnServerStopping
                .Subscribe(_ => onServerStopping = true)
                .AddTo(disposables);

            _ = ngoServer.OnClientConnected
                .Subscribe(clientId =>
                {
                    onClientConnected = true;
                    connectedClientId = clientId;
                })
                .AddTo(disposables);

            _ = ngoServer.OnClientDisconnecting
                .Subscribe(clientId =>
                {
                    onClientDisconnecting = true;
                    disconnectingClientId = clientId;
                })
                .AddTo(disposables);

            _ = ngoServer.OnClientRemoving
                .Subscribe(arg =>
                {
                    onClientRemoving = true;
                    removingClientId = arg.clientId;
                    clientRemovingMessage = arg.message;
                })
                .AddTo(disposables);

            serverMessagingHub = new ServerMessagingHub(ngoServer);
            onMessageReceived = false;

            _ = serverMessagingHub.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            serverMessagingHub.Dispose();
            ngoServer.Dispose();
            disposables.Clear();

            if (networkManager != null)
            {
                await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
                UnityEngine.Object.Destroy(networkManager.gameObject);
            }
        });

        [OneTimeTearDown]
        public void OneTimeDispose()
            => disposables.Dispose();

        [Test]
        public void NewNgoServerWithNetworkManagerNull()
        => Assert.That(() => _ = new NgoServer(null),
            Throws.TypeOf<ArgumentNullException>()
                .With.Message.Contains(nameof(networkManager)));

        [UnityTest]
        public IEnumerator StartServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsFalse(onServerStarted);
            await ngoServer.StartServerAsync();
            Assert.IsTrue(onServerStarted);
            Assert.IsTrue(ngoServer.IsRunning);
        });

        [UnityTest]
        public IEnumerator StartServerWithConnectionApproval() => UniTask.ToCoroutine(async () =>
        {
            var connectionApproved = false;
            ngoServer.SetConnectionApprovalCallback((request, response) =>
            {
                connectionApproved = true;
                response.Approved = request.Payload.SequenceEqual(new byte[] { 3, 7, 7, 6 });
            });

            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            await UniTask.WaitUntil(() => connectionApproved);

            Assert.IsFalse(onClientConnected);

            await UniTask.WaitUntil(() => onClientConnected);
            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator StartServerTwice() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            Exception exception = null;
            try
            {
                await ngoServer.StartServerAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual("This server is already running", exception.Message);
        });

        [UnityTest]
        public IEnumerator StartServerWithOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var cancellationTokenSource = new CancellationTokenSource();

            Exception exception = null;
            try
            {
                UniTaskCancelInOneFrameAsync(cancellationTokenSource).Forget();
                await ngoServer.StartServerAsync(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
            Assert.AreEqual("The operation to start server was canceled", exception.Message);
        });

        [UnityTest]
        public IEnumerator ConnectAndDisconnectClients() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            Assert.IsFalse(onClientConnected);
            await UniTask.WaitUntil(() => onClientConnected);

            Assert.IsFalse(onClientDisconnecting);
            await UniTask.WaitUntil(() => onClientDisconnecting);
            Assert.AreEqual(connectedClientId, disconnectingClientId);
        });

        [UnityTest]
        public IEnumerator StopServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            Assert.IsFalse(onServerStopping);
            await ngoServer.StopServerAsync();
            Assert.IsTrue(onServerStopping);
            Assert.IsFalse(ngoServer.IsRunning);
        });

        [UnityTest]
        public IEnumerator StopServerWithoutStart() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await ngoServer.StopServerAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
            Assert.AreEqual("Unable to stop server because it is not running", exception.Message);
        });

        [UnityTest]
        public IEnumerator RemoveClient() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            await UniTask.WaitUntil(() => onClientConnected);
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            const string message = "This is reject client test";
            var result = ngoServer.RemoveClient(connectedClientId, message);
            Assert.IsTrue(result);
            Assert.IsTrue(onClientRemoving);
            Assert.IsFalse(onClientDisconnecting);
            Assert.AreEqual(connectedClientId, removingClientId);
            Assert.AreEqual(message, clientRemovingMessage);
        });

        [Test]
        public void RemoveClientWithoutConnect()
            => Assert.That(() => ngoServer.RemoveClient(0, "This is reject client test"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to reject client because the server is not running"));

        [UnityTest]
        public IEnumerator RemoveNotExistedClient() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const ulong notExistedClientId = 10;
            const string message = "This is reject client test";
            var result = ngoServer.RemoveClient(notExistedClientId, message);
            Assert.IsFalse(result);
            LogAssert.Expect(UnityEngine.LogType.Warning, $"[{Core.Logging.LogLevel.Warn}:{nameof(NgoServer)}] Unable to reject client with client id {notExistedClientId} because it does not exist");
        });

        [UnityTest]
        public IEnumerator SendMessageToClients() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            await UniTask.WaitUntil(() => onClientConnected);

            Assert.IsFalse(onMessageReceived);
            var clientIds = new List<ulong> { connectedClientId };
            var result = serverMessagingHub.SendHelloWorldToClients(clientIds);
            Assert.IsTrue(result);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(connectedClientId, serverMessagingHub.ReceivedClientId);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_SERVER, serverMessagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", serverMessagingHub.ReceivedMessageText);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [Test]
        public void SendMessageToClientsWithoutConnect()
            => Assert.That(() => _ = serverMessagingHub.SendHelloWorldToClients(new List<ulong> { 10 }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to send message because the server is not running"));

        [UnityTest]
        public IEnumerator SendMessageToClientsWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string nullMessageName = null;
            var clientIds = new List<ulong> { 0 };
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            Assert.That(() => _ = ngoServer.SendMessageToClients(clientIds, nullMessageName, messageStream),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsWithMessageStreamNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string messageName = "TestMessage";
            var clientIds = new List<ulong>();
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => _ = ngoServer.SendMessageToClients(clientIds, messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream must be initialized"));
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsWithNotExistedClientId() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string messageName = "TestMessage";
            var notExistedClientIds = new List<ulong> { 10 };
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            var result = ngoServer.SendMessageToClients(notExistedClientIds, messageName, messageStream);
            Assert.IsFalse(result);
            LogAssert.Expect(UnityEngine.LogType.Warning, $"[{Core.Logging.LogLevel.Warn}:{nameof(NgoServer)}] clientIds contains some ids that does not exist");
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClients() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;
            var firstConnectedClientId = connectedClientId;
            await UniTask.WaitUntil(() => onClientConnected);

            Assert.IsFalse(onMessageReceived);
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;
            Assert.AreEqual(firstConnectedClientId, serverMessagingHub.ReceivedClientId);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_SERVER, serverMessagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", serverMessagingHub.ReceivedMessageText);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(connectedClientId, serverMessagingHub.ReceivedClientId);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_SERVER, serverMessagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", serverMessagingHub.ReceivedMessageText);

            await UniTask.WaitUntil(() => ngoServer.ConnectedClients.Count == 0);
        });

        [Test]
        public void SendToAllClientsWithoutConnect()
            => Assert.That(() => _ = serverMessagingHub.SendHelloWorldToAllClients(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to send message because the server is not running"));

        [UnityTest]
        public IEnumerator SendMessageToAllClientsWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string nullMessageName = null;
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            Assert.That(() => ngoServer.SendMessageToAllClients(nullMessageName, messageStream),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClientsWithMessageStreamNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string messageName = "TestMessage";
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => ngoServer.SendMessageToAllClients(messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream must be initialized"));
        });

        [Test]
        public void RegisterNamedMessageWithoutConnect()
            => Assert.That(() => ngoServer.RegisterMessageHandler("TestMessage", (_, _) => { return; }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to register named message handler because server is not running"));

        [UnityTest]
        public IEnumerator RegisterNamedMessageWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string nullMessageName = null;
            Assert.That(() => ngoServer.RegisterMessageHandler(nullMessageName, (_, _) => { return; }),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [Test]
        public void UnregisterNamedMessageWithoutConnect()
            => Assert.That(() => ngoServer.UnregisterMessageHandler("TestMessage"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to unregister named message handler because server is not running"));

        [UnityTest]
        public IEnumerator UnregisterNamedMessageWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            const string nullMessageName = null;
            Assert.That(() => ngoServer.UnregisterMessageHandler(nullMessageName),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator UnregisterNamedMassageWithoutRegister() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(ngoServer.IsRunning);

            ngoServer.UnregisterMessageHandler("TestMessage");
        });

        [UnityTest]
        public IEnumerator GetConnectedClientsWithNetworkManagerNull() => UniTask.ToCoroutine(async () =>
        {
            UnityEngine.Object.Destroy(networkManager.gameObject);
            await UniTask.Yield();
            var connectedClients = ngoServer.ConnectedClients;
            Assert.IsNull(connectedClients);
        });

        private static async UniTaskVoid UniTaskCancelInOneFrameAsync(CancellationTokenSource cts)
        {
            await UniTask.Yield();
            cts.Cancel();
        }
    }
}
