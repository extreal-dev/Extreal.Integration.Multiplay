using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.NGO.MVS.Space
{
    public class ClientNetworkTransport : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
