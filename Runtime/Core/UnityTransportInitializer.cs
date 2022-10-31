using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Extreal.Integration.Multiplay.NGO
{
    public class UnityTransportInitializer : INetworkTransportInitializer
    {
        public void Initialize(NetworkManager networkManager, ConnectionConfig connectionConfig)
        {
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            {
                unityTransport.ConnectionData.Address = connectionConfig.Address.Trim();
                unityTransport.ConnectionData.Port = connectionConfig.Port;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(UnityTransport)} is expected, but actually {networkManager.NetworkConfig.NetworkTransport.GetType().Name}");
            }

            unityTransport.Initialize(networkManager);
        }
    }
}
