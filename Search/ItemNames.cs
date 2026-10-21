using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace HoardFinder.Search
{
    internal sealed class ItemName
    {
        public string Prefab;
        public string Display;
        public string DisplayKey;
        public string PrefabKey;
        public Sprite Icon;
        public ItemCategory Category;

        /// <summary>Name of the mod that added the item, "Valheim" for vanilla (see <see cref="ModLookup"/>).</summary>
        public string Mod;

        public bool IsVanilla => Mod == ModLookup.Vanilla;
    }

    /// <summary>
    /// Caches localized names per prefab. Invalidated when ObjectDB or the language changes, so a different server
    /// (different item set) or a language switch never serves stale names.
    /// </summary>
    internal static class ItemNames
    {
        private static readonly Dictionary<string, ItemName> Cache = new Dictionary<string, ItemName>(256);
        private static readonly StringBuilder Buffer = new StringBuilder(64);
        private static ObjectDB _cachedFor;
        private static string _cachedLanguage;

        public static ItemName Get(string prefab)
        {
            Validate();
            if (!Cache.TryGetValue(prefab, out ItemName name))
            {
                name = Build(prefab);
                Cache[prefab] = name;
            }
            return name;
        }

        public static void Clear()
        {
            Cache.Clear();
            _cachedFor = null;
            _cachedLanguage = null;
        }

        private static void Validate()
        {
            string language = Localization.instance?.GetSelectedLanguage();
            if (_cachedFor != ObjectDB.instance || _cachedLanguage != language)
            {
                Cache.Clear();
                _cachedFor = ObjectDB.instance;
                _cachedLanguage = language;
            }
        }

        private static ItemName Build(string prefab)
        {
            ItemDrop drop = null;
            GameObject go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            if (go != null)
            {
                drop = go.GetComponent<ItemDrop>();
            }
            ItemDrop.ItemData.SharedData shared = drop != null ? drop.m_itemData?.m_shared : null;
            string display = shared != null && Localization.instance != null
                ? Localization.instance.Localize(shared.m_name)
                : prefab;
            if (string.IsNullOrEmpty(display))
            {
                display = prefab;
            }
            Sprite icon = shared?.m_icons != null && shared.m_icons.Length > 0 ? shared.m_icons[0] : null;
            return new ItemName
            {
                Prefab = prefab,
                Display = display,
                DisplayKey = Normalize(display),
                PrefabKey = Normalize(prefab),
                Icon = icon,
                Category = ItemCategories.Of(shared),
                Mod = ModLookup.ModOf(prefab),
            };
        }

        /// <summary>
        /// Lower-case, accent-free, with whitespace, '_' and '-' removed: "Black metal" and "BlackMetal" both give
        /// "blackmetal", and "Piña" matches "pina".
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            Buffer.Length = 0;
            foreach (char c in Decompose(text))
            {
                if (!char.IsWhiteSpace(c) && c != '_' && c != '-'
                    && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    Buffer.Append(char.ToLowerInvariant(c));
                }
            }
            return Buffer.ToString();
        }

        private static bool _decomposeFailed;

        /// <summary>
        /// FormD splits "ñ" into "n" + combining tilde. Falls back to the raw text if the runtime lacks the tables.
        /// </summary>
        private static string Decompose(string text)
        {
            if (_decomposeFailed)
            {
                return text;
            }
            try
            {
                return text.Normalize(NormalizationForm.FormD);
            }
            catch (System.Exception e)
            {
                _decomposeFailed = true;
                Log.Warning("Unicode normalization unavailable; accent-insensitive search disabled. " + e.Message);
                return text;
            }
        }
    }
}
