using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS.App;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayControl
{
    public class MultiplayControlPresenter : IInitializable, IDisposable
    {
        private IStageNavigator<StageName> stageNavigator;
        private NgoClient ngoClient;

        public MultiplayControlPresenter(IStageNavigator<StageName> stageNavigator, NgoClient ngoClient)
        {
            this.stageNavigator = stageNavigator;
            this.ngoClient = ngoClient;
        }

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
