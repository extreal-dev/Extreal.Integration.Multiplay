using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class UNetTransportServer : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private INgoServer ngoServer;

#pragma warning disable CC0068
        private void Awake()
        {
            LoggingManager.Initialize(Core.Logging.LogLevel.Debug);

            ngoServer = new NgoServer(networkManager);
        }

        private void OnDestroy()
            => ngoServer.Dispose();

        private void Start()
            => ngoServer.StartServerAsync().Forget();
#pragma warning restore CC0068
    }
}
