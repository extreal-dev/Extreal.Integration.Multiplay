using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INgoServer : IDisposable
    {
        event Action OnServerStarted;
        event Action OnServerStopping;
        event Action<ulong> OnClientConnected;
        event Action<ulong> OnClientDisconnecting;
        event Action<ulong, string> OnClientRemoving;

        bool IsRunning { get; }
        IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients { get; }

        UniTask StartServerAsync(CancellationToken token = default);
        UniTask StopServerAsync();
        void SetConnectionApproval(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprove);
        bool RemoveClient(ulong clientId, string message);
        bool SendMessageToClients(List<ulong> clientIds, string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void SendMessageToAllClients(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler);
        void UnregisterMessageHandler(string messageName);
    }
}
