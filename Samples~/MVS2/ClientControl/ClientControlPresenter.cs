using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using Extreal.Integration.Multiplay.NGO.MVS2.App;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.ClientControl
{
    public class ClientControlPresenter : DisposableBase, IInitializable
    {
        private readonly AppState appState;
        private readonly NgoClient ngoClient;
        private readonly NgoServer ngoServer;
        private readonly IConnectionSetter connectionSetter;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [SuppressMessage("CodeCracker", "CC0057")]
        public ClientControlPresenter
        (
            AppState appState,
            NgoClient ngoClient,
            NgoServer ngoServer,
            IConnectionSetter connectionSetter
        )
        {
            this.appState = appState;
            this.ngoClient = ngoClient;
            this.ngoServer = ngoServer;
            this.connectionSetter = connectionSetter;
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public void Initialize()
        {
            InitializeNgoServer();
            InitializeNgoClient();
        }

        private void InitializeNgoServer()
            => ngoServer.AddConnectionSetter(connectionSetter);

        private void InitializeNgoClient()
        {
            ngoClient.AddConnectionSetter(connectionSetter);

            ngoClient.OnConnectionApprovalRejected
                .Subscribe(_ => appState.Notify("Space is full"))
                .AddTo(disposables);

            ngoClient.OnUnexpectedDisconnected
                .Subscribe(_ => appState.Notify("Unexpcted error has occurred"))
                .AddTo(disposables);
        }
    }
}
