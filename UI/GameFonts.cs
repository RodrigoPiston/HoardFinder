using HoardFinder.I18n;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Valheim's UI typeface (Averia Serif Libre ships in resources.assets). Looked up once by name; a null font makes
    /// IMGUI use its default, so a missing asset only changes the look. Averia only covers Latin-1, so languages with
    /// other scripts (Cyrillic, Greek, CJK, Thai, ...) get the default font, which falls back to OS fonts.
    /// </summary>
    internal static class GameFonts
    {
        private static bool _resolved;
        private static Font _regular;
        private static Font _bold;

        public static Font Regular
        {
            get
            {
                Resolve();
                return Tr.UsesLatin1 ? _regular : null;
            }
        }

        public static Font Bold
        {
            get
            {
                Resolve();
                return Tr.UsesLatin1 ? _bold ?? _regular : null;
            }
        }

        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }
            _resolved = true;
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font.name == "AveriaSerifLibre-Regular")
                {
                    _regular = font;
                }
                else if (font.name == "AveriaSerifLibre-Bold")
                {
                    _bold = font;
                }
            }
            Log.Debug("Game font: " + (_regular != null ? _regular.name : "not found, using IMGUI default"));
        }
    }
}
