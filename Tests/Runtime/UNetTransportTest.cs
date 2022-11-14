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
    public class UNetTransportTest
    {
        private INgoClient ngoClient;
        private NetworkManager networkManager;

        private bool onConnected;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync(nameof(UNetTransportTest));

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();

            ngoClient = new NgoClient(networkManager);

            _ = ngoClient.OnConnected
                .Subscribe(_ => onConnected = true)
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
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
        public IEnumerator ConnectToServerSuccess() => UniTask.ToCoroutine(async () =>
        {
            var connectionConfig = ConnectionConfig.Default;

            Assert.IsFalse(onConnected);
            await ngoClient.ConnectAsync(connectionConfig);
            Assert.IsTrue(onConnected);
            Assert.IsTrue(ngoClient.IsRunning);
            Assert.IsTrue(ngoClient.IsConnected);
        });
    }
}
