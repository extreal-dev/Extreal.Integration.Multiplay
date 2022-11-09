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
        public string Address { get; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Port to be used in connection.</value>
        public ushort Port { get; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Key to be used in connection.</value>
        public string Key { get; }

        /// <summary>
        /// <para>Uses during development.</para>
        /// Address: 127.0.0.1, Port: 7777, Key: DefaultKey
        /// </summary>
        /// <returns>Default connection config.</returns>
        public static ConnectionConfig Default { get; } = new ConnectionConfig();

        /// <summary>
        /// Creates a new ConnectionConfig with given address, port and key.
        /// </summary>
        /// <param name="address">IP Address to be used in connection.</param>
        /// <param name="port">Port to be used in connection.</param>
        /// <param name="key">Key to be used in connection.</param>
        public ConnectionConfig(string address = "127.0.0.1", ushort port = 7777, string key = "DefaultKey")
        {
            Address = address;
            Port = port;
            Key = key;
        }
    }
}
