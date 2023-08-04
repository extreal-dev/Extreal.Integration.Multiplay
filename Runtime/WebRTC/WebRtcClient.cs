using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.WebRTC
{
    public abstract class WebRtcClient : DisposableBase
    {
        private readonly Queue<WebRtcEvent> events;

        protected WebRtcClient() => events = new Queue<WebRtcEvent>();

        public void Connect()
        {
            events.Clear();
            DoConnectAsync().Forget();
        }

        protected abstract UniTask DoConnectAsync();

        public void Send(ulong clientId, ArraySegment<byte> payload)
            => DoSend(clientId, ToStr(payload));

        protected abstract void DoSend(ulong clientId, string payload);

        public WebRtcEvent PollEvent()
            => events.Count > 0 ? events.Dequeue() : WebRtcEvent.Nothing;

        public void Clear()
        {
            events.Clear();
            DoClear();
        }

        protected abstract void DoClear();

        public abstract void DisconnectRemoteClient(ulong clientId);

        protected void FireOnConnected(ulong clientId)
            => events.Enqueue(new WebRtcEvent(NetworkEvent.Connect, clientId));

        protected void FireOnDataReceived(ulong clientId, string payload)
            => events.Enqueue(new WebRtcEvent(NetworkEvent.Data, clientId, ToByte(payload)));

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
