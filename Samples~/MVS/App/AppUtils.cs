using System.Collections.Generic;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public static class AppUtils
    {
        private static readonly HashSet<StageName> SpaceStages = new()
        {
            StageName.VirtualStage
        };

        public static bool IsSpace(StageName stageName) => SpaceStages.Contains(stageName);
    }
}
