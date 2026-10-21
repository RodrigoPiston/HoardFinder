using System;
using System.Collections.Generic;
using System.Globalization;
using HoardFinder.Storage;
using UnityEngine;

namespace HoardFinder.Search
{
    internal sealed class SearchResult
    {
        public ItemName Name;
        public int Total;
        public int Places;
        public float Closest;
        public int Rank;

        // Display strings, built once per snapshot so drawing and typing never format numbers.
        public string TotalText;
        public string ClosestText;
        public string PlacesText;
    }

    /// <summary>
    /// Groups nearby stored items (drawers and containers) by item, then filters, ranks and sorts them. Storage reads
    /// happen in a snapshot rebuilt at most once per <see cref="SnapshotLifetime"/>, when the player moves, or when
    /// the source filter changes; typing, sorting and type/mod filters only work on the cached groups.
    /// </summary>
    internal sealed class StorageSearch
    {
        private const int NoMatch = -1;
        private const float SnapshotLifetime = 1f;
        private const float SnapshotMoveTolerance = 1f;

        private readonly StorageIndex _index;
        private readonly List<StorageEntry> _entries = new List<StorageEntry>(1024);
        private readonly List<StorageEntry> _fresh = new List<StorageEntry>(1024);
        private readonly HashSet<int> _storages = new HashSet<int>();
        private readonly Dictionary<string, SearchResult> _byItem = new Dictionary<string, SearchResult>(256);
        private readonly List<SearchResult> _pool = new List<SearchResult>(256);
        private readonly List<SearchResult> _results = new List<SearchResult>(256);
        private readonly SortedSet<string> _modSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _poolUsed;
        private float _snapshotTime = float.NegativeInfinity;
        private Vector3 _snapshotCenter;
        private float _snapshotRadius;
        private int _snapshotKey = -1;

        public StorageSearch(StorageIndex index)
        {
            _index = index;
        }

        /// <summary>Distinct storages in the current snapshot.</summary>
        public int LastStorageCount { get; private set; }

        /// <summary>Mods present in the current snapshot, sorted; drives the mod filter.</summary>
        public List<string> Mods { get; } = new List<string>();

        public void Invalidate() => _snapshotTime = float.NegativeInfinity;

        /// <summary>Returns filtered, sorted results (a shared list, valid until the next call).</summary>
        public List<SearchResult> Search(string query, Vector3 center, float radius, int maxResults,
            SearchOptions options)
        {
            if (SnapshotStale(center, radius, options))
            {
                Group(center, radius, options);
            }
            string key = ItemNames.Normalize(query);
            _results.Clear();
            foreach (SearchResult result in _byItem.Values)
            {
                result.Rank = key.Length == 0 ? 0 : RankOf(result.Name, key);
                if (result.Rank != NoMatch && options.Accepts(result.Name))
                {
                    _results.Add(result);
                }
            }
            _results.Sort(SearchSort.For(options.Sort));
            if (_results.Count > maxResults)
            {
                _results.RemoveRange(maxResults, _results.Count - maxResults);
            }
            return _results;
        }

        private bool SnapshotStale(Vector3 center, float radius, SearchOptions options)
        {
            return Time.unscaledTime - _snapshotTime > SnapshotLifetime
                || options.SnapshotKey != _snapshotKey
                || !Mathf.Approximately(radius, _snapshotRadius)
                || (center - _snapshotCenter).sqrMagnitude > SnapshotMoveTolerance * SnapshotMoveTolerance;
        }

        /// <summary>Storages within radius holding any of <paramref name="prefabs"/>, read fresh.</summary>
        public void CollectHolding(HashSet<string> prefabs, Vector3 center, float radius, SearchOptions options,
            List<StorageEntry> results)
        {
            // Separate buffer: the grouped snapshot (and the result objects the UI may still be drawing) stays intact.
            _index.Collect(center, radius, _fresh);
            results.Clear();
            foreach (StorageEntry entry in _fresh)
            {
                if (prefabs.Contains(entry.Item) && options.Accepts(entry.Kind))
                {
                    results.Add(entry);
                }
            }
        }

        /// <summary>
        /// Exact display name, then prefix, then substring; prefab-name matches rank below display matches.
        /// </summary>
        private static int RankOf(ItemName name, string key)
        {
            int display = MatchLevel(name.DisplayKey, key);
            if (display != NoMatch)
            {
                return display;
            }
            int prefab = MatchLevel(name.PrefabKey, key);
            return prefab == NoMatch ? NoMatch : prefab + 3;
        }

        private static int MatchLevel(string candidate, string key)
        {
            if (candidate == key)
            {
                return 0;
            }
            if (candidate.StartsWith(key, StringComparison.Ordinal))
            {
                return 1;
            }
            return candidate.IndexOf(key, StringComparison.Ordinal) >= 0 ? 2 : NoMatch;
        }

        private void Group(Vector3 center, float radius, SearchOptions options)
        {
            _index.Collect(center, radius, _fresh);
            _entries.Clear();
            foreach (StorageEntry entry in _fresh)
            {
                if (options.Accepts(entry.Kind))
                {
                    _entries.Add(entry);
                }
            }
            _snapshotTime = Time.unscaledTime;
            _snapshotCenter = center;
            _snapshotRadius = radius;
            _snapshotKey = options.SnapshotKey;
            LastStorageCount = StorageIndex.CountStorages(_entries, _storages);
            Aggregate();
            FinishGroups();
        }

        private void Aggregate()
        {
            _byItem.Clear();
            _poolUsed = 0;
            foreach (StorageEntry entry in _entries)
            {
                if (!_byItem.TryGetValue(entry.Item, out SearchResult result))
                {
                    result = Rent();
                    result.Name = ItemNames.Get(entry.Item);
                    _byItem[entry.Item] = result;
                }
                result.Total += entry.Count;
                result.Places++;
                result.Closest = Mathf.Min(result.Closest, entry.Distance);
            }
        }

        private void FinishGroups()
        {
            _modSet.Clear();
            foreach (SearchResult result in _byItem.Values)
            {
                result.TotalText = result.Total.ToString("N0", CultureInfo.InvariantCulture);
                result.ClosestText = Mathf.RoundToInt(result.Closest) + "m";
                result.PlacesText = result.Places.ToString(CultureInfo.InvariantCulture);
                _modSet.Add(result.Name.Mod);
            }
            Mods.Clear();
            Mods.AddRange(_modSet);
        }

        private SearchResult Rent()
        {
            if (_poolUsed == _pool.Count)
            {
                _pool.Add(new SearchResult());
            }
            SearchResult result = _pool[_poolUsed++];
            result.Total = 0;
            result.Places = 0;
            result.Closest = float.MaxValue;
            result.Rank = 0;
            return result;
        }

        public void ResetSession()
        {
            _entries.Clear();
            _fresh.Clear();
            _byItem.Clear();
            _results.Clear();
            Mods.Clear();
            _poolUsed = 0;
            Invalidate();
        }
    }
}
