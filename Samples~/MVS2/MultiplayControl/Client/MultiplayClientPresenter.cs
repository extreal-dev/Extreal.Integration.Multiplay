using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Integration.Multiplay.NGO.MVS2.App;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplyControl.Client
{
    public class MultiplayClientPresenter : DisposableBase, IInitializable
    {
        private readonly NgoClient ngoClient;
        private readonly AppState appState;
        private MultiplayClient multiplayClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public MultiplayClientPresenter
        (
            NgoClient ngoClient,
            AppState appState
        )
        {
            this.ngoClient = ngoClient;
            this.appState = appState;
        }

        public void Initialize()
        {
            multiplayClient = new MultiplayClient(ngoClient);
            disposables.Add(multiplayClient);

            appState.P2PReady
                .Where(ready => ready && appState.IsClient)
                .Subscribe(_ => multiplayClient.JoinAsync().Forget())
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();
    }
}
