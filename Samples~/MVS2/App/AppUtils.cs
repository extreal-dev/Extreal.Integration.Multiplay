using System;
using System.Collections.Generic;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public static class AppUtils
    {
        private static readonly HashSet<StageName> spaceStages = new();

        static AppUtils()
        {
            foreach (StageName name in Enum.GetValues(typeof(StageName)))
            {
                if (name.ToString().EndsWith("Space"))
                {
                    spaceStages.Add(name);
                }
            }
        }

        public static bool IsSpace(StageName stageName) => spaceStages.Contains(stageName);
    }
}
