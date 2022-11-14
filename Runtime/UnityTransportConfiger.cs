using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that sets the config of NetworkTransport as UnityTransport
    /// </summary>
    public class UnityTransportConfiger : INetworkTransportConfiger
    {
        /// <summary>
        /// Set the config of UnityTransport.
        /// </summary>
        /// <param name="networkTransport">UnityTransport to be set to.</param>
        /// <param name="connectionConfig">ConnectionConfig to be used.</param>
        public void SetConfig(NetworkTransport networkTransport, ConnectionConfig connectionConfig)
        {
            var unityTransport = networkTransport as UnityTransport;
            unityTransport.ConnectionData.Address = connectionConfig.Address.Trim();
            unityTransport.ConnectionData.Port = connectionConfig.Port;
            unityTransport.ConnectionData.ServerListenAddress = connectionConfig.Address.Trim();
        }
    }
}
