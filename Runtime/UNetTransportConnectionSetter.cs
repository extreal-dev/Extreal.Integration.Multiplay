using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that sets the connection config of NetworkTransport as UNetTransport
    /// </summary>
    public class UNetTransportConnectionSetter : IConnectionSetter
    {
        /// <summary>
        /// Gets the target type of this connection setter.
        /// </summary>
        /// <returns>Unity.Netcode.Transports.UNET.UNetTransport</returns>
        public Type TargetType => typeof(UNetTransport);

        /// <summary>
        /// Set the connection config of UNetTransport.
        /// </summary>
        /// <param name="networkTransport">UNetTransport to be set to.</param>
        /// <param name="ngoConfig">NgoConfig to be used.</param>
        public void Set(NetworkTransport networkTransport, NgoConfig ngoConfig)
        {
            var uNetTransport = networkTransport as UNetTransport;
            uNetTransport.ConnectAddress = ngoConfig.Address.Trim();
            uNetTransport.ConnectPort = ngoConfig.Port;
            uNetTransport.ServerListenPort = ngoConfig.Port;
        }
    }
}
