using HoardFinder.UI;
using UnityEngine;

namespace HoardFinder.Highlighting
{
    /// <summary>
    /// Draws a marker per highlighted drawer in screen space. Being IMGUI, it is never occluded by geometry, and
    /// points outside the view (or behind the camera) are clamped to the screen edge so the direction stays readable.
    /// Only Repaint events reach this class.
    /// </summary>
    internal sealed class ScreenMarkers
    {
        private const float Margin = 24f;
        private const float Size = 12f;
        private const float NearestSize = 18f;
        private const float FadeSeconds = 1.5f;

        private Texture2D _pixel;
        private GUIStyle _style;
        private GUIStyle _shadow;
        private int _styleVersion = -1;
        private readonly GUIContent _content = new GUIContent();

        /// <param name="label">Null draws the dot only.</param>
        /// <param name="nearest">The nearest drawer gets a bigger dot.</param>
        /// <param name="remaining">Seconds left; markers fade out during the last <see cref="FadeSeconds"/>.</param>
        /// <param name="pop">Appear animation, 0..~1.1 (ease-out-back); scales the dot and fades everything in.</param>
        public void Draw(Camera camera, Vector3 world, Color color, string label, bool nearest, float remaining,
            float pop)
        {
            EnsureResources();
            Vector3 screen = camera.WorldToScreenPoint(world);
            Project(ref screen);
            float x = screen.x;
            float y = Screen.height - screen.y;
            float fade = Mathf.Clamp01(remaining / FadeSeconds) * Mathf.Clamp01(pop);
            float pulse = (0.75f + 0.25f * Mathf.Sin(Time.time * 6f)) * fade;
            float size = (nearest ? NearestSize : Size) * Mathf.Max(0f, pop);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f * fade);
            GUI.DrawTexture(new Rect(x - size / 2f - 2f, y - size / 2f - 2f, size + 4f, size + 4f), _pixel);
            GUI.color = new Color(color.r, color.g, color.b, pulse);
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), _pixel);
            if (label != null)
            {
                DrawLabel(x, y - size, label, color, fade);
            }
            GUI.color = previous;
        }

        private void DrawLabel(float x, float top, string label, Color color, float fade)
        {
            _content.text = label;
            Vector2 textSize = _style.CalcSize(_content);
            var rect = new Rect(x - textSize.x / 2f, top - textSize.y, textSize.x, textSize.y);
            GUI.color = new Color(1f, 1f, 1f, fade);
            _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), _content, _shadow);
            _style.normal.textColor = color;
            GUI.Label(rect, _content, _style);
        }

        /// <summary>Mirrors points behind the camera and clamps them to the screen border.</summary>
        private static void Project(ref Vector3 screen)
        {
            bool behind = screen.z < 0f;
            if (behind)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }
            float clampedX = Mathf.Clamp(screen.x, Margin, Screen.width - Margin);
            float clampedY = Mathf.Clamp(screen.y, Margin + NearestSize * 2f, Screen.height - Margin);
            if (behind)
            {
                // Behind the camera: pin to the bottom edge so the marker reads as "turn around".
                clampedY = Margin + NearestSize * 2f;
            }
            screen.x = clampedX;
            screen.y = clampedY;
        }

        private void EnsureResources()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "HoardFinder_Pixel" };
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply();
            }
            if (_style == null || _styleVersion != HoardFinder.I18n.Tr.Version)
            {
                _styleVersion = HoardFinder.I18n.Tr.Version;
                _style = new GUIStyle(GUI.skin.label)
                {
                    font = GameFonts.Bold,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                };
                _shadow = new GUIStyle(_style);
            }
        }

        public void Dispose()
        {
            if (_pixel != null)
            {
                Object.Destroy(_pixel);
            }
            _pixel = null;
            _style = null;
            _shadow = null;
        }
    }
}
