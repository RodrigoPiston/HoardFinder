using System.Collections.Generic;
using HoardFinder.Config;
using HoardFinder.Highlighting;
using HoardFinder.Search;
using HoardFinder.Storage;
using UnityEngine;

namespace HoardFinder.Commands
{
    /// <summary>
    /// Read-only diagnostics for the F5 console: <c>hoard_dump [radius]</c> lists nearby stored items and
    /// <c>hoard_search [query]</c> prints grouped search results, <c>hoard_clear</c> removes the highlight.
    /// None is a cheat command.
    /// </summary>
    internal static class ConsoleCommands
    {
        private const int MaxDumpLines = 50;

        private static StorageIndex _storage;
        private static StorageSearch _search;
        private static FinderConfig _config;
        private static HighlightManager _highlights;
        private static readonly List<StorageEntry> Entries = new List<StorageEntry>(1024);
        private static readonly SearchOptions DefaultOptions = new SearchOptions();

        public static void Register(StorageIndex storage, StorageSearch search, HighlightManager highlights,
            FinderConfig config)
        {
            _storage = storage;
            _search = search;
            _highlights = highlights;
            _config = config;
            new Terminal.ConsoleCommand("hoard_dump", "[radius] HoardFinder: list nearby stored items", Dump);
            new Terminal.ConsoleCommand("hoard_search", "[query] HoardFinder: search nearby storage", SearchCommand);
            new Terminal.ConsoleCommand("hoard_clear", "HoardFinder: remove the current highlight", args =>
            {
                _highlights?.Clear();
                Write(args, "Highlight cleared.");
            });
        }

        private static void Dump(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (!Ready(args, player))
            {
                return;
            }
            float radius = args.Length > 1 && float.TryParse(args[1], out float parsed)
                ? parsed
                : _config.SearchRadius.Value;
            _storage.Collect(player.transform.position, radius, Entries);
            Write(args, "Loaded storages: " + _storage.LoadedCount + ", stacks within " + radius + "m: "
                + Entries.Count);
            for (int i = 0; i < Entries.Count && i < MaxDumpLines; i++)
            {
                StorageEntry entry = Entries[i];
                Vector3 p = entry.Source.transform.position;
                Write(args, entry.Kind + ": Item: " + ItemNames.Get(entry.Item).Display + " (" + entry.Item + ")"
                    + " Count: " + entry.Count
                    + " Position: " + p.x.ToString("F1") + "," + p.y.ToString("F1") + "," + p.z.ToString("F1")
                    + " Distance: " + entry.Distance.ToString("F1") + "m");
            }
        }

        private static void SearchCommand(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (!Ready(args, player))
            {
                return;
            }
            string query = args.Length > 1 ? args.ArgsAll : string.Empty;
            List<SearchResult> results = _search.Search(query, player.transform.position,
                _config.SearchRadius.Value, _config.MaxSearchResults.Value, DefaultOptions);
            Write(args, "Search \"" + query + "\": " + results.Count + " results / "
                + _search.LastStorageCount + " storages.");
            foreach (SearchResult result in results)
            {
                Write(args, result.Name.Display + " [" + result.Name.Mod + ", " + result.Name.Category + "]"
                    + " | Total: " + result.Total + " | Places: " + result.Places
                    + " | Closest: " + result.Closest.ToString("F1") + "m");
            }
        }

        private static bool Ready(Terminal.ConsoleEventArgs args, Player player)
        {
            if (_storage == null)
            {
                Write(args, "HoardFinder is not ready.");
                return false;
            }
            if (player == null)
            {
                Write(args, "Not in a world.");
                return false;
            }
            return true;
        }

        private static void Write(Terminal.ConsoleEventArgs args, string line)
        {
            args.Context?.AddString(line);
            Log.Info(line);
        }
    }
}
