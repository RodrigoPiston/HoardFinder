using HoardFinder.Storage;

namespace HoardFinder.Search
{
    internal enum SortMode
    {
        Relevance,
        Name,
        Amount,
        Distance,
        Type,
        Mod,
    }

    /// <summary>
    /// Type filter as stored in the config: All plus every <see cref="ItemCategory"/>, in the same order.
    /// </summary>
    internal enum TypeFilter
    {
        All,
        Materials,
        Food,
        Potions,
        Ammo,
        Equipment,
        Trophies,
        Other,
    }

    /// <summary>
    /// Source filter as stored in the config: All plus every <see cref="StorageKind"/>, in the same order.
    /// </summary>
    internal enum SourceFilter
    {
        All,
        Drawers,
        Chests,
        Vehicles,
        Tombstones,
        World,
    }

    internal sealed class SearchOptions
    {
        public SortMode Sort;
        public TypeFilter Type;
        public SourceFilter Source;

        /// <summary>
        /// Mod name to keep, or null for every mod. Session-only (the mod list depends on the world).
        /// </summary>
        public string Mod;

        public bool Accepts(ItemName name)
        {
            if (Type != TypeFilter.All && (int)name.Category != (int)Type - 1)
            {
                return false;
            }
            return Mod == null || name.Mod == Mod;
        }

        public bool Accepts(StorageKind kind) => Source == SourceFilter.All || (int)kind == (int)Source - 1;

        /// <summary>Changes that alter which storages count (and therefore totals) need a fresh snapshot.</summary>
        public int SnapshotKey => (int)Source;
    }
}
