using Unity.Netcode;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NetworkObjectProvider : MonoBehaviour
    {
        [SerializeField] private NetworkObject networkObject;

        public NetworkObject NetworkObject => networkObject;
    }
}
