﻿using System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NetworkTransportMock : NetworkTransport
    {
        public override ulong ServerClientId => throw new NotImplementedException();

        public override void DisconnectLocalClient() => throw new NotImplementedException();
        public override void DisconnectRemoteClient(ulong clientId) => throw new NotImplementedException();
        public override ulong GetCurrentRtt(ulong clientId) => throw new NotImplementedException();
        public override void Initialize(NetworkManager networkManager = null) => throw new NotImplementedException();
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime) => throw new NotImplementedException();
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery) => throw new NotImplementedException();
        public override void Shutdown() => throw new NotImplementedException();
        public override bool StartClient() => throw new NotImplementedException();
        public override bool StartServer() => throw new NotImplementedException();
    }
}
