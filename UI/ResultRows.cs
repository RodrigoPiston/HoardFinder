using HoardFinder.I18n;
using HoardFinder.Search;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>Result list pieces: column headers, rows, the gliding selection bar and the scrollbar.</summary>
    internal sealed class ResultRows
    {
        private const float IconSize = 22f;
        private const float TotalColumn = 90f;
        private const float DrawersColumn = 70f;
        private const float DistanceColumn = 60f;
        private const float GlideSharpness = 24f;
        private const float FlashSeconds = SearchWindow.CloseDuration;

        private readonly SearchWindowStyles _styles;
        private readonly float _rowHeight;
        private readonly System.Collections.Generic.Dictionary<string, float> _tagWidths =
            new System.Collections.Generic.Dictionary<string, float>();
        private readonly GUIContent _measure = new GUIContent();
        private int _tagVersion = -1;
        private float _selectionY;
        private int _glideFrame = -1;

        public ResultRows(SearchWindowStyles styles, float rowHeight)
        {
            _styles = styles;
            _rowHeight = rowHeight;
        }

        public void DrawColumns(Rect rect)
        {
            GUI.Label(new Rect(rect.x + IconSize + 16f, rect.y, 220f, rect.height), Tr.T("col_item"), _styles.Column);
            GUI.Label(ColumnRect(rect, 2), Tr.T("col_places"), _styles.ColumnRight);
            GUI.Label(ColumnRect(rect, 1), Tr.T("col_total"), _styles.ColumnRight);
            GUI.Label(ColumnRect(rect, 0), Tr.T("col_near"), _styles.ColumnRight);
        }

        /// <summary>
        /// Right-aligned numeric columns, counted from the right edge: 0 distance, 1 total, 2 drawers.
        /// </summary>
        private static Rect ColumnRect(Rect row, int fromRight)
        {
            float right = row.xMax - 8f;
            if (fromRight >= 1)
            {
                right -= DistanceColumn;
            }
            if (fromRight >= 2)
            {
                right -= TotalColumn;
            }
            float width = fromRight == 0 ? DistanceColumn : fromRight == 1 ? TotalColumn : DrawersColumn;
            return new Rect(right - width, row.y, width, row.height);
        }

        /// <summary>The selection bar glides toward the selected row instead of jumping (once per frame).</summary>
        public void DrawSelection(SearchWindow window, Rect list)
        {
            int visible = window.Selected - window.Scroll;
            if (visible < 0 || visible >= SearchWindowView.VisibleRows || Event.current.type != EventType.Repaint)
            {
                return;
            }
            float target = visible * _rowHeight;
            if (_glideFrame != Time.frameCount)
            {
                bool far = Mathf.Abs(_selectionY - target) > _rowHeight * 4f;
                _selectionY = far ? target : Anim.Approach(_selectionY, target, GlideSharpness);
                _glideFrame = Time.frameCount;
            }
            var bar = new Rect(list.x, list.y + _selectionY, list.width, _rowHeight);
            GUI.DrawTexture(bar, _styles.Selection);
            GUI.DrawTexture(new Rect(bar.x, bar.y, 3f, bar.height), _styles.Accent);
        }

        /// <summary>Returns true when the row was clicked (the window is then closed and drawing must stop).</summary>
        public bool Draw(SearchWindow window, SearchResult result, Rect row, int index, bool interactive)
        {
            Event e = Event.current;
            bool hover = interactive && row.Contains(e.mousePosition);
            if (e.type == EventType.Repaint)
            {
                DrawBackground(window, row, index, hover);
                float iconY = row.y + (_rowHeight - IconSize) / 2f;
                DrawIcon(result.Name.Icon, new Rect(row.x + 8f, iconY, IconSize, IconSize));
            }
            if (hover && e.type == EventType.MouseDown && e.button == 0)
            {
                bool all = e.shift;
                e.Use();
                window.Choose(index, all);
                return true;
            }
            DrawName(window, result, row, index);
            GUI.Label(ColumnRect(row, 2), result.PlacesText, _styles.Muted);
            GUI.Label(ColumnRect(row, 1), result.TotalText, _styles.Number);
            GUI.Label(ColumnRect(row, 0), result.ClosestText, _styles.Muted);
            return false;
        }

        /// <summary>Item name, plus the source mod as a muted tag on the right for non-vanilla items.</summary>
        private void DrawName(SearchWindow window, SearchResult result, Rect row, int index)
        {
            var nameRect = new Rect(row.x + IconSize + 16f, row.y, row.width - IconSize - 250f, row.height);
            if (!result.Name.IsVanilla)
            {
                GUI.Label(nameRect, result.Name.Mod, _styles.ModTag);
                nameRect.width -= Mathf.Min(nameRect.width * 0.45f, TagWidth(result.Name.Mod) + 8f);
            }
            GUIStyle name = index == window.Selected ? _styles.NameSelected : _styles.Name;
            GUI.Label(nameRect, result.Name.Display, name);
        }

        /// <summary>Measured once per mod name and language (the font depends on the language).</summary>
        private float TagWidth(string mod)
        {
            if (_tagVersion != Tr.Version)
            {
                _tagVersion = Tr.Version;
                _tagWidths.Clear();
            }
            if (!_tagWidths.TryGetValue(mod, out float width))
            {
                _measure.text = mod;
                width = _styles.ModTag.CalcSize(_measure).x;
                _tagWidths[mod] = width;
            }
            return width;
        }

        /// <summary>Hover tint, plus a fading flash on the chosen row(s) while the window closes.</summary>
        private void DrawBackground(SearchWindow window, Rect row, int index, bool hover)
        {
            bool chosen = !SearchWindow.IsOpen && (window.ChosenIndex == index || window.ChosenIndex == int.MaxValue);
            if (chosen)
            {
                Color old = GUI.color;
                float flash = 1f - Anim.Progress(window.ClosedAt, FlashSeconds);
                GUI.color = new Color(old.r, old.g, old.b, flash);
                GUI.DrawTexture(row, _styles.Flash);
                GUI.color = old;
            }
            else if (hover && index != window.Selected)
            {
                GUI.DrawTexture(row, _styles.Hover);
            }
        }

        private static void DrawIcon(Sprite icon, Rect rect)
        {
            if (icon == null || icon.texture == null)
            {
                return;
            }
            Texture2D texture = icon.texture;
            Rect r = icon.textureRect;
            float w = texture.width;
            float h = texture.height;
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(r.x / w, r.y / h, r.width / w, r.height / h));
        }

        public void DrawScrollbar(SearchWindow window, Rect list)
        {
            int count = window.Results.Count;
            int visibleRows = SearchWindowView.VisibleRows;
            if (count <= visibleRows || Event.current.type != EventType.Repaint)
            {
                return;
            }
            float thumb = Mathf.Max(18f, list.height * visibleRows / count);
            float y = list.y + (list.height - thumb) * window.Scroll / (count - visibleRows);
            GUI.DrawTexture(new Rect(list.xMax + 4f, list.y, 3f, list.height), _styles.Track);
            GUI.DrawTexture(new Rect(list.xMax + 4f, y, 3f, thumb), _styles.Accent);
        }
    }
}
