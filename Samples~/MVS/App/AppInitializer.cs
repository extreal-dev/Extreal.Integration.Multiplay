using Extreal.Core.Logging;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.MVS.App
{
    public static class AppInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            const LogLevel logLevel = LogLevel.Debug;
            LoggingManager.Initialize(logLevel: logLevel);
        }
    }
}
