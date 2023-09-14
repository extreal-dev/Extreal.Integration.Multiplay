using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.NGO.MVS2.App;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.GroupSelectionScreen
{
    public class GroupSelectionScreenPresenter : DisposableBase, IInitializable
    {
        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly AppState appState;
        private readonly GroupSelectionScreenView groupSelectionScreenView;
        private readonly GroupProvider groupProvider;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public GroupSelectionScreenPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            GroupSelectionScreenView groupSelectionScreenView,
            GroupProvider groupProvider)
        {
            this.stageNavigator = stageNavigator;
            this.appState = appState;
            this.groupSelectionScreenView = groupSelectionScreenView;
            this.groupProvider = groupProvider;
        }

        public void Initialize()
        {
            groupSelectionScreenView.OnRoleChanged
                .Subscribe(appState.SetRole)
                .AddTo(disposables);

            groupSelectionScreenView.OnGroupNameChanged
                .Subscribe(appState.SetGroupName)
                .AddTo(disposables);

            groupSelectionScreenView.OnGroupChanged
                .Subscribe((groupName) => appState.SetGroupId(groupProvider.FindByName(groupName)?.Id))
                .AddTo(disposables);

            groupSelectionScreenView.OnUpdateButtonClicked
                .Subscribe(async _ => await groupProvider.UpdateGroupsAsync())
                .AddTo(disposables);

            groupSelectionScreenView.OnGoButtonClicked
                .Subscribe(_ => stageNavigator.ReplaceAsync(StageName.VirtualStage).Forget())
                .AddTo(disposables);

            groupProvider.OnGroupsUpdated
                .Subscribe(groups =>
                {
                    var groupNames = groups.Select(group => group.Name).ToArray();
                    groupSelectionScreenView.UpdateGroupNames(groupNames);
                    appState.SetGroupId(
                        groups.Count > 0 ? groupProvider.FindByName(groupNames.FirstOrDefault()).Id : null);
                })
                .AddTo(disposables);

            stageNavigator.OnStageTransitioned
                .Subscribe(_ =>
                {
                    groupSelectionScreenView.Initialize();
                    groupSelectionScreenView.SetInitialValues(appState.IsHost ? PeerRole.Host : PeerRole.Client);
                })
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
