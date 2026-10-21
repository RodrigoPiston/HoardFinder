using System.Collections.Generic;
using UnityEngine;

namespace HoardFinder.Storage
{
    /// <summary>
    /// Every vanilla or modded <see cref="Container"/> (chests, carts, ships, tombstones, dungeon loot). Containers
    /// register themselves through a Container.Awake postfix (Patches/ContainerPatches.cs), so there is no scene scan.
    /// Container reloads its inventory from the replicated ZDO every second on every client
    /// (Container.CheckForChanges), so <c>GetInventory()</c> is current without opening anything. Read-only.
    /// </summary>
    internal sealed class ContainerSource : IStorageSource
    {
        private sealed class Registered
        {
            public Container Container;
            public Piece Piece;
            public GameObject Target;
            public StorageKind Kind;
        }

        private static readonly List<Registered> All = new List<Registered>(256);
        private readonly Dictionary<string, int> _perItem = new Dictionary<string, int>(32);
        private readonly HashSet<StorageKind> _enabled;

        public ContainerSource(HashSet<StorageKind> enabledKinds)
        {
            _enabled = enabledKinds;
        }

        public string Description => "Container (chests, carts, ships, tombstones, world)";

        public int LoadedCount => All.Count;

        /// <summary>Called from the Container.Awake postfix.</summary>
        public static void Register(Container container)
        {
            if (container == null || container.GetInventory() == null)
            {
                return;
            }
            ZNetView root = container.m_rootObjectOverride;
            All.Add(new Registered
            {
                Container = container,
                Piece = container.GetComponentInParent<Piece>(),
                Target = root != null ? root.gameObject : container.gameObject,
                Kind = Classify(container, root),
            });
        }

        private static StorageKind Classify(Container container, ZNetView root)
        {
            if (container.GetComponent<TombStone>() != null)
            {
                return StorageKind.Tombstone;
            }
            bool vehicle = container.GetComponentInParent<Ship>() != null
                || container.GetComponentInParent<Vagon>() != null;
            if (root != null || vehicle)
            {
                return StorageKind.Vehicle;
            }
            return container.GetComponentInParent<Piece>() != null ? StorageKind.Chest : StorageKind.World;
        }

        public void Collect(Vector3 center, float radius, List<StorageEntry> results)
        {
            float radiusSqr = radius * radius;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                Registered entry = All[i];
                if (entry.Container == null || entry.Target == null)
                {
                    All.RemoveAt(i);
                    continue;
                }
                float distanceSqr = (entry.Container.transform.position - center).sqrMagnitude;
                bool wanted = distanceSqr <= radiusSqr && _enabled.Contains(entry.Kind);
                if (wanted && StorageAccess.Container(entry.Container, entry.Piece))
                {
                    AddItems(entry, Mathf.Sqrt(distanceSqr), results);
                }
            }
        }

        /// <summary>One entry per distinct prefab in the container, with the stacks summed.</summary>
        private void AddItems(Registered entry, float distance, List<StorageEntry> results)
        {
            Inventory inventory = entry.Container.GetInventory();
            if (inventory == null)
            {
                return;
            }
            _perItem.Clear();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                string prefab = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
                if (prefab != null && item.m_stack > 0)
                {
                    _perItem.TryGetValue(prefab, out int count);
                    _perItem[prefab] = count + item.m_stack;
                }
            }
            foreach (KeyValuePair<string, int> stack in _perItem)
            {
                results.Add(new StorageEntry
                {
                    Source = entry.Container,
                    Target = entry.Target,
                    Item = stack.Key,
                    Count = stack.Value,
                    Distance = distance,
                    Kind = entry.Kind,
                });
            }
        }

        public void ResetSession() => All.Clear();
    }
}
