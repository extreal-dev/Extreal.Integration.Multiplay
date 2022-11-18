using System;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for implementation Setting the connection config of NetworkTransport.
    /// </summary>
    public interface IConnectionSetter
    {
        /// <summary>
        /// Gets the target type of this connection setter.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Set the connection config of NetworkTransport.
        /// </summary>
        /// <param name="networkTransport">NetworkTransport to be set to.</param>
        /// <param name="ngoConfig">NgoConfig to be used.</param>
        void Set(NetworkTransport networkTransport, NgoConfig ngoConfig);
    }
}
