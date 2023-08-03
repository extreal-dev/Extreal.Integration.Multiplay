using System.Collections.Generic;

namespace Extreal.Integration.Multiplay.NGO.MVS2.App
{
    public static class AppUtils
    {
        private static readonly HashSet<StageName> SpaceStages = new()
        {
            StageName.GroupSelectionStage
        };

        public static bool IsSpace(StageName stageName)
            => SpaceStages.Contains(stageName);
    }
}
