using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>Keyboard navigation of the search window.</summary>
    internal sealed partial class SearchWindow
    {
        private bool HandleKey(Event e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    return true;
                case KeyCode.UpArrow:
                    Move(-1);
                    return true;
                case KeyCode.DownArrow:
                    Move(1);
                    return true;
                case KeyCode.PageUp:
                    Move(-SearchWindowView.VisibleRows);
                    return true;
                case KeyCode.PageDown:
                    Move(SearchWindowView.VisibleRows);
                    return true;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Choose(Selected, e.shift);
                    return true;
                case KeyCode.Tab:
                    Cycle(ToolbarAction.Sort, e.shift ? -1 : 1);
                    return true;
                default:
                    return false;
            }
        }
    }
}
