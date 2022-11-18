using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.NGO.MVS.Space
{
    public class ClientNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
