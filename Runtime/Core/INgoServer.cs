using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;
using static Unity.Netcode.NetworkManager;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for the mock to be used as NgoServer when testing.
    /// </summary>
    public interface INgoServer : IDisposable
    {
        /// <summary>
        /// Invokes immediately after this server starts.
        /// </summary>
        IObservable<Unit> OnServerStarted { get; }

        /// <summary>
        /// Invokes just before this server stops.
        /// </summary>
        IObservable<Unit> OnServerStopping { get; }

        /// <summary>
        /// Invokes immediately after the client connects to this server.
        /// </summary>
        IObservable<ulong> OnClientConnected { get; }

        /// <summary>
        /// Invokes just before the client disconnects from this server.
        /// </summary>
        IObservable<ulong> OnClientDisconnecting { get; }

        /// <summary>
        /// Invokes just before removing the client.
        /// </summary>
        IObservable<(ulong clientId, string message)> OnClientRemoving { get; }

        /// <summary>
        /// Gets if this server is running or not.
        /// </summary>
        /// <value>True if this server is running, false otherwise.</value>
        bool IsRunning { get; }

        /// <summary>
        /// Gets information on connected clients.
        /// </summary>
        /// <value>
        /// <para>Dictionary of the connected clients.</para>
        /// Key: Client id, Value: Client information
        /// </value>
        IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients { get; }

        /// <summary>
        /// Asynchronously starts this server.
        /// </summary>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <returns>UniTask of this method.</returns>
        UniTask StartServerAsync(CancellationToken token = default);

        /// <summary>
        /// Asynchronously Stops this server.
        /// </summary>
        /// <returns>UniTask of this method.</returns>
        UniTask StopServerAsync();

        /// <summary>
        /// Sets ConnectionApprovalCallback of NetworkManager.
        /// The callback is called when the client attempts to connect to this server.
        /// </summary>
        /// <param name="connectionApprovalCallback">Callback to be set to ConnectionApprovalCallback.</param>
        void SetConnectionApprovalCallback(Action<ConnectionApprovalRequest, ConnectionApprovalResponse> connectionApprovalCallback);

        /// <summary>
        /// Remove the client.
        /// </summary>
        /// <param name="clientId">Id of the client to be removed.</param>
        /// <param name="message">Message notified upon removing.</param>
        /// <returns>True if the client is successfully removed, false otherwise.</returns>
        bool RemoveClient(ulong clientId, string message);

        /// <summary>
        /// Sends a message to the clients.
        /// </summary>
        /// <param name="clientIds">Ids of the clients to whom the message is sent.</param>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        /// <returns>True if the message is successfully sent, false otherwise.</returns>
        bool SendMessageToClients(List<ulong> clientIds, string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Sends a message to the all clients.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        void SendMessageToAllClients(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);

        /// <summary>
        /// Registers a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageHandler">Message handler to be registered.</param>
        void RegisterMessageHandler(string messageName, HandleNamedMessageDelegate messageHandler);

        /// <summary>
        /// Unregisters a message handler.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        void UnregisterMessageHandler(string messageName);
    }
}
