using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CanCommitToTransform = IsOwner || IsServer;
        }

        protected override void Update()
        {
            base.Update();
            if (NetworkManager.Singleton != null
                && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
                && CanCommitToTransform)
            {
                TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
            }
        }
    }
}
