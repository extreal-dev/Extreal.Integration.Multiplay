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
    public class NgoServerTestSub
    {
        private NgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingManager clientMessagingManager;

        private bool onUnexpectedDisconnected;
        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));

            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();

            ngoClient = new NgoClient(networkManager);
            onUnexpectedDisconnected = false;

            _ = ngoClient.OnUnexpectedDisconnected
                .Subscribe(_ => onUnexpectedDisconnected = true)
                .AddTo(disposables);

            clientMessagingManager = new ClientMessagingManager(ngoClient);
            onMessageReceived = false;

            _ = clientMessagingManager.OnMessageReceived
                .Subscribe(_ => onMessageReceived = true)
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            clientMessagingManager.Dispose();
            ngoClient.Dispose();
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

        [UnityTest]
        public IEnumerator StartServerWithConnectionApprovalSub() => UniTask.ToCoroutine(async () =>
        {
            networkManager.NetworkConfig.ConnectionApproval = true;

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

            var successNgoConfig = new NgoConfig(connectionData: new byte[] { 3, 7, 7, 6 });
            _ = await ngoClient.ConnectAsync(successNgoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator ConnectAndDisconnectClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);
        });

        [UnityTest]
        public IEnumerator RemoveClientSuccessSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_CLIENT, clientMessagingManager.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMessagingManager.ReceivedMessageText);

            clientMessagingManager.SendHelloWorld();
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);
            Assert.IsTrue(networkManager.IsConnectedClient);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_ALL_CLIENTS, clientMessagingManager.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMessagingManager.ReceivedMessageText);

            clientMessagingManager.SendHelloWorld();
        });

        [UnityTest]
        public IEnumerator SpawnWithServerOwnershipSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsFalse(networkObject.IsOwner);
        });

        [UnityTest]
        public IEnumerator SpawnWithClientOwnershipSub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);

            await UniTask.WaitUntil(() => onMessageReceived);

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsTrue(networkObject.IsOwner);
        });

        [UnityTest]
        public IEnumerator SpawnAsPlayerObjectSub()
            => SpawnWithClientOwnershipSub();

        [UnityTest]
        public IEnumerator VisibilitySub() => UniTask.ToCoroutine(async () =>
        {
            var ngoConfig = new NgoConfig();
            _ = await ngoClient.ConnectAsync(ngoConfig);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectA = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectA == null);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectB = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectB != null);
        });
    }
}
