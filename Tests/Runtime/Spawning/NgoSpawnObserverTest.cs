using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoSpawnObserverTest
    {
        private INgoSpawnObserver ngoSpawnObserver;
        private NgoServer ngoServer;
        private NetworkObject networkObjectPrefab;
        private NetworkManager networkManager;

        private bool onSpawned;
        private NetworkObject[] onSpawnedObjects;
        private bool onDespawned;
        private ulong[] onDespawnedObjects;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            var networkObjectProvider = UnityEngine.Object.FindObjectOfType<NetworkObjectProvider>();
            networkObjectPrefab = networkObjectProvider.NetworkObject;

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoServer = new NgoServer(networkManager);

            ngoSpawnObserver = new NgoSpawnObserver();
            onSpawned = false;
            onSpawnedObjects = Array.Empty<NetworkObject>();
            onDespawned = false;
            onDespawnedObjects = Array.Empty<ulong>();

            _ = ngoSpawnObserver.OnSpawnedAsObservable
                .Subscribe(networkObjs =>
                {
                    onSpawned = true;
                    onSpawnedObjects = networkObjs;
                })
                .AddTo(disposables);

            _ = ngoSpawnObserver.OnDespawnedAsObservable
                .Subscribe(objIds =>
                {
                    onDespawned = true;
                    onDespawnedObjects = objIds;
                })
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            disposables.Clear();
            ngoSpawnObserver.Clear();
            ngoServer.Dispose();

            if (networkManager != null)
            {
                await UniTask.WaitUntil(() => !networkManager.ShutdownInProgress);
                UnityEngine.Object.Destroy(networkManager.gameObject);
            }
        });

        [OneTimeTearDown]
        public void OneTimeDispose()
        {
            disposables.Dispose();
            ngoSpawnObserver.Dispose();
        }

        [UnityTest]
        public IEnumerator StartAndSpawnDespawn() => UniTask.ToCoroutine(async () =>
        {
            await ngoServer.StartServerAsync();
            ngoSpawnObserver.Start();

            var instance = NgoSpawnHelper.SpawnWithServerOwnership(networkObjectPrefab.gameObject);
            var networkObject = instance.GetComponent<NetworkObject>();
            await UniTask.WaitUntil(() => onSpawned);

            Assert.IsTrue(onSpawnedObjects.Length == 1);
            Assert.AreSame(networkObject, onSpawnedObjects[0]);


            var spawnedObjId = networkObject.NetworkObjectId;
            networkObject.Despawn();
            await UniTask.WaitUntil(() => onDespawned);

            Assert.IsTrue(onDespawnedObjects.Length == 1);
            Assert.AreEqual(spawnedObjId, onDespawnedObjects[0]);
        });

        [UnityTest]
        public IEnumerator StartWithoutNetworkManager() => UniTask.ToCoroutine(async () =>
        {
            UnityEngine.Object.Destroy(networkManager.gameObject);
            await UniTask.Yield();
            Assert.That(() => ngoSpawnObserver.Start(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo($"{nameof(NetworkManager)} must exist in Scenes"));
        });

        [Test]
        public void StartWithoutServerStart()
            => Assert.That(() => ngoSpawnObserver.Start(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo($"{nameof(NetworkManager)} must be connected to server or listening before start"));
    }
}
