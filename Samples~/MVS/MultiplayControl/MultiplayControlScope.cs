using Extreal.Core.Common.Retry;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayControl
{
    public class MultiplayControlScope : LifetimeScope
    {
        [SerializeField] private NetworkManager networkManager;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(networkManager);
            builder.Register<NgoClient>(Lifetime.Singleton).WithParameter(typeof(IRetryStrategy), NoRetryStrategy.Instance);

            builder.RegisterEntryPoint<MultiplayControlPresenter>();
        }
    }
}
