using System;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class UnityTransportInitializerTest
    {
        [Test]
        public void InitializeWithNetworkTransportNull()
        {
            var unityTransportInitializer = new UnityTransportInitializer();
            const NetworkTransport nullNetworkTransport = null;
            var connectionConfig = ConnectionConfig.Default;
            Assert.That(() => unityTransportInitializer.Initialize(nullNetworkTransport, connectionConfig),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("networkTransport"));
        }

        [Test]
        public void InitializeWithNetworkTransportExceptUnityTransport()
        {
            var unityTransportInitializer = new UnityTransportInitializer();
            var networkTransport = new GameObject().AddComponent<UNetTransport>().GetComponent<UNetTransport>();
            var connectionConfig = ConnectionConfig.Default;
            Assert.That(() => unityTransportInitializer.Initialize(networkTransport, connectionConfig),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contains($"Expected type is {nameof(UnityTransport)}, but {nameof(UNetTransport)}"));
        }
    }
}
