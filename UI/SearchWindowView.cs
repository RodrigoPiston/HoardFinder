using HoardFinder.I18n;
using HoardFinder.Search;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Draws the search window with absolute rects (no GUILayout, so no extra layout pass). Animation: the window
    /// fades, slides and scales in and out, rows stagger in when the results change, the selection bar glides, and
    /// the chosen row flashes while the window closes. Row text comes pre-formatted from <see cref="SearchResult"/>.
    /// </summary>
    internal sealed class SearchWindowView
    {
        public const int VisibleRows = 12;

        private const string QueryControl = "HoardFinderQuery";
        private const float Width = 600f;
        private const float Pad = 24f;
        private const float ToolbarHeight = 28f;
        private const float RowHeight = 28f;
        private const float RowStagger = 0.018f;
        private const float RowFade = 0.16f;

        private readonly SearchWindowStyles _styles = new SearchWindowStyles();
        private readonly ValheimSkin _skin = new ValheimSkin();
        private readonly ResultRows _rows;
        private readonly Toolbar _toolbar;
        private readonly GUIContent _content = new GUIContent();
        private string _hints;
        private int _hintsVersion = -1;

        public SearchWindowView()
        {
            _rows = new ResultRows(_styles, RowHeight);
            _toolbar = new Toolbar(_styles, _skin);
        }

        /// <summary>Looks up Valheim's UI sprites; by the time the window can open, the game UI is loaded.</summary>
        public void OnOpened() => _skin.Resolve();

        public void Draw(SearchWindow window, bool interactive)
        {
            _styles.Ensure();
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            int rows = Mathf.Clamp(window.Results.Count, 1, VisibleRows);
            float height = Pad + 28f + 36f + ToolbarHeight + 6f + 24f + rows * RowHeight + 52f + Pad;
            Transition(window, out float alpha, out float offset, out float zoom);
            var frame = new Rect((Screen.width / scale - Width) / 2f, 90f + offset, Width, height);
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Anim.ScaleAround(Matrix4x4.Scale(new Vector3(scale, scale, 1f)), frame.center, zoom);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawContent(window, frame, rows, interactive, alpha);
            GUI.color = previousColor;
            GUI.matrix = previous;
        }

        /// <summary>Open: ease-out fade, 16px slide down, 96% to 100% scale. Close: quick fade and slide up.</summary>
        private static void Transition(SearchWindow window, out float alpha, out float offset, out float zoom)
        {
            if (SearchWindow.IsOpen)
            {
                float t = Anim.EaseOutCubic(Anim.Progress(window.OpenedAt, SearchWindow.OpenDuration));
                alpha = t;
                offset = (t - 1f) * 16f;
                zoom = 0.96f + 0.04f * t;
                return;
            }
            float c = Anim.EaseOutCubic(Anim.Progress(window.ClosedAt, SearchWindow.CloseDuration));
            alpha = 1f - c;
            offset = -10f * c;
            zoom = 1f - 0.03f * c;
        }

        private void DrawContent(SearchWindow window, Rect frame, int rows, bool interactive, float alpha)
        {
            DrawFrame(frame);
            float x = frame.x + Pad;
            float inner = frame.width - Pad * 2f;
            float y = frame.y + Pad;
            GUI.Label(new Rect(x, y, inner, 24f), Tr.T("title"), _styles.Title);
            GUI.Label(new Rect(x, y, inner, 24f), window.Status, _styles.Status);
            y += 28f;
            DrawQuery(window, new Rect(x, y, inner, 30f), interactive);
            y += 36f;
            _toolbar.Draw(window, new Rect(x, y, inner, ToolbarHeight), interactive);
            y += ToolbarHeight + 6f;
            _rows.DrawColumns(new Rect(x, y, inner, 20f));
            y += 24f;
            var list = new Rect(x, y, inner, rows * RowHeight);
            DrawList(window, list, interactive, alpha);
            DrawFooter(window, new Rect(x, list.yMax + 8f, inner, 20f), interactive);
        }

        /// <summary>
        /// Valheim's dark wood panel when available, the flat dark frame otherwise. A translucent dark scrim always
        /// goes over the panel's inside: some panel sprites are light wood that the game tints in code, and the cream
        /// and parchment text needs a dark backdrop to stay readable over either.
        /// </summary>
        private void DrawFrame(Rect frame)
        {
            if (_skin.Draw(ValheimSkin.PanelAlt, frame) || _skin.Draw(ValheimSkin.Panel, frame))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(new Rect(frame.x + 10f, frame.y + 10f, frame.width - 20f, frame.height - 20f),
                        _styles.Scrim);
                }
                return;
            }
            GUI.DrawTexture(new Rect(frame.x - 1f, frame.y - 1f, frame.width + 2f, frame.height + 2f), _styles.Border);
            GUI.DrawTexture(frame, _styles.Background);
            GUI.DrawTexture(new Rect(frame.x, frame.y, frame.width, 3f), _styles.Accent);
        }

        private void DrawQuery(SearchWindow window, Rect rect, bool interactive)
        {
            _skin.Draw(ValheimSkin.Field, rect, 0.5f);
            if (!interactive)
            {
                GUI.Label(rect, window.Query, _styles.Input);
                return;
            }
            GUI.SetNextControlName(QueryControl);
            window.Query = GUI.TextField(rect, window.Query ?? string.Empty, 64, _styles.Input);
            if (GUI.GetNameOfFocusedControl() != QueryControl)
            {
                GUI.FocusControl(QueryControl);
            }
            else if (window.SelectAllPending && Event.current.type == EventType.Repaint)
            {
                // Reopened with the previous query: select it so typing replaces it.
                window.SelectAllPending = false;
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.SelectAll();
            }
            if (window.Query.Length == 0)
            {
                GUI.Label(rect, Tr.T("placeholder"), _styles.Placeholder);
            }
        }

        private void DrawList(SearchWindow window, Rect list, bool interactive, float alpha)
        {
            Event e = Event.current;
            if (interactive && e.type == EventType.ScrollWheel && list.Contains(e.mousePosition))
            {
                window.ScrollBy(e.delta.y > 0f ? 3 : -3);
                e.Use();
            }
            if (window.Results.Count == 0)
            {
                string empty = window.Query.Length == 0 ? Tr.F("empty_none", window.RadiusText) : Tr.T("empty_match");
                GUI.Label(list, empty, _styles.Empty);
                return;
            }
            _rows.DrawSelection(window, list);
            if (DrawVisibleRows(window, list, interactive, alpha))
            {
                return;
            }
            GUI.color = new Color(1f, 1f, 1f, alpha);
            _rows.DrawScrollbar(window, list);
        }

        /// <summary>Staggered fade/slide-in per visible row. Returns true when a row click closed the window.</summary>
        private bool DrawVisibleRows(SearchWindow window, Rect list, bool interactive, float alpha)
        {
            int last = Mathf.Min(window.Results.Count, window.Scroll + VisibleRows);
            for (int i = window.Scroll; i < last; i++)
            {
                int visible = i - window.Scroll;
                float rowStart = window.ResultsChangedAt + visible * RowStagger;
                float appear = Anim.EaseOutCubic(Anim.Progress(rowStart, RowFade));
                var row = new Rect(list.x + (1f - appear) * 10f, list.y + visible * RowHeight, list.width, RowHeight);
                GUI.color = new Color(1f, 1f, 1f, alpha * appear);
                if (_rows.Draw(window, window.Results[i], row, i, interactive))
                {
                    return true;
                }
            }
            return false;
        }

        private void DrawFooter(SearchWindow window, Rect rect, bool interactive)
        {
            if (_hintsVersion != Tr.Version)
            {
                _hintsVersion = Tr.Version;
                _hints = "Enter  " + Tr.T("hint_highlight") + "     Shift+Enter  " + Tr.T("hint_all")
                    + "     Tab  " + Tr.T("hint_sort") + "     Up/Down  " + Tr.T("hint_select")
                    + "     Esc  " + Tr.T("hint_close");
            }
            GUI.Label(rect, _hints, _styles.Footer);
            var hotkeyRect = new Rect(rect.x, rect.yMax + 4f, rect.width, 20f);
            if (window.Capture.Active)
            {
                float blink = 0.55f + 0.45f * Mathf.Sin((Anim.Now - window.Capture.StartedAt) * 7f);
                Color old = GUI.color;
                GUI.color = new Color(old.r, old.g, old.b, old.a * blink);
                GUI.Label(hotkeyRect, Tr.T("hotkey_press"), _styles.HotkeyActive);
                GUI.color = old;
                return;
            }
            _content.text = Tr.F("hotkey", window.HotkeyText) + "   [" + Tr.T("hotkey_change") + "]";
            float width = _styles.Hotkey.CalcSize(_content).x;
            var link = new Rect(hotkeyRect.center.x - width / 2f, hotkeyRect.y, width, hotkeyRect.height);
            bool hover = link.Contains(Event.current.mousePosition);
            GUI.Label(hotkeyRect, _content, hover && interactive ? _styles.HotkeyHover : _styles.Hotkey);
            if (interactive && GUI.Button(link, GUIContent.none, GUIStyle.none))
            {
                window.Capture.Begin();
            }
        }

        public void Dispose()
        {
            _styles.Dispose();
            _skin.Dispose();
        }
    }
}
