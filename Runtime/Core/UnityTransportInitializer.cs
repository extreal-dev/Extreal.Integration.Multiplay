using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that initializes NetworkTransport as UnityTransport
    /// </summary>
    public class UnityTransportInitializer : INetworkTransportInitializer
    {
        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">If 'networkTransport' is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the type of 'networkTransport' is not UnityTransport.</exception>
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
