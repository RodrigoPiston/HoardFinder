using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HoardFinder.Commands;
using HoardFinder.Config;
using HoardFinder.Drawers;
using HoardFinder.Highlighting;
using HoardFinder.I18n;
using HoardFinder.Search;
using HoardFinder.Storage;
using HoardFinder.UI;
using HarmonyLib;

namespace HoardFinder
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(BetrDrawerApi.BetrValheimGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(JotunnGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // The same mod under its previous name: two copies would fight over the hotkey and the input lock.
    [BepInIncompatibility(LegacyGuid)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "heido.valheim.hoardfinder";
        public const string Name = "HoardFinder";
        public const string Version = "1.4.0";
        private const string JotunnGuid = "com.jotunn.jotunn";
        private const string LegacyGuid = "heido.valheim.betrdrawerfinder";

        private readonly List<StorageEntry> _holding = new List<StorageEntry>(64);
        private readonly HashSet<string> _chosenItems = new HashSet<string>();
        private readonly HashSet<int> _storageScratch = new HashSet<int>();
        private readonly HashSet<StorageKind> _enabledKinds = new HashSet<StorageKind>();
        private readonly StorageIndex _storage = new StorageIndex();
        private readonly Toast _toast = new Toast();
        private FinderConfig _config;
        private Harmony _harmony;
        private StorageSearch _search;
        private HighlightManager _highlights;
        private SearchWindow _window;
        private bool _wasInWorld;
        private bool _updateErrorLogged;

        private void Awake()
        {
            ConfigMigration.FromLegacy(Config, Logger);
            _config = new FinderConfig(Config);
            Log.Init(Logger, _config.EnableDebugLogging);
            // ModQuery must be enabled before FejdStartup.Awake, so this cannot wait for Start.
            if (_config.DetectItemMods.Value)
            {
                ModLookup.Enable();
            }
            ApplySourceSettings();
            Config.SettingChanged += OnSettingChanged;
            // Patch in Awake so the Container.Awake registry sees every container of the first world.
            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        // BetrValheim registers in its own Awake; resolving drawers in Start guarantees every plugin is loaded.
        private void Start()
        {
            BetrDrawerApi drawers = BetrDrawerApi.TryCreate();
            if (drawers != null)
            {
                drawers.EnabledKinds = _enabledKinds;
                _storage.Add(drawers);
            }
            _storage.Add(new ContainerSource(_enabledKinds));
            _search = new StorageSearch(_storage);
            _highlights = new HighlightManager();
            _window = new SearchWindow(_config, _search, OnResultsSelected, text => _toast.Show(text, Palette.Gold));
            ConsoleCommands.Register(_storage, _search, _highlights, _config);
            Log.Info(Name + " " + Version + " ready. Hotkey: " + _config.OpenSearchHotkey.Value);
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            ApplySourceSettings();
            _search?.Invalidate();
        }

        private void ApplySourceSettings()
        {
            _config.FillEnabledKinds(_enabledKinds);
            StorageAccess.Enabled = _config.RespectStorageAccess.Value;
        }

        private void Update()
        {
            if (_window == null)
            {
                return;
            }
            try
            {
                InputGuard.Tick();
                TrackWorld();
                if (!_config.Enabled.Value)
                {
                    _window.Close();
                    _highlights.Clear();
                    return;
                }
                _window.Update();
                if (_config.ClearHighlightHotkey.Value.IsDown() && _highlights.Active)
                {
                    _highlights.Clear();
                    _toast.Show(Tr.T("toast_cleared"), Palette.Gold);
                }
                _highlights.Tick();
            }
            catch (Exception e)
            {
                ReportOnce(e);
            }
        }

        /// <summary>Drops every per-world state on logout, disconnect or world change (no local player).</summary>
        private void TrackWorld()
        {
            bool inWorld = Player.m_localPlayer != null;
            if (inWorld == _wasInWorld)
            {
                return;
            }
            _wasInWorld = inWorld;
            if (inWorld)
            {
                Log.Info("Entered world. Loaded " + _storage.LoadedCount + " storages so far.");
                return;
            }
            _window.Close();
            _highlights.Clear();
            _search.ResetSession();
            ItemNames.Clear();
            Log.Debug("Left world; caches cleared.");
        }

        private void OnGUI()
        {
            if (_window == null || !_config.Enabled.Value)
            {
                return;
            }
            try
            {
                _highlights.OnGUI();
                _window.OnGUI();
                _toast.OnGUI();
            }
            catch (Exception e)
            {
                ReportOnce(e);
            }
        }

        /// <summary>Highlights every storage holding any of the chosen items (one color per item).</summary>
        private void OnResultsSelected(List<SearchResult> results, SearchOptions options)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            _chosenItems.Clear();
            foreach (SearchResult result in results)
            {
                _chosenItems.Add(result.Name.Prefab);
            }
            float radius = _config.SearchRadius.Value;
            _search.CollectHolding(_chosenItems, player.transform.position, radius, options, _holding);
            _highlights.Show(_holding, _config.HighlightDuration.Value);
            int storages = StorageIndex.CountStorages(_holding, _storageScratch);
            string toast = storages == 0 ? Tr.T("toast_none") : Tr.F("toast_highlight", storages);
            _toast.Show(toast, storages == 0 ? Palette.ForIndex(3) : Palette.Gold);
        }

        private void ReportOnce(Exception e)
        {
            if (_updateErrorLogged)
            {
                return;
            }
            _updateErrorLogged = true;
            Log.Error("Unexpected error; further errors are suppressed. Closing the window. " + e);
            _window?.Close();
            _highlights?.Clear();
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            _harmony?.UnpatchSelf();
            _window?.Dispose();
            _highlights?.Dispose();
            _toast.Dispose();
        }
    }
}
