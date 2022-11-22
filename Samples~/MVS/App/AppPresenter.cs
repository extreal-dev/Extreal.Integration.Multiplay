using Extreal.Core.StageNavigation;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public class AppPresenter : IStartable
    {
        [Inject] private IStageNavigator<StageName> stageNavigator;

        public void Start() => stageNavigator.ReplaceAsync(StageName.VirtualSpace);
    }
}
