using HoardFinder.Storage;
using HarmonyLib;

namespace HoardFinder.Patches
{
    /// <summary>
    /// Event-driven container registry: no FindObjectsOfType, ever. Destroyed containers are pruned lazily.
    /// </summary>
    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class ContainerAwakePatch
    {
        private static void Postfix(Container __instance)
        {
            try
            {
                ContainerSource.Register(__instance);
            }
            catch (System.Exception e)
            {
                Log.Debug("Container registration skipped: " + e.Message);
            }
        }
    }
}
