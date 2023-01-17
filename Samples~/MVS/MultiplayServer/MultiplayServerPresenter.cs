using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Multiplay.NGO.MVS.Common;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayServer
{
    public class MultiplayServerPresenter : IInitializable, IStartable, IDisposable
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MultiplayServerPresenter));

        private NgoServer ngoServer;

        public MultiplayServerPresenter(NgoServer ngoServer) => this.ngoServer = ngoServer;

        private CompositeDisposable compositeDisposable = new CompositeDisposable();

        public void Initialize()
        {
            ngoServer.OnServerStarted.Subscribe(_ => ngoServer.RegisterMessageHandler(MessageName.PlayerSpawn.ToString(), PlayerSpawnMessageHandler)).AddTo(compositeDisposable);

            ngoServer.OnServerStopping.Subscribe(_ => ngoServer.UnregisterMessageHandler(MessageName.PlayerSpawn.ToString())).AddTo(compositeDisposable);
        }

        private async void PlayerSpawnMessageHandler(ulong clientId, FastBufferReader messageStream)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{MessageName.PlayerSpawn}: {clientId}");
            }

            var result = Addressables.LoadAssetAsync<GameObject>("PlayerPrefab");
            var playerPrefab = await result.Task;
            ngoServer.SpawnAsPlayerObject(clientId, playerPrefab);
        }

        public void Start() => ngoServer.StartServerAsync().Forget();

        public void Dispose() => compositeDisposable.Dispose();
    }
}
