using Extreal.Core.Logging;
using UnityEngine;

namespace Extreal.Integration.Multiplay.NGO.MVS.MultiplayServer
{
    public class MultiplayServerInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            const LogLevel logLevel = LogLevel.Debug;
            LoggingManager.Initialize(logLevel: logLevel);
        }
    }
}
