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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoServerTest
    {
        private NgoServer ngoServer;
        private NetworkManager networkManager;
        private NetworkObject networkObjectPrefab;
        private ServerMessagingManager serverMessagingManager;

        private bool onServerStarted;
        private bool onServerStopping;

        private bool onClientConnected;
        private ulong connectedClientId;

        private bool onClientDisconnecting;
        private ulong disconnectingClientId;

        private bool onClientRemoving;
        private ulong removingClientId;

        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            var networkObjectProvider = UnityEngine.Object.FindObjectOfType<NetworkObjectProvider>();
            networkObjectPrefab = networkObjectProvider.NetworkObject;

            ngoServer = new NgoServer(networkManager);

            onServerStarted = false;
            onServerStopping = false;
            onClientConnected = false;
            connectedClientId = 0;
            onClientDisconnecting = false;
            disconnectingClientId = 0;
            onClientRemoving = false;
            removingClientId = 0;

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
                .Subscribe(clientId =>
                {
                    onClientRemoving = true;
                    removingClientId = clientId;
                })
                .AddTo(disposables);

            serverMessagingManager = new ServerMessagingManager(ngoServer);
            onMessageReceived = false;

            _ = serverMessagingManager.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            serverMessagingManager.Dispose();
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
        public IEnumerator GetConnectedClientsWithNetworkManagerNull() => UniTask.ToCoroutine(async () =>
        {
            UnityEngine.Object.Destroy(networkManager.gameObject);
            await UniTask.Yield();
            var connectedClients = ngoServer.ConnectedClients;
            Assert.IsEmpty(connectedClients);
        });

        [UnityTest]
        public IEnumerator SetConnectionApprovalCallbackWithConnectionApprovalFalse() => UniTask.ToCoroutine(async () =>
        {
            networkManager.NetworkConfig.ConnectionApproval = false;
            ngoServer.SetConnectionApprovalCallback((_, _) => { return; });
            await ngoServer.StartServerAsync();
            LogAssert.Expect(LogType.Warning, "[Netcode] A ConnectionApproval callback is defined but ConnectionApproval is disabled. In order to use ConnectionApproval it has to be explicitly enabled ");
        });

        [UnityTest]
        public IEnumerator StartServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsFalse(onServerStarted);
            await ngoServer.StartServerAsync();
            Assert.IsTrue(onServerStarted);
            Assert.IsTrue(networkManager.IsServer);
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
            Assert.IsTrue(networkManager.IsServer);

            await UniTask.WaitUntil(() => connectionApproved);

            Assert.IsFalse(onClientConnected);

            await UniTask.WaitUntil(() => onClientConnected);
            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator StartServerTwice() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

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
            Assert.IsTrue(networkManager.IsServer);

            Assert.IsFalse(onServerStopping);
            await ngoServer.StopServerAsync();
            Assert.IsTrue(onServerStopping);
            Assert.IsFalse(networkManager.IsServer);
        });

        [UnityTest]
        public IEnumerator RemoveClientSuccess() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            await UniTask.WaitUntil(() => onClientConnected);
            await UniTask.DelayFrame(10);


            var result = ngoServer.RemoveClient(connectedClientId);
            Assert.IsTrue(result);
            Assert.IsTrue(onClientRemoving);
            Assert.IsFalse(onClientDisconnecting);
            Assert.AreEqual(connectedClientId, removingClientId);
        });

        [Test]
        public void RemoveClientWithoutConnect()
            => Assert.That(() => ngoServer.RemoveClient(0),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This server is not running"));

        [UnityTest]
        public IEnumerator RemoveNotExistedClient() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const ulong notExistedClientId = 10;
            var result = ngoServer.RemoveClient(notExistedClientId);
            Assert.IsFalse(result);
            LogAssert.Expect(LogType.Log, $"[{Core.Logging.LogLevel.Debug}:{nameof(NgoServer)}] Unable to remove client with client id {notExistedClientId} because it does not exist");
        });

        [UnityTest]
        public IEnumerator SendMessageToClients() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            await UniTask.WaitUntil(() => onClientConnected);

            Assert.IsFalse(onMessageReceived);
            var clientIds = new List<ulong> { connectedClientId };
            serverMessagingManager.SendHelloWorldToClients(clientIds);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(connectedClientId, serverMessagingManager.ReceivedClientId);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_SERVER, serverMessagingManager.ReceivedMessageName);
            Assert.AreEqual("Hello World", serverMessagingManager.ReceivedMessageText);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [Test]
        public void SendMessageToClientsWithoutConnect()
            => Assert.That(() => serverMessagingManager.SendHelloWorldToClients(new List<ulong> { 10 }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This server is not running"));

        [UnityTest]
        public IEnumerator SendMessageToClientsWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const string nullMessageName = null;
            var clientIds = new List<ulong> { 0 };
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            Assert.That(() => ngoServer.SendMessageToClients(clientIds, nullMessageName, messageStream),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsWithMessageStreamNotInitialized() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const string messageName = "TestMessage";
            var clientIds = new List<ulong>();
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => ngoServer.SendMessageToClients(clientIds, messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream must be initialized"));
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsWithNotExistedClientId() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const string messageName = "TestMessage";
            var notExistedClientIds = new List<ulong> { 10 };
            var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
            ngoServer.SendMessageToClients(notExistedClientIds, messageName, messageStream);
            LogAssert.Expect(LogType.Log, $"[{Core.Logging.LogLevel.Debug}:{nameof(NgoServer)}] clientIds contains some ids that does not exist: " + string.Join(", ", notExistedClientIds));
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClients() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            await UniTask.WaitUntil(() => onClientConnected);

            Assert.IsFalse(onMessageReceived);
            serverMessagingManager.SendHelloWorldToAllClients();

            await UniTask.WaitUntil(() => onMessageReceived);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [Test]
        public void SendToAllClientsWithoutConnect()
            => Assert.That(() => serverMessagingManager.SendHelloWorldToAllClients(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This server is not running"));

        [UnityTest]
        public IEnumerator SendMessageToAllClientsWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

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
            Assert.IsTrue(networkManager.IsServer);

            const string messageName = "TestMessage";
            var notInitializedMessageStream = new FastBufferWriter();
            Assert.That(() => ngoServer.SendMessageToAllClients(messageName, notInitializedMessageStream),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("messageStream must be initialized"));
        });

        [Test]
        public void RegisterMessageHandlerWithoutConnect()
            => Assert.That(() => ngoServer.RegisterMessageHandler("TestMessage", (_, _) => { return; }),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This server is not running"));

        [UnityTest]
        public IEnumerator RegisterMessageHandlerWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const string nullMessageName = null;
            Assert.That(() => ngoServer.RegisterMessageHandler(nullMessageName, (_, _) => { return; }),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [Test]
        public void UnregisterMessageHandlerWithoutConnect()
            => Assert.That(() => ngoServer.UnregisterMessageHandler("TestMessage"),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("This server is not running"));

        [UnityTest]
        public IEnumerator UnregisterMessageHandlerWithMessageNameNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            const string nullMessageName = null;
            Assert.That(() => ngoServer.UnregisterMessageHandler(nullMessageName),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("messageName"));
        });

        [UnityTest]
        public IEnumerator UnregisterMessageHandlerWithoutRegister() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.IsTrue(networkManager.IsServer);

            ngoServer.UnregisterMessageHandler("TestMessage");
        });

        [UnityTest]
        public IEnumerator SpawnWithServerOwnership() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instance = ngoServer.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.DelayFrame(10);
            serverMessagingManager.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsTrue(networkObject.IsOwner);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator SpawnWithClientOwnership() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);

            var instance = ngoServer.SpawnWithClientOwnership(connectedClientId, networkObjectPrefab.gameObject);
            await UniTask.DelayFrame(10);
            serverMessagingManager.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.AreEqual(connectedClientId, networkObject.OwnerClientId);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator SpawnAsPlayerObject() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);

            var instance = ngoServer.SpawnAsPlayerObject(connectedClientId, networkObjectPrefab.gameObject);
            await UniTask.DelayFrame(10);
            serverMessagingManager.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsNotNull(ngoServer.ConnectedClients[connectedClientId].PlayerObject);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [Test]
        public void SpawnWithServerOwnershipWithoutStartServer()
            => Assert.That(() => ngoServer.SpawnWithServerOwnership(new GameObject()),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to spawn objects because this server is not listening"));

        [UnityTest]
        public IEnumerator Visibility() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            var count = 0;
            ngoServer.SetVisibilityDelegate(clientId =>
            {
                if (count++ == 0)
                {
                    return false;
                }
                return true;
            });

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instanceA = ngoServer.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.DelayFrame(10);
            serverMessagingManager.SendHelloWorldToAllClients();

            var foundNetworkObjectA = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectA != null);

            await UniTask.DelayFrame(10);

            var instanceB = ngoServer.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.DelayFrame(10);
            serverMessagingManager.SendHelloWorldToAllClients();

            var foundNetworkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
            Assert.IsTrue(foundNetworkObjects.Length == 2);

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator SpawnWithPrefabNull() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.That(() => ngoServer.SpawnWithServerOwnership(null),
               Throws.TypeOf<ArgumentNullException>()
                   .With.Message.Contains("prefab"));
        });

        [UnityTest]
        public IEnumerator SpawnWithoutNetworkObject() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            Assert.That(() => ngoServer.SpawnWithServerOwnership(new GameObject()),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("GameObject without NetworkObject cannot be spawned"));
        });

        private static async UniTaskVoid UniTaskCancelInOneFrameAsync(CancellationTokenSource cts)
        {
            await UniTask.Yield();
            cts.Cancel();
        }
    }
}
