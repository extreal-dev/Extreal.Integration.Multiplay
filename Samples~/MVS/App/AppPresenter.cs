using Cysharp.Threading.Tasks;
using Extreal.Core.StageNavigation;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public class AppPresenter : IStartable
    {
        private StageNavigator<StageName, SceneName> stageNavigator;

        public AppPresenter(StageNavigator<StageName, SceneName> stageNavigator)
            => this.stageNavigator = stageNavigator;

        public void Start() => stageNavigator.ReplaceAsync(StageName.VirtualStage).Forget();
    }
}
