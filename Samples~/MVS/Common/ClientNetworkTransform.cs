using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.NGO.MVS.Common
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
