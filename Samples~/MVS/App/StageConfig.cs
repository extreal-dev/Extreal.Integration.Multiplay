using Extreal.Core.StageNavigation;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : StageConfigBase<StageName, SceneName>
    {
    }
}
