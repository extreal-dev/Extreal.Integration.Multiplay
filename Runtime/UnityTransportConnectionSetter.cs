using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that sets the connection config of NetworkTransport as UnityTransport
    /// </summary>
    public class UnityTransportConnectionSetter : IConnectionSetter
    {
        /// <summary>
        /// Gets the target type of this connection setter.
        /// </summary>
        /// <returns>Unity.Netcode.Transports.UTP.UnityTransport</returns>
        public Type TargetType => typeof(UnityTransport);

        /// <summary>
        /// Set the connection config of UnityTransport.
        /// </summary>
        /// <param name="networkTransport">UnityTransport to be set to.</param>
        /// <param name="ngoConfig">NgoConfig to be used.</param>
        public void Set(NetworkTransport networkTransport, NgoConfig ngoConfig)
        {
            var unityTransport = networkTransport as UnityTransport;
            unityTransport.ConnectionData.Address = ngoConfig.Address.Trim();
            unityTransport.ConnectionData.Port = ngoConfig.Port;
            unityTransport.ConnectionData.ServerListenAddress = ngoConfig.Address.Trim();
        }
    }
}
