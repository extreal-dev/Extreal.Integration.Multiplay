using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    /// <summary>
    /// Abstract class that becomes the base of WebRTC client classes.
    /// </summary>
    public abstract class WebRtcClient : DisposableBase
    {
        private readonly Queue<WebRtcEvent> events;

        /// <summary>
        /// Creates WebRtcClient.
        /// </summary>
        protected WebRtcClient() => events = new Queue<WebRtcEvent>();

        /// <summary>
        /// Connects to the host.
        /// </summary>
        public void Connect()
        {
            events.Clear();
            DoConnectAsync().Forget();
        }

        /// <summary>
        /// Uses for connecting to the host.
        /// </summary>
        /// <returns>UniTask for this method.</returns>
        protected abstract UniTask DoConnectAsync();

        /// <summary>
        /// Sends the byte array to the host/client.
        /// </summary>
        /// <param name="clientId">Client ID of the destination.</param>
        /// <param name="payload">Byte array to be sent.</param>
        public void Send(ulong clientId, ArraySegment<byte> payload)
            => DoSend(clientId, ToStr(payload));

        /// <summary>
        /// Uses for sending byte array to the host/client.
        /// </summary>
        /// <param name="clientId">Client ID of the destination.</param>
        /// <param name="payload">Byte array to be sent.</param>
        protected abstract void DoSend(ulong clientId, string payload);

        /// <summary>
        /// Polls the event.
        /// </summary>
        /// <returns>Event to be executed next.</returns>
        public WebRtcEvent PollEvent()
            => events.Count > 0 ? events.Dequeue() : WebRtcEvent.Nothing;

        /// <summary>
        /// Clears the status of this instance.
        /// </summary>
        public void Clear()
        {
            events.Clear();
            DoClear();
        }

        /// <summary>
        /// Uses for clearing the status of this instance.
        /// </summary>
        protected abstract void DoClear();

        /// <summary>
        /// Disconnects the remote client from the host.
        /// </summary>
        /// <param name="clientId">Client ID to be disconnected.</param>
        public abstract void DisconnectRemoteClient(ulong clientId);

        /// <summary>
        /// Enqueues connect event.
        /// </summary>
        /// <param name="clientId">Client ID of the host.</param>
        protected void FireOnConnected(ulong clientId)
            => events.Enqueue(new WebRtcEvent(NetworkEvent.Connect, clientId));

        /// <summary>
        /// Enqueues data received event.
        /// </summary>
        /// <param name="clientId">ID of the client that sent the data.</param>
        /// <param name="payload">Received data.</param>
        protected void FireOnDataReceived(ulong clientId, string payload)
            => events.Enqueue(new WebRtcEvent(NetworkEvent.Data, clientId, ToByte(payload)));

        /// <summary>
        /// Enqueues disconnect event.
        /// </summary>
        /// <param name="clientId">ID of the connected host/client.</param>
        protected void FireOnDisconnected(ulong clientId)
            => events.Enqueue(new WebRtcEvent(NetworkEvent.Disconnect, clientId));

        private static string ToStr(ArraySegment<byte> payload)
        {
            var buf = new byte[payload.Count];
            Buffer.BlockCopy(payload.Array!, payload.Offset, buf, 0, payload.Count);
            return BitConverter.ToString(buf);
        }

        private static byte[] ToByte(string payload)
        {
            var str2Array = payload.Split('-');
            var byteBuf = new byte[str2Array.Length];
            for (var i = 0; i < str2Array.Length; i++)
            {
                byteBuf[i] = Convert.ToByte(str2Array[i], 16);
            }
            return byteBuf;
        }
    }
}
