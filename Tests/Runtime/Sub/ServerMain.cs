using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ServerMain : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private INgoServer ngoServer;
        private ServerMessagingHub serverMessagingHub;

#pragma warning disable CC0068
        private void Awake()
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            var networkManager = FindObjectOfType<NetworkManager>();
            ngoServer = new NgoServer(networkManager);
            serverMessagingHub = new ServerMessagingHub(ngoServer);

            ngoServer.SetConnectionApproval((_, connectionData) => connectionData.SequenceEqual(Array.Empty<byte>()) || connectionData.SequenceEqual(new byte[] { 3, 7, 7, 6 }));
        }

        private void OnDestroy()
        {
            serverMessagingHub.Dispose();
            ngoServer.Dispose();
        }

        private void OnEnable()
            => serverMessagingHub.OnMessageReceived += OnMessageReceivedEventHandler;

        private void OnDisable()
            => serverMessagingHub.OnMessageReceived -= OnMessageReceivedEventHandler;

        private void Start()
            => ngoServer.StartServerAsync().Forget();
#pragma warning restore CC0068

        private void OnMessageReceivedEventHandler()
            => serverMessagingHub.SendHelloWorldToAllClients();
    }
}
