using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class NgoServerTestSub
    {
        private INgoClient ngoClient;
        private NetworkManager networkManager;
        private ClientMessagingHub clientMassagingHub;

        private bool onUnexpectedDisconnected;
        private bool onMessageReceived;

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            await SceneManager.LoadSceneAsync("TestNgoMain");

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
            ngoClient = new NgoClient(networkManager);
            onUnexpectedDisconnected = false;
            ngoClient.OnUnexpectedDisconnected += OnUnexpectedDisconnectedEventHandler;

            clientMassagingHub = new ClientMessagingHub(ngoClient);
            onMessageReceived = false;
            clientMassagingHub.OnMessageReceived += OnMessageReceivedEventHandler;
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            clientMassagingHub.OnMessageReceived -= OnMessageReceivedEventHandler;
            ngoClient.OnUnexpectedDisconnected -= OnUnexpectedDisconnectedEventHandler;

            clientMassagingHub.Dispose();
            ngoClient.Dispose();

            await UniTask.Yield();
        });

        [UnityTest]
        public IEnumerator StartServerWithConnectionApprovalSub() => UniTask.ToCoroutine(async () =>
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

            var successConnectionData = new ConnectionData();
            successConnectionData.SetData(new byte[] { 3, 7, 7, 6 });
            var successConnectionParameter = new ConnectionParameter(successConnectionData);
            await ngoClient.ConnectAsync(successConnectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator ConnectAndDisconnectClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);
        });

        [UnityTest]
        public IEnumerator RemoveClientSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onUnexpectedDisconnected);
        });

        [UnityTest]
        public IEnumerator SendMessageToClientsSub() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageNameConst.HELLO_WORLD_TO_CLIENT.ToString(), clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            clientMassagingHub.SendHelloWorldToServer();
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClientsSubFirst() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString(), clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            clientMassagingHub.SendHelloWorldToServer();
        });

        [UnityTest]
        public IEnumerator SendMessageToAllClientsSubSecond() => UniTask.ToCoroutine(async () =>
        {
            var connectionParameter = new ConnectionParameter();
            await ngoClient.ConnectAsync(connectionParameter);
            Assert.IsTrue(ngoClient.IsConnected);

            await UniTask.WaitUntil(() => onMessageReceived);
            Assert.AreEqual(MessageNameConst.HELLO_WORLD_TO_ALL_CLIENTS.ToString(), clientMassagingHub.ReceivedMessageName);
            Assert.AreEqual("Hello World", clientMassagingHub.ReceivedMessageText);

            await UniTask.Delay(TimeSpan.FromSeconds(1));
            clientMassagingHub.SendHelloWorldToServer();
        });

        private void OnUnexpectedDisconnectedEventHandler()
            => onUnexpectedDisconnected = true;

        private void OnMessageReceivedEventHandler()
            => onMessageReceived = true;
    }
}
