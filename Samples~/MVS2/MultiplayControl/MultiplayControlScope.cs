using Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplayControl.Host;
using Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplyControl.Client;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplyClientControl
{
    public class MultiplayControlScope : LifetimeScope
    {
        [SerializeField] private GameObject playerPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MultiplayHostPresenter>().WithParameter(playerPrefab);
            builder.RegisterEntryPoint<MultiplayClientPresenter>();
        }
    }
}
