using System;
using System.Reflection;
using BepInEx;

namespace HoardFinder.Search
{
    /// <summary>
    /// Which mod added an item, through Jotunn's ModQuery (it tracks prefabs added by any mod, Jotunn-based or not).
    /// Soft dependency, resolved by reflection: without Jotunn, or if ModQuery changes, <see cref="Available"/> is
    /// false and the mod sort/filter is hidden. ModQuery must be enabled before FejdStartup.Awake, so
    /// <see cref="Enable"/> runs from the plugin's Awake.
    /// </summary>
    internal static class ModLookup
    {
        public const string Vanilla = "Valheim";

        private static MethodInfo _getPrefab;
        private static PropertyInfo _sourceMod;

        public static bool Available => _getPrefab != null;

        public static void Enable()
        {
            try
            {
                Type query = Type.GetType("Jotunn.Utils.ModQuery, Jotunn", false);
                Type prefab = Type.GetType("Jotunn.Utils.IModPrefab, Jotunn", false);
                MethodInfo enable = query?.GetMethod("Enable", BindingFlags.Public | BindingFlags.Static);
                MethodInfo getPrefab = query?.GetMethod("GetPrefab", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                PropertyInfo sourceMod = prefab?.GetProperty("SourceMod");
                if (enable == null || getPrefab == null || sourceMod == null)
                {
                    Log.Info("Jotunn ModQuery not available; the mod filter is disabled.");
                    return;
                }
                enable.Invoke(null, null);
                _getPrefab = getPrefab;
                _sourceMod = sourceMod;
                Log.Info("Item mod detection enabled through Jotunn ModQuery.");
            }
            catch (Exception e)
            {
                Log.Warning("Jotunn ModQuery could not be enabled; the mod filter is disabled. " + e.Message);
            }
        }

        /// <summary>Mod name for the item prefab; "Valheim" for vanilla items. Called once per item (cached).</summary>
        public static string ModOf(string prefab)
        {
            if (_getPrefab == null)
            {
                return Vanilla;
            }
            try
            {
                object modPrefab = _getPrefab.Invoke(null, new object[] { prefab });
                var plugin = modPrefab != null ? _sourceMod.GetValue(modPrefab) as BepInPlugin : null;
                return plugin != null ? plugin.Name : Vanilla;
            }
            catch (Exception)
            {
                return Vanilla;
            }
        }
    }
}
