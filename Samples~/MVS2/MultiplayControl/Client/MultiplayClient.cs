using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Integration.Multiplay.NGO.MVS2.Common;
using UniRx;
using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.MVS2.Controls.MultiplyControl.Client
{
    public class MultiplayClient : DisposableBase
    {
        private readonly NgoClient ngoClient;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public MultiplayClient(NgoClient ngoClient)
        {
            this.ngoClient = ngoClient;

            this.ngoClient.OnConnected
                .Subscribe(_ =>
                {
                    var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
                    ngoClient.SendMessage(MessageName.PlayerSpawn.ToString(), messageStream);
                })
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public async UniTask JoinAsync()
        {
            var ngoConfig = new NgoConfig();
            await ngoClient.ConnectAsync(ngoConfig);
        }

        public UniTask LeaveAsync()
            => ngoClient.DisconnectAsync();
    }
}
