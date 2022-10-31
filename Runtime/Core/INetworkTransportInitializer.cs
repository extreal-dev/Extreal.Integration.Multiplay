using Unity.Netcode;

namespace Extreal.Integration.Multiplay.NGO
{
    public interface INetworkTransportInitializer
    {
        void Initialize(NetworkManager networkManager, ConnectionConfig connectionConfig);
    }
}
