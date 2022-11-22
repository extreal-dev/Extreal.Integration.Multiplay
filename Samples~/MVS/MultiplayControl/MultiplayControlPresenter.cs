using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS.App;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayControl
{
    public class MultiplayControlPresenter : IInitializable, IDisposable
    {
        [Inject] private IStageNavigator<StageName> stageNavigator;
        [Inject] private NgoClient ngoClient;

        public void Initialize() => stageNavigator.OnStageTransitioned += OnStageTransitioned;

        public void Dispose() => stageNavigator.OnStageTransitioned -= OnStageTransitioned;

        public void OnStageTransitioned(StageName stageName)
        {
            if (AppUtils.IsSpace(stageName))
            {
                var ngoConfig = new NgoConfig();
                ngoClient.ConnectAsync(ngoConfig).Forget();
            }
            else
            {
                ngoClient.DisconnectAsync().Forget();
            }

        }
    }
}
