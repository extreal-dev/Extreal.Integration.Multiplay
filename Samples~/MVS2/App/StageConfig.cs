using Extreal.Core.StageNavigation;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.MVS2.App
{
    [CreateAssetMenu(
        menuName = "Multiplay.NGO.MVS2/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : StageConfigBase<StageName, SceneName>
    {
    }
}
