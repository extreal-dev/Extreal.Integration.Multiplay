namespace Extreal.Integration.Multiplay.NGO
{
    public class ConnectionConfig
    {
        public string Address { get; }
        public ushort Port { get; }
        public string Key { get; }

        public static ConnectionConfig Default { get; } = new ConnectionConfig();

        public ConnectionConfig(string address = "127.0.0.1", ushort port = 7777, string key = "DefaultKey")
        {
            Address = address;
            Port = port;
            Key = key;
        }
    }
}
