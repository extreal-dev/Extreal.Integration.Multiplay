using System;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that holds the connection config for NGO.
    /// </summary>
    public class NgoConfig
    {
        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Address to be used in connection.</value>
        public string Address { get; internal set; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Port to be used in connection.</value>
        public ushort Port { get; internal set; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Connection data to be used in connection.</value>
        public byte[] ConnectionData { get; internal set; }

        /// <summary>
        /// Uses when connection is not successful.
        /// </summary>
        /// <value>Time to wait when connection is not successful.</value>
        public TimeSpan Timeout { get; internal set; }

        /// <summary>
        /// Creates a new NgoConfig with given address, port, connectionData and timeout.
        /// </summary>
        /// <param name="address">Address to be used in connection.</param>
        /// <param name="port">Port to be used in connection.</param>
        /// <param name="connectionData">Connection data to be used in connection.</param>
        /// <param name="timeout">
        /// <para>Time to wait when connection is not successful.</para>
        /// Default: 10 seconds
        /// </param>
        /// <exception cref="ArgumentNullException">If 'address' is null.</exception>
        public NgoConfig
        (
            string address = "127.0.0.1",
            ushort port = 7777,
            byte[] connectionData = null,
            TimeSpan timeout = default
        )
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            Port = port;
            ConnectionData = connectionData;
            Timeout = timeout == default ? TimeSpan.FromSeconds(10) : timeout;
        }
    }
}
