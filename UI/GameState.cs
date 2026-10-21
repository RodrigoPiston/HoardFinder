namespace HoardFinder.UI
{
    /// <summary>Valheim UI checks deciding when the search window may open or must close.</summary>
    internal static class GameState
    {
        public static bool InWorld()
        {
            Player player = Player.m_localPlayer;
            return player != null && !player.IsDead() && !player.IsTeleporting() && ZNetScene.instance != null;
        }

        /// <summary>
        /// No other text field, menu or panel owns the keyboard. Called only while our window is closed.
        /// </summary>
        public static bool CanOpenWindow()
        {
            return InWorld()
                && (Chat.instance == null || !Chat.instance.HasFocus())
                && !Console.IsVisible()
                && !TextInput.IsVisible()
                && !Menu.IsVisible()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Minimap.IsOpen()
                && !Minimap.InTextInput()
                && !Hud.IsPieceSelectionVisible()
                && !Hud.InRadial()
                && (TextViewer.instance == null || !TextViewer.instance.IsVisible());
        }

        /// <summary>Something that ignores our input lock (console F5, death, teleport, logout) took over.</summary>
        public static bool MustCloseWindow()
        {
            return !InWorld() || Console.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen();
        }
    }
}
