using System.Threading;
using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INetcodeClient : IDisposable
    {
        event Action<ulong> OnConnected;
        event Action<ulong> OnDisconnected;
        event Action OnBeforeDisconnect;

        bool IsRunning { get; }
        bool IsConnected { get; }

        UniTask<bool> ConnectAsync(ConnectionParameter connectParameter, CancellationToken token = default);
        void Disconnect();
        void SendMessageToServer(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void RegisterNamedMessage(string messageName, HandleNamedMessageDelegate namedMessageHandler);
        void UnregisterNamedMessage(string messageName);
    }
}
