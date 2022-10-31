using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO
{
    [CreateAssetMenu(
        menuName = "Extreal.Integration/Multiplay/NGO/" + nameof(ConnectionConfig),
        fileName = nameof(ConnectionConfig))]
    public class ConnectionConfig : ScriptableObject
    {
#pragma warning disable CC0052
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string key = "DefaultKey";
#pragma warning restore CC0052

        public string Address => address;
        public ushort Port => port;
        public string Key => key;

        private static ConnectionConfig defaultInstance;

        public static ConnectionConfig GetDefault()
        {
            if (defaultInstance == null)
            {
                defaultInstance = CreateInstance<ConnectionConfig>();
            }
            return defaultInstance;
        }
    }
}
