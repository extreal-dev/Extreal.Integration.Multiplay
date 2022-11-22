using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.NGO.MVS.Common
{
    public class ClientNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
