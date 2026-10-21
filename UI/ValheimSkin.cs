using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace HoardFinder.UI
{
    /// <summary>
    /// Valheim's own UI sprites (the same ones Jotunn uses for its Valheim-style panels), drawn 9-sliced from IMGUI.
    /// Sprites are looked up by name among loaded sprites, then in the "UIAtlas" sprite atlas. A missing sprite makes
    /// <see cref="Draw"/> return false so the caller can draw its flat fallback; nothing is created per frame.
    /// </summary>
    internal sealed class ValheimSkin
    {
        public const string Panel = "woodpanel_settings";
        public const string PanelAlt = "woodpanel_trophys";
        public const string Field = "text_field";
        public const string Button = "button";
        public const string ButtonOn = "button_highlight";

        private static readonly string[] Wanted = { Panel, PanelAlt, Field, Button, ButtonOn };
        private const int MaxResolveAttempts = 3;

        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Rect> _uvs = new Dictionary<string, Rect>();
        private readonly List<Sprite> _atlasClones = new List<Sprite>();
        private int _attempts;

        public bool Has(string name) => _sprites.ContainsKey(name);

        /// <summary>
        /// Called when the window opens; retries a few times in case the UI bundle was not loaded yet.
        /// </summary>
        public void Resolve()
        {
            if (_sprites.Count == Wanted.Length || _attempts >= MaxResolveAttempts)
            {
                return;
            }
            _attempts++;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (System.Array.IndexOf(Wanted, sprite.name) >= 0 && !_sprites.ContainsKey(sprite.name))
                {
                    _sprites[sprite.name] = sprite;
                }
            }
            if (_sprites.Count < Wanted.Length)
            {
                ResolveFromAtlas();
            }
            Log.Info("Valheim UI sprites found: " + _sprites.Count + "/" + Wanted.Length + ".");
        }

        private void ResolveFromAtlas()
        {
            foreach (SpriteAtlas atlas in Resources.FindObjectsOfTypeAll<SpriteAtlas>())
            {
                if (atlas.name != "UIAtlas")
                {
                    continue;
                }
                foreach (string name in Wanted)
                {
                    Sprite sprite = _sprites.ContainsKey(name) ? null : atlas.GetSprite(name);
                    if (sprite != null)
                    {
                        // SpriteAtlas.GetSprite returns a clone; keep it to destroy on dispose.
                        _sprites[name] = sprite;
                        _atlasClones.Add(sprite);
                    }
                }
            }
        }

        /// <summary>Draws <paramref name="name"/> 9-sliced into <paramref name="rect"/>, tinted by GUI.color.</summary>
        public bool Draw(string name, Rect rect, float borderScale = 1f)
        {
            if (!_sprites.TryGetValue(name, out Sprite sprite) || sprite == null || sprite.texture == null)
            {
                return false;
            }
            if (Event.current.type == EventType.Repaint)
            {
                if (!_uvs.TryGetValue(name, out Rect uv))
                {
                    uv = NineSlice.UvRect(sprite);
                    _uvs[name] = uv;
                }
                NineSlice.Draw(sprite, uv, rect, borderScale);
            }
            return true;
        }

        public void Dispose()
        {
            foreach (Sprite clone in _atlasClones)
            {
                if (clone != null)
                {
                    Object.Destroy(clone);
                }
            }
            _atlasClones.Clear();
            _sprites.Clear();
            _uvs.Clear();
            _attempts = 0;
        }
    }
}
