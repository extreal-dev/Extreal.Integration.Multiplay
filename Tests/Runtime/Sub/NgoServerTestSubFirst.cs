using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class NgoServerTestSubFirst
    {
        private INgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingHub clientMassagingHub;

        private bool onUnexpectedDisconnected;
        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();

            ngoClient = new NgoClient(networkManager);
            onUnexpectedDisconnected = false;

            _ = ngoClient.OnUnexpectedDisconnected
                .Subscribe(_ => onUnexpectedDisconnected = true)
                .AddTo(disposables);

            clientMassagingHub = new ClientMessagingHub(ngoClient);
            onMessageReceived = false;

            _ = clientMassagingHub.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            clientMassagingHub.Dispose();
            ngoClient.Dispose();
            disposables.Clear();
            await UniTask.Yield();
        });

        [OneTimeTearDown]
        public void OneTimeDispose()
            => disposables.Dispose();

        [UnityTest]
        public IEnumerator StartServerWithConnectionApprovalSub() => UniTask.ToCoroutine(async () =>
        {
            var failedConnectionConfig = new ConnectionConfig(connectionData: new byte[] { 1, 2, 3, 4 }, timeoutSeconds: 1);

            Exception exception = null;
            try
            {
                await ngoClient.ConnectAsync(failedConnectionConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);

            var successConnectionConfig = new ConnectionConfig(connectionData: new byte[] { 3, 7, 7, 6 });
            await ngoClient.ConnectAsync(successConnectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectAndDisconnectClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator RemoveClientSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_CLIENT, clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            clientMassagingHub.SendHelloWorldToServer();
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClientsSubFirst() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_ALL_CLIENTS, clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            clientMassagingHub.SendHelloWorldToServer();
        });

        [UnityTest]
        public IEnumerator SpawnWithServerOwnershipSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsFalse(networkObject.IsOwner);

            await ngoClient.DisconnectAsync();

            await ngoClient.ConnectAsync(connectionConfig);
            foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);

            await UniTask.WaitUntil(() => onMessageReceived);

            foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject == null);
        });

        [UnityTest]
        public IEnumerator SpawnWithClientOwnershipSubFirst() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsTrue(networkObject.IsOwner);

            await UniTask.WaitUntil(() => onMessageReceived);
        });

        [UnityTest]
        public IEnumerator VisibilitySub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectA = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectA == null);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectB = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectB != null);

            await ngoClient.DisconnectAsync();
            await ngoClient.ConnectAsync(connectionConfig);

            var foundNetworkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
            Assert.IsTrue(foundNetworkObjects.Length == 2);
        });

        [UnityTest]
        public IEnumerator SpawnAsPlayerObjectSubFirst()
            => SpawnWithClientOwnershipSubFirst();
    }
}
