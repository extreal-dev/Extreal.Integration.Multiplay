using System;
using Extreal.Core.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that uses as network transport for WebRTC in Netcode for GameObjects.
    /// </summary>
    public class WebRtcTransport : NetworkTransport
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebRtcTransport));

        private WebRtcClient webRtcClient;

        /// <summary>
        /// Sets the WebRTC client.
        /// </summary>
        /// <param name="webRtcClient">WebRTC client to be set.</param>
        public void SetWebRtcClient(WebRtcClient webRtcClient) => this.webRtcClient = webRtcClient;

        /// <inheritdoc/>
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
            => webRtcClient.Send(clientId, payload);

        /// <inheritdoc/>
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            var evt = webRtcClient is not null ? webRtcClient.PollEvent() : WebRtcEvent.Nothing;
            clientId = evt.ClientId;
            payload = evt.Payload != null ? new ArraySegment<byte>(evt.Payload) : new ArraySegment<byte>();
            receiveTime = Time.realtimeSinceStartup;
            if (Logger.IsDebug() && (evt.Type == NetworkEvent.Connect || evt.Type == NetworkEvent.Disconnect))
            {
                Logger.LogDebug(evt.ToString());
            }
            return evt.Type;
        }

        /// <inheritdoc/>
        public override bool StartClient() => Connect();

        /// <inheritdoc/>
        public override bool StartServer() => Connect();

        private bool Connect()
        {
            webRtcClient.Connect();
            return true;
        }

        /// <inheritdoc/>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(DisconnectRemoteClient)}: clientId={clientId}");
            }
            webRtcClient.DisconnectRemoteClient(clientId);
        }

        /// <inheritdoc/>
        public override void DisconnectLocalClient()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(DisconnectLocalClient)}");
            }
            // Do nothing here because the DataChannel's OnClose event handles the closing processing.
        }

        /// <inheritdoc/>
        public override ulong GetCurrentRtt(ulong clientId) => 100;

        /// <inheritdoc/>
        public override void Shutdown()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(Shutdown)}");
            }
            webRtcClient.Clear();
        }

        /// <inheritdoc/>
        public override void Initialize(NetworkManager networkManager = null)
        {
        }

        /// <inheritdoc/>
        public override ulong ServerClientId => 0;
    }
}
