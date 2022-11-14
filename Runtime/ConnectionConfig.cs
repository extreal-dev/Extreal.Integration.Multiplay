using System;
using System.Net;

namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that holds the connection config.
    /// </summary>
    public class ConnectionConfig
    {
        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>IP Address to be used in connection.</value>
        public string Address { get; internal set; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Port to be used in connection.</value>
        public ushort Port { get; internal set; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Key to be used in connection.</value>
        public string Key { get; internal set; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Connection data to be used in connection.</value>
        public byte[] ConnectionData { get; internal set; }

        /// <summary>
        /// Uses when connection is not successful.
        /// </summary>
        /// <value>Number of seconds to wait when connection is not successful.</value>
        public int TimeoutSeconds { get; internal set; }

        /// <summary>
        /// <para>Uses during development.</para>
        /// Address: 127.0.0.1, Port: 7777, Key: DefaultKey, ConnectionData: null, TimeoutSeconds: 10
        /// </summary>
        /// <returns>Default connection config.</returns>
        public static ConnectionConfig Default { get; } = new ConnectionConfig();

        /// <summary>
        /// Creates a new ConnectionConfig with given address, port, key, connectionData and timeoutSeconds.
        /// </summary>
        /// <param name="address">IP Address to be used in connection.</param>
        /// <param name="port">Port to be used in connection.</param>
        /// <param name="key">Key to be used in connection.</param>
        /// <param name="connectionData">Connection data to be used in connection.</param>
        /// <param name="timeoutSeconds">Number of seconds to wait when connection is not successful.</param>
        /// <exception cref="ArgumentException">If the form of 'address' is invalid.</exception>
        public ConnectionConfig
        (
            string address = "127.0.0.1",
            ushort port = 7777,
            string key = "DefaultKey",
            byte[] connectionData = null,
            int timeoutSeconds = 10
        )
        {
            if (!IPAddress.TryParse(address, out var _))
            {
                throw new ArgumentException($"The form of '{nameof(address)}' is invalid");
            }

            Address = address;
            Port = port;
            Key = key;
            ConnectionData = connectionData;
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Updates the config with given address, port, key, connectionData and timeoutSeconds.
        /// </summary>
        /// <param name="address">IP Address to be used in connection.</param>
        /// <param name="port">Port to be used in connection.</param>
        /// <param name="key">Key to be used in connection.</param>
        /// <param name="connectionData">Connection data to be used in connection.</param>
        /// <param name="timeoutSeconds">Number of seconds to wait when connection is not successful.</param>
        /// <exception cref="InvalidOperationException">If attempts to update the config of the default instance.</exception>
        /// <exception cref="ArgumentException">If the form of 'address' is invalid.</exception>
        public void UpdateConfig
        (
            string address = default,
            ushort port = default,
            string key = default,
            byte[] connectionData = default,
            int timeoutSeconds = default
        )
        {
            if (ReferenceEquals(this, Default))
            {
                throw new InvalidOperationException("Unable to update the config of the default instance");
            }
            if (address != null && !IPAddress.TryParse(address, out var _))
            {
                throw new ArgumentException($"The form of '{nameof(address)}' is invalid");
            }

            if (address != null)
            {
                Address = address;
            }
            if (port != 0)
            {
                Port = port;
            }
            if (key != null)
            {
                Key = key;
            }
            if (connectionData != null)
            {
                ConnectionData = connectionData;
            }
            if (timeoutSeconds != 0)
            {
                TimeoutSeconds = timeoutSeconds;
            }
        }
    }
}
