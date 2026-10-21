using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// In-game rebinding of a <see cref="KeyboardShortcut"/> config entry: after <see cref="Begin"/>, the next
    /// non-modifier key press (with whatever Ctrl/Shift/Alt is held) becomes the new shortcut and is written to the
    /// BepInEx config file. Esc cancels; Backspace restores the default.
    /// </summary>
    internal sealed class HotkeyCapture
    {
        private static readonly HashSet<KeyCode> ModifierKeys = new HashSet<KeyCode>
        {
            KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftCommand, KeyCode.RightCommand, KeyCode.AltGr,
        };

        private readonly ConfigEntry<KeyboardShortcut> _entry;

        public HotkeyCapture(ConfigEntry<KeyboardShortcut> entry)
        {
            _entry = entry;
        }

        public bool Active { get; private set; }

        public float StartedAt { get; private set; }

        public void Begin()
        {
            Active = true;
            StartedAt = Anim.Now;
        }

        public void Cancel() => Active = false;

        /// <summary>
        /// Handles one KeyDown while active. Returns the saved shortcut, or null when nothing was saved.
        /// </summary>
        public KeyboardShortcut? Handle(Event e)
        {
            if (e.keyCode == KeyCode.None || ModifierKeys.Contains(e.keyCode) || ReservedWithoutModifier(e))
            {
                return null;
            }
            Active = false;
            if (e.keyCode == KeyCode.Escape)
            {
                return null;
            }
            KeyboardShortcut shortcut = e.keyCode == KeyCode.Backspace
                ? (KeyboardShortcut)_entry.DefaultValue
                : new KeyboardShortcut(e.keyCode, Modifiers(e));
            _entry.Value = shortcut;
            Log.Info("Search hotkey set to " + shortcut + ".");
            return shortcut;
        }

        /// <summary>Bare Enter/Tab would fight chat and the inventory; keep waiting for another key.</summary>
        private static bool ReservedWithoutModifier(Event e)
        {
            bool bare = !e.control && !e.alt && !e.shift;
            KeyCode key = e.keyCode;
            return bare && (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Tab);
        }

        private static KeyCode[] Modifiers(Event e)
        {
            var modifiers = new List<KeyCode>(3);
            if (e.control)
            {
                modifiers.Add(KeyCode.LeftControl);
            }
            if (e.alt)
            {
                modifiers.Add(KeyCode.LeftAlt);
            }
            if (e.shift)
            {
                modifiers.Add(KeyCode.LeftShift);
            }
            return modifiers.ToArray();
        }

        /// <summary>"Ctrl + F" instead of BepInEx's "F + LeftControl".</summary>
        public static string Format(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
            {
                return "-";
            }
            var text = new StringBuilder(24);
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                text.Append(KeyName(modifier)).Append(" + ");
            }
            return text.Append(KeyName(shortcut.MainKey)).ToString();
        }

        private static string KeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "Alt";
            }
            string name = key.ToString();
            if (name.StartsWith("Alpha") && name.Length == 6)
            {
                return name.Substring(5);
            }
            return name.StartsWith("Keypad") ? "Num " + name.Substring(6) : name;
        }
    }
}
