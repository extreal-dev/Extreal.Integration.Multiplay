using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Class that holds information for WebRTC event.
    /// </summary>
    public class WebRtcEvent
    {
        /// <summary>
        /// WebRTC event indicating that the event is nothing.
        /// </summary>
        /// <returns>WebRTC event indicating that the event is nothing.</returns>
        public static readonly WebRtcEvent Nothing = new WebRtcEvent();

        /// <summary>
        /// Type of network event.
        /// </summary>
        /// <value>Event type.</value>
        public NetworkEvent Type { get; }

        /// <summary>
        /// Client ID for this event.
        /// </summary>
        /// <value>Client ID.</value>
        public ulong ClientId { get; }

        /// <summary>
        /// Received data.
        /// </summary>
        /// <value>Received data.</value>
        public byte[] Payload { get; }

        private WebRtcEvent() : this(NetworkEvent.Nothing, ulong.MinValue)
        {
        }

        /// <summary>
        /// Creates WebRtcEvent with type, clientId and payload.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <param name="clientId">Client ID.</param>
        /// <param name="payload">Received data.</param>
        public WebRtcEvent(NetworkEvent type, ulong clientId, byte[] payload = null)
        {
            Type = type;
            ClientId = clientId;
            Payload = payload;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(ClientId)}: {ClientId}";
    }
}
