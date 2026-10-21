using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace HoardFinder.Config
{
    /// <summary>
    /// The plugin was published as BetrDrawerFinder before; its settings (hotkey, radius, filters) are copied once
    /// into the new config file. The old file is left untouched.
    /// </summary>
    internal static class ConfigMigration
    {
        private const string LegacyFile = "heido.valheim.betrdrawerfinder.cfg";

        public static void FromLegacy(ConfigFile config, ManualLogSource log)
        {
            string legacy = Path.Combine(Paths.ConfigPath, LegacyFile);
            string current = config.ConfigFilePath;
            bool currentEmpty = !File.Exists(current) || new FileInfo(current).Length == 0;
            if (!File.Exists(legacy) || !currentEmpty)
            {
                return;
            }
            try
            {
                File.Copy(legacy, current, true);
                config.Reload();
                log.LogInfo("Settings imported from " + LegacyFile + ".");
            }
            catch (IOException e)
            {
                log.LogWarning("Could not import settings from " + LegacyFile + ": " + e.Message);
            }
        }
    }
}
