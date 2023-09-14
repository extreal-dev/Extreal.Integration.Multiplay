using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Integration.Multiplay.NGO.MVS2.App;
using UniRx;
using UnityEngine;
using Extreal.Core.Common.System;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplayControl.Host
{
    public class MultiplayHostPresenter : DisposableBase, IInitializable
    {
        private readonly NgoServer ngoServer;
        private readonly GameObject playerPrefab;
        private readonly AppState appState;

        private MultiplayHost multiplayHost;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public MultiplayHostPresenter
        (
            AppState appState,
            NgoServer ngoServer,
            GameObject playerPrefab
        )
        {
            this.appState = appState;
            this.ngoServer = ngoServer;
            this.playerPrefab = playerPrefab;
        }

        public void Initialize()
        {
            if (!appState.IsHost)
            {
                return;
            }

            multiplayHost = new MultiplayHost(ngoServer, playerPrefab);
            disposables.Add(multiplayHost);

            appState.P2PReady
                .Where(ready => ready)
                .Subscribe(_ => multiplayHost.StartHostAsync().Forget())
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
        {
            if (appState.IsHost)
            {
                multiplayHost.StopHostAsync().Forget();
            }
            disposables.Dispose();
        }
    }
}
