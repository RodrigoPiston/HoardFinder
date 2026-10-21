using System;

namespace HoardFinder.Search
{
    /// <summary>One cached comparison per sort mode; every mode falls back to name, so the order is stable.</summary>
    internal static class SearchSort
    {
        private static readonly Comparison<SearchResult> ByName =
            (a, b) => string.Compare(a.Name.Display, b.Name.Display, StringComparison.CurrentCultureIgnoreCase);

        private static readonly Comparison<SearchResult> ByRelevance = (a, b) =>
        {
            int rank = a.Rank.CompareTo(b.Rank);
            return rank != 0 ? rank : ThenAmount(a, b);
        };

        private static readonly Comparison<SearchResult> ByAmount = ThenAmount;

        private static readonly Comparison<SearchResult> ByDistance = (a, b) =>
        {
            int distance = a.Closest.CompareTo(b.Closest);
            return distance != 0 ? distance : ByName(a, b);
        };

        private static readonly Comparison<SearchResult> ByType = (a, b) =>
        {
            int category = a.Name.Category.CompareTo(b.Name.Category);
            return category != 0 ? category : ByName(a, b);
        };

        /// <summary>Vanilla first, then mods alphabetically; items by name inside each mod.</summary>
        private static readonly Comparison<SearchResult> ByMod = (a, b) =>
        {
            int vanilla = b.Name.IsVanilla.CompareTo(a.Name.IsVanilla);
            if (vanilla != 0)
            {
                return vanilla;
            }
            int mod = string.Compare(a.Name.Mod, b.Name.Mod, StringComparison.OrdinalIgnoreCase);
            return mod != 0 ? mod : ByName(a, b);
        };

        private static int ThenAmount(SearchResult a, SearchResult b)
        {
            int total = b.Total.CompareTo(a.Total);
            return total != 0 ? total : ByName(a, b);
        }

        public static Comparison<SearchResult> For(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.Name:
                    return ByName;
                case SortMode.Amount:
                    return ByAmount;
                case SortMode.Distance:
                    return ByDistance;
                case SortMode.Type:
                    return ByType;
                case SortMode.Mod:
                    return ByMod;
                default:
                    return ByRelevance;
            }
        }
    }
}
