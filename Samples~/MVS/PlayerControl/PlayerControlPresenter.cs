using System;
using Extreal.Integration.Multiplay.NGO.MVS.Common;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS.PlayerControl
{
    public class PlayerControlPresenter : IInitializable, IDisposable
    {
        [Inject] private NgoClient ngoClient;

        private CompositeDisposable compositeDisposable = new CompositeDisposable();

        public void Initialize() =>
            ngoClient.OnConnected.Subscribe(_ =>
            {
                var messageStream = new FastBufferWriter(FixedString64Bytes.UTF8MaxLengthInBytes, Allocator.Temp);
                ngoClient.SendMessage(MessageName.PlayerSpawn.ToString(), messageStream);
            }).AddTo(compositeDisposable);

        public void Dispose() => compositeDisposable.Dispose();
    }
}
