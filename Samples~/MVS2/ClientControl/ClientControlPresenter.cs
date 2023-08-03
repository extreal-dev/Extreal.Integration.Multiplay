using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.ClientControl
{
    public class ClientControlPresenter : DisposableBase, IInitializable
    {
        private readonly NgoClient ngoClient;
        private readonly NgoServer ngoServer;
        private readonly IConnectionSetter connectionSetter;

        [SuppressMessage("CodeCracker", "CC0057")]
        public ClientControlPresenter
        (
            NgoClient ngoClient,
            NgoServer ngoServer,
            IConnectionSetter connectionSetter
        )
        {
            this.ngoClient = ngoClient;
            this.ngoServer = ngoServer;
            this.connectionSetter = connectionSetter;
        }

        public void Initialize()
        {
            InitializeNgoServer();
            InitializeNgoClient();
        }

        private void InitializeNgoServer()
            => ngoServer.AddConnectionSetter(connectionSetter);

        private void InitializeNgoClient()
            => ngoClient.AddConnectionSetter(connectionSetter);
    }
}
