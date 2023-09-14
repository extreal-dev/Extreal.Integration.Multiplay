using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that provides WebRTC client.
    /// </summary>
    public static class WebRtcClientProvider
    {
        /// <summary>
        /// Provides WebRTC client.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        /// <returns>WebRTC client.</returns>
        [SuppressMessage("Style", "CC0038"), SuppressMessage("Style", "CC0057"), SuppressMessage("Style", "IDE0022")]
        public static WebRtcClient Provide(PeerClient peerClient)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeWebRtcClient(peerClient as NativePeerClient);
#else
            return new WebGLWebRtcClient();
#endif
        }
    }
}
