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
    public class NgoServerTestSubSecond
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

            await SceneManager.LoadSceneAsync("Main");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoClient = new NgoClient(networkManager);

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
        public IEnumerator SendMessageToAllClientsSubSecond() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageName.HELLO_WORLD_TO_ALL_CLIENTS, clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            await UniTask.Delay(TimeSpan.FromSeconds(1));
            clientMassagingHub.SendHelloWorldToServer();
        });

        [UnityTest]
        public IEnumerator SpawnWithClientOwnershipSubSecond() => UniTask.ToCoroutine(async () =>
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
        public IEnumerator SpawnAsPlayerObjectSubSecond()
            => SpawnWithClientOwnershipSubSecond();
    }
}
