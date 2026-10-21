using System.Collections.Generic;
using UnityEngine;

namespace HoardFinder.Storage
{
    /// <summary>Where an item is stored. Also used as the source filter (see <see cref="SourceFilter"/>).</summary>
    internal enum StorageKind
    {
        Drawer,
        Chest,
        Vehicle,
        Tombstone,
        World,
    }

    /// <summary>One stored item stack in one storage, as read at collection time.</summary>
    internal struct StorageEntry
    {
        /// <summary>The component that holds the items (a BetrValheim Drawer or a Container).</summary>
        public Component Source;

        /// <summary>
        /// The object to highlight (the network root of carts and ships, the storage itself otherwise).
        /// </summary>
        public GameObject Target;

        public string Item;
        public int Count;
        public float Distance;
        public StorageKind Kind;
    }

    /// <summary>A read-only provider of stored items. Collect appends to the list; it never clears it.</summary>
    internal interface IStorageSource
    {
        string Description { get; }

        int LoadedCount { get; }

        void Collect(Vector3 center, float radius, List<StorageEntry> results);

        void ResetSession();
    }
}
