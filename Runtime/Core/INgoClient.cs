using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INgoClient : IDisposable
    {
        event Action OnConnected;
        event Action OnDisconnecting;
        event Action OnUnexpectedDisconnected;

        bool IsRunning { get; }
        bool IsConnected { get; }

        UniTask ConnectAsync(ConnectionParameter connectParameter, CancellationToken token = default);
        UniTask DisconnectAsync();
        void SendMessage(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate namedMessageHandler);
        void UnregisterMessageHandler(string messageName);
    }
}
