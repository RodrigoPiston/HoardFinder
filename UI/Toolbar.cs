using HoardFinder.I18n;
using HoardFinder.Search;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Sort and filter buttons under the search field. Left click steps forward, right click steps back; the active
    /// (non-default) ones use Valheim's highlighted button sprite. The mod button only shows when mod detection works.
    /// </summary>
    internal sealed class Toolbar
    {
        private const float Gap = 6f;

        private readonly SearchWindowStyles _styles;
        private readonly ValheimSkin _skin;
        private readonly GUIContent _content = new GUIContent();

        public Toolbar(SearchWindowStyles styles, ValheimSkin skin)
        {
            _styles = styles;
            _skin = skin;
        }

        public void Draw(SearchWindow window, Rect rect, bool interactive)
        {
            SearchOptions options = window.Options;
            int buttons = ModLookup.Available ? 4 : 3;
            float width = (rect.width - Gap * (buttons - 1)) / buttons;
            var cell = new Rect(rect.x, rect.y, width, rect.height);
            string sort = Tr.F("sort", Tr.T("sort_" + options.Sort.ToString().ToLowerInvariant()));
            Button(window, cell, sort, options.Sort != SortMode.Relevance, interactive, ToolbarAction.Sort);
            cell.x += width + Gap;
            string type = Tr.F("filter_type", TypeLabel(options.Type));
            Button(window, cell, type, options.Type != TypeFilter.All, interactive, ToolbarAction.Type);
            cell.x += width + Gap;
            string source = Tr.F("filter_source", SourceLabel(options.Source));
            Button(window, cell, source, options.Source != SourceFilter.All, interactive, ToolbarAction.Source);
            if (buttons == 4)
            {
                cell.x += width + Gap;
                string mod = Tr.F("filter_mod", options.Mod ?? Tr.T("all"));
                Button(window, cell, mod, options.Mod != null, interactive, ToolbarAction.Mod);
            }
        }

        private void Button(SearchWindow window, Rect rect, string label, bool active, bool interactive,
            ToolbarAction action)
        {
            Event e = Event.current;
            bool hover = interactive && rect.Contains(e.mousePosition);
            DrawBackground(rect, active, hover);
            _content.text = label;
            GUI.Label(rect, _content, active ? _styles.ButtonActive : _styles.ButtonLabel);
            if (hover && e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
            {
                int step = e.button == 0 ? 1 : -1;
                e.Use();
                window.Cycle(action, step);
            }
        }

        private void DrawBackground(Rect rect, bool active, bool hover)
        {
            Color old = GUI.color;
            if (hover)
            {
                GUI.color = new Color(old.r * 1.15f, old.g * 1.15f, old.b * 1.15f, old.a);
            }
            string sprite = active ? ValheimSkin.ButtonOn : ValheimSkin.Button;
            if (!_skin.Draw(sprite, rect, 0.5f) && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, active ? _styles.Selection : _styles.Hover);
            }
            GUI.color = old;
        }

        private static string TypeLabel(TypeFilter filter)
        {
            return filter == TypeFilter.All
                ? Tr.T("all")
                : Tr.T(ItemCategories.Key((ItemCategory)((int)filter - 1)));
        }

        private static string SourceLabel(SourceFilter filter)
        {
            switch (filter)
            {
                case SourceFilter.Drawers:
                    return Tr.T("src_drawers");
                case SourceFilter.Chests:
                    return Tr.T("src_chests");
                case SourceFilter.Vehicles:
                    return Tr.T("src_vehicles");
                case SourceFilter.Tombstones:
                    return Tr.T("src_tombstones");
                case SourceFilter.World:
                    return Tr.T("src_world");
                default:
                    return Tr.T("all");
            }
        }
    }

    internal enum ToolbarAction
    {
        Sort,
        Type,
        Source,
        Mod,
    }
}
