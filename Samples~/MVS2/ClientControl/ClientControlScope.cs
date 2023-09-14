using System;
using System.Collections.Generic;
using Extreal.Core.Common.Retry;
using Extreal.Integration.Multiplay.NGO.WebRTC;
using Extreal.Integration.P2P.WebRTC;
using SocketIOClient;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.NGO.MVS2.ClientControl
{
    public class ClientControlScope : LifetimeScope
    {
        [SerializeField] private NetworkManager networkManager;

        protected override void Configure(IContainerBuilder builder)
        {
            var peerConfig = new PeerConfig(
                "http://127.0.0.1:3010",
                new SocketIOOptions
                {
                    ConnectionTimeout = TimeSpan.FromSeconds(3),
                    Reconnection = false
                },
                new List<string>
                {
                    "stun:stun.l.google.com:19302",
                    "stun:stun1.l.google.com:19302",
                    "stun:stun2.l.google.com:19302",
                    "stun:stun3.l.google.com:19302",
                    "stun:stun4.l.google.com:19302",
                });

            var peerClient = PeerClientProvider.Provide(peerConfig);
            builder.RegisterComponent(peerClient);

            builder.RegisterComponent(networkManager);
            var webRtcClient = WebRtcClientProvider.Provide(peerClient);
            builder.RegisterComponent(webRtcClient);
            builder.Register<WebRtcTransportConnectionSetter>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<NgoServer>(Lifetime.Singleton);
            builder.Register<NgoClient>(Lifetime.Singleton).WithParameter(typeof(IRetryStrategy), null);

            builder.RegisterEntryPoint<ClientControlPresenter>();
        }
    }
}
