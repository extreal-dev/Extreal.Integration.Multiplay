using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    public static class WebRtcClientProvider
    {
        [SuppressMessage("Style", "CC0038"), SuppressMessage("Style", "CC0057"), SuppressMessage("Style", "IDE0022")]
        public static WebRtcClient Provide(PeerClient peerClient)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeWebRtcClient(peerClient as NativePeerClient);
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLWebRtcClient();
#endif
        }
    }
}
