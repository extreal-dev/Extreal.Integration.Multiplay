using Unity.Netcode;
using Unity.Netcode.Transports.UNET;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that sets the config of NetworkTransport as UNetTransport
    /// </summary>
    public class UNetTransportConfiger : INetworkTransportConfiger
    {
        /// <summary>
        /// Set the config of UNetTransport.
        /// </summary>
        /// <param name="networkTransport">UNetTransport to be set to.</param>
        /// <param name="connectionConfig">ConnectionConfig to be used.</param>
        public void SetConfig(NetworkTransport networkTransport, ConnectionConfig connectionConfig)
        {
            var uNetTransport = networkTransport as UNetTransport;
            uNetTransport.ConnectAddress = connectionConfig.Address.Trim();
            uNetTransport.ConnectPort = connectionConfig.Port;
            uNetTransport.ServerListenPort = connectionConfig.Port;
        }
    }
}
