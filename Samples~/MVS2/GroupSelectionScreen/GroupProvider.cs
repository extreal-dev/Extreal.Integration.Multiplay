using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Integration.P2P.WebRTC;
using UniRx;

namespace Extreal.Integration.Multiplay.NGO.MVS2.GroupSelectionScreen
{
    public class GroupProvider : DisposableBase
    {
        public IObservable<List<Group>> OnGroupsUpdated => groups.AddTo(disposables).Skip(1);
        [SuppressMessage("Usage", "CC0033")]
        private readonly ReactiveProperty<List<Group>> groups = new ReactiveProperty<List<Group>>(new List<Group>());

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly PeerClient peerClient;

        [SuppressMessage("CodeCracker", "CC0057")]
        public GroupProvider(PeerClient peerClient) => this.peerClient = peerClient;

        protected override void ReleaseManagedResources() => disposables.Dispose();

        public async UniTask UpdateGroupsAsync()
        {
            var hosts = await peerClient.ListHostsAsync();
            groups.Value = hosts.Select(host => new Group(host.Id, host.Name)).ToList();
        }

        public Group FindByName(string name) => groups.Value.First(groups => groups.Name == name);
    }
}
