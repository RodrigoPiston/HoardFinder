using HoardFinder.UI;
using HarmonyLib;

namespace HoardFinder.Patches
{
    /// <summary>
    /// TextInput.IsVisible() is Valheim's "a text field owns the keyboard" signal: PlayerController, Player, Chat,
    /// Menu, Minimap and GameCamera (cursor) all respect it. Reporting true while the search window is open gives the
    /// same lock Valheim uses for sign editing, without touching any of those systems directly.
    /// </summary>
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
    internal static class TextInputIsVisiblePatch
    {
        private static void Postfix(ref bool __result)
        {
            if (SearchWindow.InputLocked)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// InventoryGui.Update does not check TextInput, so Tab would open the inventory while typing. The window only
    /// opens when the inventory is closed and closes if it appears, so skipping the update while open is safe.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdatePatch
    {
        private static bool Prefix() => !SearchWindow.InputLocked;
    }

    /// <summary>
    /// GameCamera zooms with ZInput's scroll wheel; IMGUI scrolling reads Unity events, so it keeps working.
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    internal static class ScrollWheelPatch
    {
        private static void Postfix(ref float __result)
        {
            if (SearchWindow.InputLocked)
            {
                __result = 0f;
            }
        }
    }
}
