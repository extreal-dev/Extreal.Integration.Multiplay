using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    public class WebRtcTransportConnectionSetter : IConnectionSetter
    {
        private readonly WebRtcClient webRtcClient;

        [SuppressMessage("Style", "CC0057")]
        public WebRtcTransportConnectionSetter(WebRtcClient webRtcClient)
            => this.webRtcClient = webRtcClient;

        public Type TargetType => typeof(WebRtcTransport);

        public void Set(NetworkTransport networkTransport, NgoConfig ngoConfig)
        {
            var webRtcTransport = networkTransport as WebRtcTransport;
            webRtcTransport.SetWebRtcClient(webRtcClient);
        }
    }
}
