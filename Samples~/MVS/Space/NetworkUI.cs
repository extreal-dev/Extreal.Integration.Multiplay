using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Extreal.Integration.Multiplay.NGO.MVS.Space
{
    public class NetworkUI : MonoBehaviour
    {
        [SerializeField] private Button serverButton;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;

        private void Awake()
        {
            serverButton.onClick.AddListener(() => NetworkManager.Singleton.StartServer());
            hostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
            clientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
        }
    }
}
