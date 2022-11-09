namespace Extreal.Integration.Multiplay.NGO
{
    public class ConnectionParameter
    {
        public ConnectionConfig ConnectionConfig { get; }
        public IConnectionData ConnectionData { get; }
        public int ConnectionTimeoutSeconds { get; }

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
