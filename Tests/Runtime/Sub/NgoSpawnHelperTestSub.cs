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
    public class NgoSpawnHelperTestSub
    {
        private INgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingHub clientMassagingHub;

        private bool onMessageReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            networkManager = Object.FindObjectOfType<NetworkManager>();
            var networkTransportInitializer = new UnityTransportInitializer();
            ngoClient = new NgoClient(networkManager, networkTransportInitializer);

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
        public IEnumerator SpawnWithServerOwnershipSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsFalse(networkObject.IsOwner);

            await ngoClient.DisconnectAsync();

            await ngoClient.ConnectAsync(connectionParameter);
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
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);

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
        public IEnumerator SpawnWithClientOwnershipSubSecond() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsFalse(networkObject.IsOwner);

            await ngoClient.DisconnectAsync();

            await ngoClient.ConnectAsync(connectionParameter);
            foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);

            await UniTask.WaitUntil(() => onMessageReceived);

            foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject == null);
        });

        [UnityTest]
        public IEnumerator VisibilitySub() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            var connectionParameter = new ConnectionParameter(connectionConfig);
            await ngoClient.ConnectAsync(connectionParameter);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectA = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectA == null);

            await UniTask.WaitUntil(() => onMessageReceived);
            onMessageReceived = false;

            var foundNetworkObjectB = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectB != null);

            await ngoClient.DisconnectAsync();
            await ngoClient.ConnectAsync(connectionParameter);

            var foundNetworkObjects = Object.FindObjectsOfType<NetworkObject>();
            Assert.IsTrue(foundNetworkObjects.Length == 2);
        });

        [UnityTest]
        public IEnumerator SpawnAsPlayerObjectSubFirst()
            => SpawnWithClientOwnershipSubFirst();

        [UnityTest]
        public IEnumerator SpawnAsPlayerObjectSubSecond()
            => SpawnWithClientOwnershipSubSecond();
    }
}
