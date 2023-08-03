using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.SpaceControl
{
    public class SpaceControlScope : LifetimeScope
    {
        [SerializeField] private SpaceControlView spaceControlView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(spaceControlView);

            builder.RegisterEntryPoint<SpaceControlPresenter>();
        }
    }
}
