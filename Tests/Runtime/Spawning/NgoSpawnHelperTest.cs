using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Multiplay.NGO.Test.Sub;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoSpawnHelperTest
    {
        private NgoServer ngoServer;
        private NetworkObject networkObjectPrefab;
        private NetworkManager networkManager;
        private ServerMessagingHub serverMessagingHub;

        private bool onClientConnected;
        private ulong connectedClientId;
        private bool onClientDisconnecting;
        private ulong disconnectingClientId;
        private bool onMessageReceived;

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            var networkObjectProvider = UnityEngine.Object.FindObjectOfType<NetworkObjectProvider>();
            networkObjectPrefab = networkObjectProvider.NetworkObject;

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoServer = new NgoServer(networkManager);
            onClientConnected = false;
            connectedClientId = 0;
            onClientDisconnecting = false;
            disconnectingClientId = 0;

            ngoServer.OnClientConnected += OnClientConnectedEventHandler;
            ngoServer.OnClientDisconnecting += OnClientDisconnectingEventHandler;

            serverMessagingHub = new ServerMessagingHub(ngoServer);
            onMessageReceived = false;

            serverMessagingHub.OnMessageReceived += OnMessageReceivedEventHandler;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            serverMessagingHub.OnMessageReceived -= OnMessageReceivedEventHandler;
            ngoServer.OnClientConnected -= OnClientConnectedEventHandler;
            ngoServer.OnClientDisconnecting -= OnClientDisconnectingEventHandler;

            serverMessagingHub.Dispose();
            ngoServer.Dispose();

            await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
            UnityEngine.Object.Destroy(networkManager.gameObject);
        });

        [UnityTest]
        public IEnumerator SpawnWithServerOwnership() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instance = NgoSpawnHelper.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsTrue(networkObject.IsOwner);

            await UniTask.WaitUntil(() => onClientConnected);
            onClientDisconnecting = false;
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            networkObject.Despawn();
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator SpawnWithClientOwnership() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;
            var firstConnectedClientId = connectedClientId;
            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instance = NgoSpawnHelper.SpawnWithClientOwnership(firstConnectedClientId, networkObjectPrefab.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.AreEqual(firstConnectedClientId, networkObject.OwnerClientId);

            await UniTask.WaitUntil(() => onClientConnected);
            onClientDisconnecting = false;
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _ = serverMessagingHub.SendHelloWorldToClients(new List<ulong> { firstConnectedClientId });

            await UniTask.WaitUntil(() => onClientDisconnecting);
            onClientDisconnecting = false;
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _ = serverMessagingHub.SendHelloWorldToAllClients();

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator SpawnAsPlayerObject() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;
            var firstConnectedClientId = connectedClientId;
            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instance = NgoSpawnHelper.SpawnAsPlayerObject(firstConnectedClientId, networkObjectPrefab.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            var foundNetworkObject = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObject != null);
            Assert.AreSame(instance, foundNetworkObject);
            var networkObject = foundNetworkObject.GetComponent<NetworkObject>();
            Assert.IsTrue(networkObject != null);
            Assert.IsNotNull(ngoServer.ConnectedClients[firstConnectedClientId].PlayerObject);

            await UniTask.WaitUntil(() => onClientConnected);
            onClientDisconnecting = false;
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _ = serverMessagingHub.SendHelloWorldToClients(new List<ulong> { firstConnectedClientId });

            await UniTask.WaitUntil(() => onClientDisconnecting);
            onClientDisconnecting = false;
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            _ = serverMessagingHub.SendHelloWorldToAllClients();

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [UnityTest]
        public IEnumerator Visibility() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            var count = 0;
            NgoSpawnHelper.SetVisibilityDelegate(clientId =>
            {
                if (count++ == 0)
                {
                    return false;
                }
                return true;
            });

            await UniTask.WaitUntil(() => onClientConnected);
            onClientConnected = false;

            var instanceA = NgoSpawnHelper.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            var foundNetworkObjectA = GameObject.Find("NetworkPlayer(Clone)");
            Assert.IsTrue(foundNetworkObjectA != null);

            await UniTask.Delay(TimeSpan.FromSeconds(1));

            var instanceB = NgoSpawnHelper.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _ = serverMessagingHub.SendHelloWorldToAllClients();

            var foundNetworkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
            Assert.IsTrue(foundNetworkObjects.Length == 2);

            await UniTask.WaitUntil(() => onClientConnected);
            onClientDisconnecting = false;

            await UniTask.WaitUntil(() => onClientDisconnecting);
        });

        [Test]
        public void SpawnWithPrefabNull()
        {
            var instance = NgoSpawnHelper.SpawnWithServerOwnership(null);
            Assert.IsTrue(instance == null);
            LogAssert.Expect(LogType.Warning, $"[{Core.Logging.LogLevel.Warn}:{nameof(NgoSpawnHelper)}] Null is accepted as prefab");
        }

        [Test]
        public void SpawnWithoutNetworkObject()
        {
            var instance = NgoSpawnHelper.SpawnWithServerOwnership(new GameObject());
            Assert.IsTrue(instance == null);
            LogAssert.Expect(LogType.Warning, $"[{Core.Logging.LogLevel.Warn}:{nameof(NgoSpawnHelper)}] GameObject without NetworkObject cannot be spawned");
        }

        private void OnClientConnectedEventHandler(ulong clientId)
        {
            onClientConnected = true;
            connectedClientId = clientId;
        }

        private void OnClientDisconnectingEventHandler(ulong clientId)
        {
            onClientDisconnecting = true;
            disconnectingClientId = clientId;
        }

        private void OnMessageReceivedEventHandler()
            => onMessageReceived = true;
    }
}
