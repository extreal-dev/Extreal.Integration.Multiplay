using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    public class WebRtcEvent
    {
        public static readonly WebRtcEvent Nothing = new WebRtcEvent();

        public NetworkEvent Type { get; private set; }
        public ulong ClientId { get; private set; }
        public byte[] Payload { get; private set; }

        private WebRtcEvent() : this(NetworkEvent.Nothing, ulong.MinValue)
        {
        }

        public WebRtcEvent(NetworkEvent type, ulong clientId, byte[] payload = null)
        {
            Type = type;
            ClientId = clientId;
            Payload = payload;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(ClientId)}: {ClientId}";
    }
}
