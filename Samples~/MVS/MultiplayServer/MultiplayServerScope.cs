using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using LogLevel = Extreal.Core.Logging.LogLevel;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayServer
{
    public class MultiplayServerScope : LifetimeScope
    {
        [SerializeField] private NetworkManager networkManager;

        private static void InitializeApp()
        {
            const LogLevel logLevel = LogLevel.Debug;
            LoggingManager.Initialize(logLevel: logLevel);
        }

        protected override void Awake()
        {
            InitializeApp();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(networkManager);
            builder.Register<NgoServer>(Lifetime.Singleton);

            builder.RegisterEntryPoint<MultiplayServerPresenter>();
        }
    }
}
