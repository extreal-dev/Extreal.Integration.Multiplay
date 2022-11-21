using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.PlayerControl
{
    public class PlayerControlScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
            => builder.RegisterEntryPoint<PlayerControlPresenter>();
    }
}
