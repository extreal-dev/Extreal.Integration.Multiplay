using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for implementation Setting the config of NetworkTransport.
    /// </summary>
    public interface INetworkTransportConfiger
    {
        /// <summary>
        /// Set the config of networkTransport.
        /// </summary>
        /// <param name="networkTransport">NetworkTransport to be set to.</param>
        /// <param name="connectionConfig">ConnectionConfig to be used.</param>
        void SetConfig(NetworkTransport networkTransport, ConnectionConfig connectionConfig);
    }
}
