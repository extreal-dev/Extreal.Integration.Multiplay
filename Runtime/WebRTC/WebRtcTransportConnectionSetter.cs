using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that sets the connection config of NetworkTransport as WebRtcTransport.
    /// </summary>
    public class WebRtcTransportConnectionSetter : IConnectionSetter
    {
        private readonly WebRtcClient webRtcClient;

        /// <summary>
        /// Creates WebRtcTransportConnectionSetter with webRtcClient.
        /// </summary>
        /// <param name="webRtcClient">WebRTC client.</param>
        [SuppressMessage("Style", "CC0057")]
        public WebRtcTransportConnectionSetter(WebRtcClient webRtcClient)
            => this.webRtcClient = webRtcClient;

        /// <inheritdoc/>
        public Type TargetType => typeof(WebRtcTransport);

        /// <inheritdoc/>
        public void Set(NetworkTransport networkTransport, NgoConfig ngoConfig)
        {
            var webRtcTransport = networkTransport as WebRtcTransport;
            webRtcTransport.SetWebRtcClient(webRtcClient);
        }
    }
}
