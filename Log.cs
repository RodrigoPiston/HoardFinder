using BepInEx.Configuration;
using BepInEx.Logging;

namespace HoardFinder
{
    /// <summary>Thin wrapper over the BepInEx logger; the source name already prefixes every line.</summary>
    internal static class Log
    {
        private static ManualLogSource _source;
        private static ConfigEntry<bool> _debug;

        public static void Init(ManualLogSource source, ConfigEntry<bool> debug)
        {
            _source = source;
            _debug = debug;
        }

        public static bool DebugEnabled => _debug != null && _debug.Value;

        public static void Info(string message) => _source?.LogInfo(message);

        public static void Warning(string message) => _source?.LogWarning(message);

        public static void Error(string message) => _source?.LogError(message);

        public static void Debug(string message)
        {
            if (DebugEnabled)
            {
                _source?.LogInfo("[debug] " + message);
            }
        }
    }
}
