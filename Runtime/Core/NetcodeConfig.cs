using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO
{
    [CreateAssetMenu(
        menuName = "Extreal.Integration/MultiplayNGO/" + nameof(NetcodeConfig),
        fileName = nameof(NetcodeConfig))]
    public class NetcodeConfig : ScriptableObject
    {
        [SerializeField] private bool connectionApproval;
        [SerializeField] private bool enableSceneManagement;
        [SerializeField] private bool forceSamePrefabs;
        [SerializeField] private bool recycleNetworkIds;

        public bool ConnectionApproval => connectionApproval;
        public bool EnableSceneManagement => enableSceneManagement;
        public bool ForceSamePrefabs => forceSamePrefabs;
        public bool RecycleNetworkIds => recycleNetworkIds;
    }
}
