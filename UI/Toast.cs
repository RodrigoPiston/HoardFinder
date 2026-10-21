using HoardFinder.Highlighting;
using HoardFinder.I18n;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>Short confirmation banner at the top of the screen: slides in, holds, fades out.</summary>
    internal sealed class Toast
    {
        private const float In = 0.22f;
        private const float Hold = 2.2f;
        private const float Out = 0.45f;

        private readonly GUIContent _content = new GUIContent();
        private Texture2D _background;
        private Texture2D _accent;
        private GUIStyle _style;
        private int _styleVersion = -1;
        private string _text;
        private Color _color = Palette.Gold;
        private float _shownAt = float.NegativeInfinity;

        public void Show(string text, Color color)
        {
            _text = text;
            _color = color;
            _shownAt = Anim.Now;
        }

        public void OnGUI()
        {
            float age = Anim.Now - _shownAt;
            if (_text == null || age > In + Hold + Out || Event.current.type != EventType.Repaint)
            {
                return;
            }
            Ensure();
            float appear = Anim.EaseOutCubic(Anim.Progress(_shownAt, In));
            float alpha = appear * (1f - Anim.Progress(_shownAt + In + Hold, Out));
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _content.text = _text;
            Vector2 size = _style.CalcSize(_content);
            var rect = new Rect((Screen.width / scale - size.x) / 2f, 28f + (appear - 1f) * 18f, size.x, size.y);
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, _background);
            GUI.color = new Color(_color.r, _color.g, _color.b, alpha);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width * appear, 2f), _accent);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, _content, _style);
            GUI.color = old;
            GUI.matrix = previous;
        }

        private void Ensure()
        {
            if (_background == null)
            {
                _background = SearchWindowStyles.Solid(new Color(0.07f, 0.06f, 0.05f, 0.92f));
                _accent = SearchWindowStyles.Solid(Color.white);
            }
            if (_style == null || _styleVersion != Tr.Version)
            {
                _styleVersion = Tr.Version;
                _style = new GUIStyle(GUI.skin.label)
                {
                    font = GameFonts.Bold,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(18, 18, 8, 10),
                };
                _style.normal.textColor = new Color(0.95f, 0.92f, 0.85f, 1f);
            }
        }

        public void Dispose()
        {
            if (_background != null)
            {
                Object.Destroy(_background);
                Object.Destroy(_accent);
            }
            _background = null;
            _accent = null;
            _style = null;
        }
    }
}
