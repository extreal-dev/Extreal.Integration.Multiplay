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
        [SerializeField] private NetworkObject networkObjectPrefab;

        private INgoServer ngoServer;
        private ServerMessagingHub serverMessagingHub;

#pragma warning disable CC0068
        private void Awake()
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            var networkManager = FindObjectOfType<NetworkManager>();
            ngoServer = new NgoServer(networkManager);
            serverMessagingHub = new ServerMessagingHub(ngoServer);

            ngoServer.SetConnectionApproval((request, response)
                => response.Approved = request.Payload.SequenceEqual(Array.Empty<byte>())
                                        || request.Payload.SequenceEqual(new byte[] { 3, 7, 7, 6 }));
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
        {
#pragma warning disable CC0120
#pragma warning disable IDE0010
            switch (serverMessagingHub.ReceivedMessageName)
            {
                case MessageName.SPAWN_PLAYER_TO_SERVER:
                {
                    _ = NgoSpawnHelper.SpawnAsPlayerObject(serverMessagingHub.ReceivedClientId, networkObjectPrefab.gameObject);
                    break;
                }
                case MessageName.HELLO_WORLD_TO_SERVER:
                {
                    _ = serverMessagingHub.SendHelloWorldToAllClients();
                    break;
                }
            }
#pragma warning disable IDE0010
#pragma warning disable CC0120
        }
    }
}
