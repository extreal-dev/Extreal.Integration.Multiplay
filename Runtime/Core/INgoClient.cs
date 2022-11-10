using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for the mock to be used as NgoClient when testing.
    /// </summary>
    public interface INgoClient : IDisposable
    {
        /// <summary>
        /// Invokes immediately after connecting to the server.
        /// </summary>
        IObservable<Unit> OnConnected { get; }

        /// <summary>
        /// Invokes just before disconnection from the server.
        /// </summary>
        IObservable<Unit> OnDisconnecting { get; }

        /// <summary>
        /// Invokes immediately after an unexpected disconnection from the server.
        /// </summary>
        IObservable<Unit> OnUnexpectedDisconnected { get; }

        /// <summary>
        /// Gets if this client is running or not.
        /// </summary>
        /// <value>True if this client is running, false otherwise.</value>
        bool IsRunning { get; }

        /// <summary>
        /// Gets if this client is connected to the server or not.
        /// </summary>
        /// <value>True if this client is connected to the server, false otherwise.</value>
        bool IsConnected { get; }

        /// <summary>
        /// Asynchronously connects to the server.
        /// </summary>
        /// <param name="connectionParameter">Connection parameter to be used in connection.</param>
        /// <param name="token">Token used to cancel this operation.</param>
        /// <returns>UniTask of this method.</returns>
        UniTask ConnectAsync(ConnectionParameter connectionParameter, CancellationToken token = default);

        /// <summary>
        /// Asynchronously disconnects from the server.
        /// </summary>
        /// <returns>UniTask of this method.</returns>
        UniTask DisconnectAsync();

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="messageName">Identifier of the message.</param>
        /// <param name="messageStream">Message contents.</param>
        /// <param name="networkDelivery">Specification of the method to transmission.</param>
        void SendMessage(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.Reliable);

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
