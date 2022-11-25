using Extreal.Core.StageNavigation;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public class AppPresenter : IStartable
    {
        private IStageNavigator<StageName> stageNavigator;

        public AppPresenter(IStageNavigator<StageName> stageNavigator)
            => this.stageNavigator = stageNavigator;

        public void Start() => stageNavigator.ReplaceAsync(StageName.VirtualStage);
    }
}
