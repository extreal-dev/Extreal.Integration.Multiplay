using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INgoServer : IDisposable
    {
        delegate bool ConnectionApprovalCallback(ulong clientId, byte[] connectionData);

        event Action OnServerStarted;
        event Action OnServerStopping;
        event Action<ulong> OnClientConnected;
        event Action<ulong> OnClientDisconnecting;
        event Action<ulong, string> OnClientRemoving;

        bool IsRunning { get; }
        IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients { get; }

        UniTask StartServerAsync(CancellationToken token = default);
        UniTask StopServerAsync();
        void SetConnectionApproval(ConnectionApprovalCallback approvalConnection);
        bool RemoveClient(ulong clientId, string message);
        bool SendMessageToClients(List<ulong> clientId, string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void SendMessageToAllClients(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);
        void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate namedMessageHandler);
        void UnregisterMessageHandler(string messageName);
    }
}
