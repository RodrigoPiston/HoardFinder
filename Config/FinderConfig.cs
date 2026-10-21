using System.Collections.Generic;
using BepInEx.Configuration;
using HoardFinder.Search;
using HoardFinder.Storage;
using UnityEngine;

namespace HoardFinder.Config
{
    internal sealed class FinderConfig
    {
        private const string General = "General";
        private const string Sources = "Sources";
        private const string Window = "Window";

        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<float> SearchRadius { get; private set; }
        public ConfigEntry<float> HighlightDuration { get; private set; }
        public ConfigEntry<int> MaxSearchResults { get; private set; }
        public ConfigEntry<KeyboardShortcut> OpenSearchHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> ClearHighlightHotkey { get; private set; }
        public ConfigEntry<bool> EnableDebugLogging { get; private set; }

        public ConfigEntry<bool> IncludeDrawers { get; private set; }
        public ConfigEntry<bool> IncludeChests { get; private set; }
        public ConfigEntry<bool> IncludeVehicles { get; private set; }
        public ConfigEntry<bool> IncludeTombstones { get; private set; }
        public ConfigEntry<bool> IncludeWorldContainers { get; private set; }
        public ConfigEntry<bool> RespectStorageAccess { get; private set; }
        public ConfigEntry<bool> DetectItemMods { get; private set; }

        public ConfigEntry<SortMode> Sort { get; private set; }
        public ConfigEntry<TypeFilter> TypeFilter { get; private set; }
        public ConfigEntry<SourceFilter> SourceFilter { get; private set; }

        public FinderConfig(ConfigFile file)
        {
            BindGeneral(file);
            BindSources(file);
            BindWindow(file);
        }

        private void BindGeneral(ConfigFile file)
        {
            Enabled = file.Bind(General, nameof(Enabled), true, "Enable HoardFinder.");
            SearchRadius = file.Bind(General, nameof(SearchRadius), 50f, new ConfigDescription(
                "Radius in meters around the player in which storage is searched.",
                new AcceptableValueRange<float>(5f, 200f)));
            HighlightDuration = file.Bind(General, nameof(HighlightDuration), 10f, new ConfigDescription(
                "Seconds the matching storage stays highlighted.",
                new AcceptableValueRange<float>(1f, 120f)));
            MaxSearchResults = file.Bind(General, nameof(MaxSearchResults), 50, new ConfigDescription(
                "Maximum number of item rows shown in the search window.",
                new AcceptableValueRange<int>(5, 300)));
            OpenSearchHotkey = file.Bind(General, nameof(OpenSearchHotkey),
                new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
                "Shortcut that opens (and closes) the search window. Can also be changed from the window itself.");
            ClearHighlightHotkey = file.Bind(General, nameof(ClearHighlightHotkey), KeyboardShortcut.Empty,
                "Optional shortcut that removes the current highlight early. Empty = disabled.");
            EnableDebugLogging = file.Bind(General, nameof(EnableDebugLogging), false,
                "Write per-search diagnostics to the BepInEx log.");
        }

        private void BindSources(ConfigFile file)
        {
            IncludeDrawers = file.Bind(Sources, nameof(IncludeDrawers), true, "Search BetrValheim drawers.");
            IncludeChests = file.Bind(Sources, nameof(IncludeChests), true,
                "Search built chests (vanilla and modded).");
            IncludeVehicles = file.Bind(Sources, nameof(IncludeVehicles), true, "Search carts and ships.");
            IncludeTombstones = file.Bind(Sources, nameof(IncludeTombstones), true, "Search tombstones.");
            IncludeWorldContainers = file.Bind(Sources, nameof(IncludeWorldContainers), false,
                "Search containers that are not player-built (dungeon and location loot).");
            RespectStorageAccess = file.Bind(Sources, nameof(RespectStorageAccess), true,
                "Only list storage you could open: ward permission and the container privacy setting.");
            DetectItemMods = file.Bind(Sources, nameof(DetectItemMods), true,
                "Detect which mod added each item (Jotunn ModQuery) for the mod sort and filter. Needs a restart.");
        }

        private void BindWindow(ConfigFile file)
        {
            Sort = file.Bind(Window, nameof(Sort), SortMode.Relevance, "Last used sort order.");
            TypeFilter = file.Bind(Window, nameof(TypeFilter), Search.TypeFilter.All, "Last used item type filter.");
            SourceFilter = file.Bind(Window, nameof(SourceFilter), Search.SourceFilter.All,
                "Last used storage filter.");
        }

        /// <summary>Refills <paramref name="kinds"/> from the Include* settings.</summary>
        public void FillEnabledKinds(HashSet<StorageKind> kinds)
        {
            kinds.Clear();
            Toggle(kinds, StorageKind.Drawer, IncludeDrawers.Value);
            Toggle(kinds, StorageKind.Chest, IncludeChests.Value);
            Toggle(kinds, StorageKind.Vehicle, IncludeVehicles.Value);
            Toggle(kinds, StorageKind.Tombstone, IncludeTombstones.Value);
            Toggle(kinds, StorageKind.World, IncludeWorldContainers.Value);
        }

        private static void Toggle(HashSet<StorageKind> kinds, StorageKind kind, bool enabled)
        {
            if (enabled)
            {
                kinds.Add(kind);
            }
        }
    }
}
