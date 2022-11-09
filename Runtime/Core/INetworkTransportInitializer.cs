using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for implementation initializing NetworkTransport.
    /// </summary>
    public interface INetworkTransportInitializer
    {
        /// <summary>
        /// Initializes networkTransport using connectionData.
        /// </summary>
        /// <param name="networkTransport">NetworkTransport to be initialized.</param>
        /// <param name="connectionConfig">ConnectionConfig to be used in initialization.</param>
        void Initialize(NetworkTransport networkTransport, ConnectionConfig connectionConfig);
    }
}
