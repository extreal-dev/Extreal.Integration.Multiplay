namespace Extreal.Integration.Multiplay.NGO
{
    public class ConnectionParameter
    {
        public IConnectionData ConnectionData { get; }
        public int ConnectionTimeoutSeconds { get; }

        public ConnectionParameter
        (
            IConnectionData connectionData = null,
            int connectionTimeoutSeconds = 10
        )
        {
            ConnectionData = connectionData;
            ConnectionTimeoutSeconds = connectionTimeoutSeconds;
        }
    }
}
