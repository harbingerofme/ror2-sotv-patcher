using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SotV_patcher
{
    internal static class Logger
    {
        private static readonly ManualLogSource logSource = BepInEx.Logging.Logger.CreateLogSource("SotVPatcher");

        public static void Info(object data) => logSource.LogInfo(data);

        public static void Error(object data) => logSource.LogError(data);

        public static void Warn(object data) => logSource.LogWarning(data);

        public static void Fatal(object data) => logSource.LogFatal(data);

        public static void Message(object data) => logSource.LogMessage(data);

        public static void Debug(object data) => logSource.LogDebug(data);
    }
}
