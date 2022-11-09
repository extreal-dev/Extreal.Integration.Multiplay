namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Class that holds connection parameters.
    /// </summary>
    public class ConnectionParameter
    {
        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Connection config to be used in connection.</value>
        public ConnectionConfig ConnectionConfig { get; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Connection data to be used in connection.</value>
        public IConnectionData ConnectionData { get; }

        /// <summary>
        /// Uses when connection is not successful.
        /// </summary>
        /// <value>Number of seconds to wait when connection is not successful.</value>
        public int ConnectionTimeoutSeconds { get; }

        /// <summary>
        /// Creates a new ConnectionParameter with given connectionConfig, connectionData and connectionTimeoutSeconds.
        /// </summary>
        /// <param name="connectionConfig">Connection Config to be used in connection.</param>
        /// <param name="connectionData">Connection data to be used in connection.</param>
        /// <param name="connectionTimeoutSeconds">Number of seconds to wait when connection is not successful.</param>
        public ConnectionParameter
        (
            ConnectionConfig connectionConfig,
            IConnectionData connectionData = null,
            int connectionTimeoutSeconds = 10
        )
        {
            ConnectionConfig = connectionConfig;
            ConnectionData = connectionData;
            ConnectionTimeoutSeconds = connectionTimeoutSeconds;
        }
    }
}
