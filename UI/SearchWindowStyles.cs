using HoardFinder.Highlighting;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>Lazily created IMGUI styles and textures; GUI.skin is only valid inside OnGUI.</summary>
    internal sealed class SearchWindowStyles
    {
        // Valheim's UI palette: cream body text, orange headings and highlights, muted parchment for secondary text.
        private static readonly Color Text = new Color(0.94f, 0.9f, 0.8f, 1f);
        private static readonly Color Dim = new Color(0.8f, 0.74f, 0.62f, 1f);
        public static readonly Color Orange = new Color(1f, 0.64f, 0.24f, 1f);

        public Texture2D Background { get; private set; }
        public Texture2D Border { get; private set; }
        public Texture2D Accent { get; private set; }
        public Texture2D Selection { get; private set; }
        public Texture2D Hover { get; private set; }
        public Texture2D Track { get; private set; }
        public Texture2D Flash { get; private set; }
        public Texture2D Scrim { get; private set; }

        public GUIStyle Title { get; private set; }
        public GUIStyle Status { get; private set; }
        public GUIStyle Input { get; private set; }
        public GUIStyle Placeholder { get; private set; }
        public GUIStyle Column { get; private set; }
        public GUIStyle ColumnRight { get; private set; }
        public GUIStyle Name { get; private set; }
        public GUIStyle NameSelected { get; private set; }
        public GUIStyle Number { get; private set; }
        public GUIStyle Muted { get; private set; }
        public GUIStyle Empty { get; private set; }
        public GUIStyle Footer { get; private set; }
        public GUIStyle Hotkey { get; private set; }
        public GUIStyle HotkeyHover { get; private set; }
        public GUIStyle HotkeyActive { get; private set; }
        public GUIStyle ButtonLabel { get; private set; }
        public GUIStyle ButtonActive { get; private set; }
        public GUIStyle ModTag { get; private set; }

        private Texture2D _inputBackground;
        private int _stylesVersion = -1;

        /// <summary>Styles are rebuilt when the language changes, because the font depends on the script.</summary>
        public void Ensure()
        {
            I18n.Tr.Refresh();
            if (Background == null)
            {
                CreateTextures();
                Title = null;
            }
            if (Title == null || _stylesVersion != I18n.Tr.Version)
            {
                _stylesVersion = I18n.Tr.Version;
                CreateStyles();
            }
        }

        private void CreateTextures()
        {
            Background = Solid(new Color(0.05f, 0.04f, 0.03f, 0.96f));
            Border = Solid(new Color(0.55f, 0.42f, 0.18f, 0.9f));
            Accent = Solid(Orange);
            Selection = Solid(new Color(1f, 0.64f, 0.24f, 0.2f));
            Hover = Solid(new Color(1f, 1f, 1f, 0.06f));
            Track = Solid(new Color(1f, 1f, 1f, 0.08f));
            Flash = Solid(new Color(1f, 0.85f, 0.4f, 0.55f));
            _inputBackground = Solid(new Color(0f, 0f, 0f, 0.68f));
            Scrim = Solid(new Color(0.035f, 0.025f, 0.015f, 0.9f));
        }

        private void CreateStyles()
        {
            Font regular = GameFonts.Regular;
            Font bold = GameFonts.Bold;
            Title = Label(bold, 20, TextAnchor.MiddleLeft, Orange);
            Status = Label(regular, 13, TextAnchor.MiddleRight, Dim);
            Placeholder = Label(regular, 16, TextAnchor.MiddleLeft, new Color(Dim.r, Dim.g, Dim.b, 0.7f));
            Placeholder.padding = new RectOffset(10, 10, 0, 0);
            Column = Label(bold, 12, TextAnchor.MiddleLeft, Dim);
            ColumnRight = Label(bold, 12, TextAnchor.MiddleRight, Dim);
            Name = Label(regular, 16, TextAnchor.MiddleLeft, Text);
            NameSelected = Label(bold, 16, TextAnchor.MiddleLeft, Orange);
            Number = Label(bold, 16, TextAnchor.MiddleRight, Text);
            Muted = Label(regular, 15, TextAnchor.MiddleRight, Dim);
            Empty = Label(regular, 15, TextAnchor.MiddleCenter, Dim);
            Footer = Label(regular, 13, TextAnchor.MiddleCenter, Dim);
            Hotkey = Label(regular, 12, TextAnchor.MiddleCenter, new Color(0.8f, 0.7f, 0.45f, 1f));
            HotkeyHover = Label(bold, 12, TextAnchor.MiddleCenter, Orange);
            HotkeyActive = Label(bold, 13, TextAnchor.MiddleCenter, Orange);
            ButtonLabel = Label(regular, 13, TextAnchor.MiddleCenter, Text);
            ButtonActive = Label(bold, 13, TextAnchor.MiddleCenter, Orange);
            ModTag = Label(regular, 12, TextAnchor.MiddleRight, Dim);
            Input = InputStyle(regular);
        }

        private GUIStyle InputStyle(Font font)
        {
            var input = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 4, 4),
            };
            foreach (GUIStyleState state in new[] { input.normal, input.focused, input.hover, input.active })
            {
                state.background = _inputBackground;
                state.textColor = Text;
            }
            return input;
        }

        private static GUIStyle Label(Font font, int size, TextAnchor anchor, Color color)
        {
            var label = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                alignment = anchor,
                wordWrap = false,
                clipping = TextClipping.Clip,
            };
            label.normal.textColor = color;
            return label;
        }

        public static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "HoardFinder_UI" };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public void Dispose()
        {
            Texture2D[] textures =
            {
                Background, Border, Accent, Selection, Hover, Track, Flash, Scrim, _inputBackground,
            };
            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }
            Background = null;
            Title = null;
        }
    }
}
