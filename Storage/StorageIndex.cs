using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoardFinder.Storage
{
    /// <summary>All storage sources behind one Collect call. A failing source is logged once and skipped.</summary>
    internal sealed class StorageIndex
    {
        private readonly List<IStorageSource> _sources = new List<IStorageSource>(2);
        private readonly HashSet<IStorageSource> _failed = new HashSet<IStorageSource>();

        public void Add(IStorageSource source)
        {
            if (source != null)
            {
                _sources.Add(source);
                Log.Info("Storage source: " + source.Description + ".");
            }
        }

        public int LoadedCount
        {
            get
            {
                int total = 0;
                foreach (IStorageSource source in _sources)
                {
                    total += source.LoadedCount;
                }
                return total;
            }
        }

        public void Collect(Vector3 center, float radius, List<StorageEntry> results)
        {
            results.Clear();
            foreach (IStorageSource source in _sources)
            {
                try
                {
                    source.Collect(center, radius, results);
                }
                catch (Exception e)
                {
                    if (_failed.Add(source))
                    {
                        Log.Warning("Storage source failed, skipping it: " + source.Description + ": " + e.Message);
                    }
                }
            }
        }

        /// <summary>Distinct storages in <paramref name="entries"/> (one container can hold many items).</summary>
        public static int CountStorages(List<StorageEntry> entries, HashSet<int> scratch)
        {
            scratch.Clear();
            foreach (StorageEntry entry in entries)
            {
                if (entry.Source != null)
                {
                    scratch.Add(entry.Source.GetInstanceID());
                }
            }
            return scratch.Count;
        }

        public void ResetSession()
        {
            foreach (IStorageSource source in _sources)
            {
                source.ResetSession();
            }
            _failed.Clear();
        }
    }
}
