using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UniRx;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

#pragma warning disable CC0068
        private void Awake()
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            ngoServer = new NgoServer(networkManager);
            serverMessagingHub = new ServerMessagingHub(ngoServer);

            ngoServer.SetConnectionApprovalCallback((request, response)
                => response.Approved = request.Payload.SequenceEqual(Array.Empty<byte>())
                                        || request.Payload.SequenceEqual(new byte[] { 3, 7, 7, 6 }));
        }

        private void OnDestroy()
        {
            serverMessagingHub.Dispose();
            ngoServer.Dispose();
            disposables.Dispose();
        }

        private void OnEnable()
            => serverMessagingHub.OnMessageReceived
                .Subscribe(arg =>
                {
#pragma warning disable CC0120
#pragma warning disable IDE0010
                    switch (serverMessagingHub.ReceivedMessageName)
                    {
                        case MessageName.RESTART_TO_SERVER:
                        {
                            RestartAsync().Forget();
                            break;
                        }
                        case MessageName.HELLO_WORLD_TO_SERVER:
                        {
                            serverMessagingHub.SendHelloWorldToAllClients();
                            break;
                        }
                    }
#pragma warning disable IDE0010
#pragma warning disable CC0120
                })
                .AddTo(disposables);

        private void OnDisable()
            => disposables.Clear();

        private void Start()
            => ngoServer.StartServerAsync().Forget();
#pragma warning restore CC0068

        private async UniTaskVoid RestartAsync()
        {
            await UniTask.DelayFrame(10);
            await ngoServer.StopServerAsync();
            await UniTask.DelayFrame(10);
            await ngoServer.StartServerAsync();
        }
    }
}
