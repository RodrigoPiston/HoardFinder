using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HoardFinder.Config;
using HoardFinder.I18n;
using HoardFinder.Search;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// State and input for the search window; <see cref="SearchWindowView"/> draws it. While it is open,
    /// <see cref="IsOpen"/> makes Valheim treat the game as being in a text input (see Patches/InputPatches.cs), which
    /// stops movement, look, hotbar, chat and the Escape menu, and frees the cursor. Input is released the moment the
    /// window closes; the close animation is drawn afterwards without taking input.
    /// </summary>
    internal sealed partial class SearchWindow
    {
        public const float OpenDuration = 0.2f;
        public const float CloseDuration = 0.16f;
        private const float RefreshInterval = 1f;

        public static bool IsOpen { get; private set; }

        /// <summary>
        /// Game input is blocked while open and briefly after closing (see <see cref="InputGuard"/>).
        /// </summary>
        public static bool InputLocked => IsOpen || InputGuard.Active;

        private readonly FinderConfig _config;
        private readonly StorageSearch _search;
        private readonly Action<List<SearchResult>, SearchOptions> _onSelect;
        private readonly Action<string> _notify;
        private readonly SearchWindowView _view = new SearchWindowView();
        private readonly List<SearchResult> _chosen = new List<SearchResult>(32);
        private string _searchedQuery;
        private int _openedFrame;
        private float _nextRefresh;

        public string Query = string.Empty;
        public List<SearchResult> Results = new List<SearchResult>();
        public int Selected;
        public int Scroll;
        public bool SelectAllPending;
        public string Status = string.Empty;
        public string RadiusText = string.Empty;
        public string HotkeyText = string.Empty;
        public float OpenedAt = float.NegativeInfinity;
        public float ClosedAt = float.NegativeInfinity;
        public float ResultsChangedAt = float.NegativeInfinity;
        public int ChosenIndex = -1;

        public HotkeyCapture Capture { get; }

        /// <summary>
        /// Sort and filters; sort, type and source persist in the config, the mod filter is per session.
        /// </summary>
        public SearchOptions Options { get; } = new SearchOptions();

        public SearchWindow(FinderConfig config, StorageSearch search,
            Action<List<SearchResult>, SearchOptions> onSelect, Action<string> notify)
        {
            _config = config;
            _search = search;
            _onSelect = onSelect;
            _notify = notify;
            Capture = new HotkeyCapture(config.OpenSearchHotkey);
            Options.Sort = config.Sort.Value;
            Options.Type = config.TypeFilter.Value;
            Options.Source = config.SourceFilter.Value;
        }

        /// <summary>True while open or while the close animation is still playing.</summary>
        public bool Visible => IsOpen || Anim.Now - ClosedAt < CloseDuration;

        public void Update()
        {
            if (!Capture.Active && _config.OpenSearchHotkey.Value.IsDown())
            {
                if (IsOpen)
                {
                    Close();
                    return;
                }
                if (GameState.CanOpenWindow())
                {
                    Open();
                }
            }
            if (!IsOpen)
            {
                return;
            }
            if (GameState.MustCloseWindow())
            {
                Close();
                return;
            }
            if (Query != _searchedQuery || Time.unscaledTime >= _nextRefresh)
            {
                Refresh();
            }
        }

        private void Open()
        {
            IsOpen = true;
            Selected = 0;
            Scroll = 0;
            ChosenIndex = -1;
            SelectAllPending = Query.Length > 0;
            _openedFrame = Time.frameCount;
            OpenedAt = Anim.Now;
            ResultsChangedAt = Anim.Now + OpenDuration * 0.5f;
            _search.Invalidate();
            _view.OnOpened();
            Refresh();
            Log.Info("Loaded " + _search.LastStorageCount + " nearby storages.");
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            InputGuard.Engage();
            Capture.Cancel();
            ClosedAt = Anim.Now;
            GUIUtility.keyboardControl = 0;
        }

        private void Refresh()
        {
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            bool queryChanged = Query != _searchedQuery;
            _searchedQuery = Query;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            float radius = _config.SearchRadius.Value;
            Results = _search.Search(Query, player.transform.position, radius, _config.MaxSearchResults.Value, Options);
            RadiusText = Mathf.RoundToInt(radius) + "m";
            Status = Tr.F("status", Results.Count, _search.LastStorageCount, RadiusText);
            HotkeyText = HotkeyCapture.Format(_config.OpenSearchHotkey.Value);
            if (queryChanged)
            {
                Selected = 0;
                Scroll = 0;
                ResultsChangedAt = Mathf.Max(ResultsChangedAt, Anim.Now);
                Log.Debug("Search \"" + Query + "\": " + Results.Count + " results / "
                    + _search.LastStorageCount + " storages.");
            }
            Selected = Mathf.Clamp(Selected, 0, Mathf.Max(0, Results.Count - 1));
        }

        public void OnGUI()
        {
            if (!IsOpen)
            {
                if (Visible)
                {
                    _view.Draw(this, false);
                }
                return;
            }
            Event current = Event.current;
            if (current.type == EventType.KeyDown && ConsumeKey(current))
            {
                current.Use();
                return;
            }
            _view.Draw(this, true);
        }

        /// <summary>
        /// Swallows the opening hotkey (so its letter never reaches the field) and every handled key.
        /// </summary>
        private bool ConsumeKey(Event e)
        {
            if (Time.frameCount == _openedFrame || IsHotkeyCharacter(e))
            {
                return true;
            }
            if (Capture.Active)
            {
                OnCaptured(Capture.Handle(e));
                return true;
            }
            return IsHotkeyEvent(e) || HandleKey(e);
        }

        private void OnCaptured(KeyboardShortcut? saved)
        {
            if (saved.HasValue)
            {
                HotkeyText = HotkeyCapture.Format(saved.Value);
                _notify(Tr.F("hotkey_saved", HotkeyText));
            }
        }

        /// <summary>
        /// A printable hotkey (for example a bare comma) also produces a character event, which can arrive a frame
        /// after the key press. Drop it during the first frames so the key that opened the window is not typed.
        /// </summary>
        private bool IsHotkeyCharacter(Event e)
        {
            const int GraceFrames = 3;
            if (Time.frameCount > _openedFrame + GraceFrames)
            {
                return false;
            }
            int key = (int)_config.OpenSearchHotkey.Value.MainKey;
            bool printable = key >= 32 && key < 127;
            return e.keyCode == (KeyCode)key || (printable && char.ToLowerInvariant(e.character) == (char)key);
        }

        private bool IsHotkeyEvent(Event e)
        {
            KeyboardShortcut hotkey = _config.OpenSearchHotkey.Value;
            return e.keyCode == hotkey.MainKey && (e.control || e.alt || e.command);
        }

        public void Move(int delta)
        {
            Selected = Mathf.Clamp(Selected + delta, 0, Mathf.Max(0, Results.Count - 1));
            if (Selected < Scroll)
            {
                Scroll = Selected;
            }
            else if (Selected >= Scroll + SearchWindowView.VisibleRows)
            {
                Scroll = Selected - SearchWindowView.VisibleRows + 1;
            }
        }

        public void ScrollBy(int delta)
        {
            int max = Mathf.Max(0, Results.Count - SearchWindowView.VisibleRows);
            Scroll = Mathf.Clamp(Scroll + delta, 0, max);
        }

        /// <summary>
        /// Highlights one result, or every listed result when <paramref name="all"/> (Shift) is held.
        /// </summary>
        public void Choose(int index, bool all)
        {
            if (index < 0 || index >= Results.Count)
            {
                return;
            }
            _chosen.Clear();
            if (all)
            {
                _chosen.AddRange(Results);
            }
            else
            {
                _chosen.Add(Results[index]);
            }
            Close();
            ChosenIndex = all ? int.MaxValue : index;
            _onSelect(_chosen, Options);
        }

        /// <summary>Steps one toolbar setting forward (+1) or back (-1), saves it and re-runs the search.</summary>
        public void Cycle(ToolbarAction action, int step)
        {
            switch (action)
            {
                case ToolbarAction.Sort:
                    _config.Sort.Value = Options.Sort = OptionCycling.Step(Options.Sort, step);
                    break;
                case ToolbarAction.Type:
                    _config.TypeFilter.Value = Options.Type = OptionCycling.Step(Options.Type, step);
                    break;
                case ToolbarAction.Source:
                    _config.SourceFilter.Value = Options.Source = OptionCycling.Step(Options.Source, step);
                    break;
                case ToolbarAction.Mod:
                    Options.Mod = OptionCycling.StepMod(_search.Mods, Options.Mod, step);
                    break;
            }
            // Force a refresh as if the query changed: selection resets and the rows animate in again.
            _searchedQuery = null;
            Refresh();
        }

        public void Dispose()
        {
            Close();
            ClosedAt = float.NegativeInfinity;
            _view.Dispose();
        }
    }
}
