using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using Extreal.Integration.Multiplay.NGO.MVS2.Common;

namespace Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplayControl.Host
{
    public class MultiplayHost : DisposableBase
    {
        private readonly NgoServer ngoServer;
        private readonly GameObject playerPrefab;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MultiplayHost));

        private const int MaxCapacity = 3;

        public MultiplayHost
        (
            NgoServer ngoServer,
            GameObject playerPrefab
        )
        {
            this.ngoServer = ngoServer;
            this.playerPrefab = playerPrefab;

            this.ngoServer.SetConnectionApprovalCallback((_, response) =>
                response.Approved = ngoServer.ConnectedClients.Count < MaxCapacity);

            this.ngoServer.OnServerStarted
                .Subscribe(_ =>
                    ngoServer.RegisterMessageHandler(MessageName.PlayerSpawn.ToString(), PlayerSpawnMessageHandler))
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public async UniTaskVoid StartHostAsync()
        {
            var ngoConfig = new NgoConfig();
            await ngoServer.StartHostAsync(ngoConfig);
        }

        public UniTask StopHostAsync() => ngoServer.StopServerAsync();

        private void PlayerSpawnMessageHandler(ulong clientId, FastBufferReader messageStream)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{MessageName.PlayerSpawn}: {clientId}");
            }

            ngoServer.SpawnAsPlayerObject(clientId, playerPrefab);
        }
    }
}
