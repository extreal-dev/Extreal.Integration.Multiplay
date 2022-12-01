using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS.App;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayControl
{
    public class MultiplayControlPresenter : IInitializable, IDisposable
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

        public void Dispose()
        {
            compositeDisposable.Dispose();
            GC.SuppressFinalize(this);
        }

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
