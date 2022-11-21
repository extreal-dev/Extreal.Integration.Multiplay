using System.Collections.Generic;
using Extreal.Core.StageNavigation;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public class StageConfigBase<TStage, TScene> : ScriptableObject, IStageConfig<TStage, TScene>
        where TStage : struct
        where TScene : struct
    {
        [SerializeField] private List<TScene> commonScenes;
        [SerializeField] private List<Stage<TStage, TScene>> stages;

        public List<TScene> CommonScenes => commonScenes;
        public List<Stage<TStage, TScene>> Stages => stages;
    }
}
