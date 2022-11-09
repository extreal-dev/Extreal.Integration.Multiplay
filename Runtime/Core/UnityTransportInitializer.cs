using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Extreal.Integration.Multiplay.NGO
{
    public class UnityTransportInitializer : INetworkTransportInitializer
    {
        public void Initialize(NetworkTransport networkTransport, ConnectionConfig connectionConfig)
        {
            if (networkTransport == null)
            {
                throw new ArgumentNullException(nameof(networkTransport));
            }

            if (networkTransport is UnityTransport unityTransport)
            {
                unityTransport.ConnectionData.Address = connectionConfig.Address.Trim();
                unityTransport.ConnectionData.Port = connectionConfig.Port;
                unityTransport.ConnectionData.ServerListenAddress = connectionConfig.Address.Trim();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(networkTransport), $"Expected type is {nameof(UnityTransport)}, but {networkTransport.GetType().Name}");
            }
        }
    }
}
