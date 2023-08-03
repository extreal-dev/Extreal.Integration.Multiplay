using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS2.App;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.SpaceControl
{
    public class SpaceControlPresenter : DisposableBase, IInitializable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly SpaceControlView spaceControlView;

        public SpaceControlPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            SpaceControlView spaceControlView)
        {
            this.stageNavigator = stageNavigator;
            this.spaceControlView = spaceControlView;
        }

        public void Initialize()
            => spaceControlView.OnBackButtonClicked
                .Subscribe(_ => stageNavigator.ReplaceAsync(StageName.GroupSelectionStage).Forget())
                .AddTo(disposables);

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
