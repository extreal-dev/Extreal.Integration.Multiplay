using System.Threading;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INetcodeServer : IDisposable
    {
        delegate bool ConnectionApprovalDelegate(ulong clientId, byte[] connectionData);

        event Action OnServerStarted;
        event Action OnBeforeStopServer;
        event Action<ulong> OnClientConnected;
        event Action<ulong> OnClientDisconnected;
        event Action<ulong> OnFirstClientConnected;
        event Action<ulong> OnLastClientDisconnected;
        event Action<ulong, string> OnBeforeRejectClient;

        bool IsRunning { get; }
        IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients { get; }

        UniTask<bool> StartServer(ConnectionConfig connectionConfig, CancellationToken token = default);
        void StopServer();
        UniTask RejectClient(ulong clientId, string message);
        void SendMessageToClient(string messageName, ulong clientId, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void SendMessageToAllClients(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void SendMessageToAllClientsExcept(string messageName, ulong clientIdToIgnore, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void RegisterNamedMessage(string messageName, HandleNamedMessageDelegate namedMessageHandler);
        void UnregisterNamedMessage(string messageName);
        void SetConnectionApproval(ConnectionApprovalDelegate approvalConnection);
    }
}
