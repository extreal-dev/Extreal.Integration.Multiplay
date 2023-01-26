using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS.App;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayControl
{
    public class MultiplayControlPresenter : DisposableBase, IInitializable
    {
        private StageNavigator<StageName, SceneName> stageNavigator;
        private NgoClient ngoClient;

        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();

        public MultiplayControlPresenter(StageNavigator<StageName, SceneName> stageNavigator, NgoClient ngoClient)
        {
            this.stageNavigator = stageNavigator;
            this.ngoClient = ngoClient;
        }

        public void Initialize() =>
            stageNavigator.OnStageTransitioned
                .Subscribe(OnStageTransitioned)
                .AddTo(compositeDisposable);

        protected override void ReleaseManagedResources() => compositeDisposable.Dispose();

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
