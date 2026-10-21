using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Bootstrap;
using HoardFinder.Storage;
using UnityEngine;

namespace HoardFinder.Drawers
{
    /// <summary>
    /// Read-only access to BetrValheim drawers. Every member is resolved once at startup; per-drawer reads go through
    /// compiled delegates. See docs/FINDINGS.md for the inspected BetrValheim surface.
    /// </summary>
    internal sealed class BetrDrawerApi : IStorageSource
    {
        public const string BetrValheimGuid = "dezzycode.valheim.betrvalheim";
        private const string DrawerTypeName = "BetrValheim.Drawers.Drawer";
        private const string RegistryTypeName = "BetrValheim.Drawers.DrawerIndex";
        private const string ZdoKeyItem = "ew_drawer_item";
        private const string ZdoKeyCount = "ew_drawer_count";
        private const float FallbackScanInterval = 5f;

        private readonly Type _drawerType;
        private readonly IList _registry;
        private readonly Func<Component, string> _readItem;
        private readonly Func<Component, int> _readCount;
        private readonly List<Component> _scanned = new List<Component>(256);
        private float _nextScan;
        private bool _readErrorLogged;

        public string Description { get; }

        /// <summary>
        /// Kinds enabled in the config; drawers are skipped when <see cref="StorageKind.Drawer"/> is off.
        /// </summary>
        public HashSet<StorageKind> EnabledKinds { get; set; }

        private BetrDrawerApi(Type drawerType, IList registry, Func<Component, string> readItem,
            Func<Component, int> readCount, string description)
        {
            _drawerType = drawerType;
            _registry = registry;
            _readItem = readItem;
            _readCount = readCount;
            Description = description;
        }

        /// <summary>Returns null (and logs why) when BetrValheim or its drawer type is missing.</summary>
        public static BetrDrawerApi TryCreate()
        {
            if (!Chainloader.PluginInfos.TryGetValue(BetrValheimGuid, out var info) || info.Instance == null)
            {
                Log.Info("BetrValheim not detected; drawer support off, containers are still searched.");
                return null;
            }
            Log.Info("BetrValheim detected (" + info.Metadata.Version + ").");
            Assembly assembly = info.Instance.GetType().Assembly;
            Type drawerType = assembly.GetType(DrawerTypeName, false);
            if (drawerType == null || !typeof(Component).IsAssignableFrom(drawerType))
            {
                Log.Warning("BetrValheim API not detected (" + DrawerTypeName + " missing). Drawer support off.");
                return null;
            }
            IList registry = ResolveRegistry(assembly);
            Func<Component, string> readItem = CompileGetter<string>(drawerType, "ItemName");
            Func<Component, int> readCount = CompileGetter<int>(drawerType, "Count");
            bool viaProperties = readItem != null && readCount != null;
            string description = drawerType.FullName
                + (registry != null ? " via DrawerIndex.All" : " via periodic scan (fallback)")
                + (viaProperties ? ", ItemName/Count properties" : ", raw ZDO keys (fallback)");
            Log.Info("Drawer API detected: " + description + ".");
            return viaProperties
                ? new BetrDrawerApi(drawerType, registry, readItem, readCount, description)
                : new BetrDrawerApi(drawerType, registry, ReadZdoItem, ReadZdoCount, description);
        }

        private static IList ResolveRegistry(Assembly assembly)
        {
            try
            {
                Type index = assembly.GetType(RegistryTypeName, false);
                const BindingFlags anyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                PropertyInfo all = index?.GetProperty("All", anyStatic);
                // DrawerIndex.All returns a static readonly List, so the reference stays valid for the session.
                return all?.GetValue(null) as IList;
            }
            catch (Exception e)
            {
                Log.Warning("DrawerIndex not usable, falling back to periodic scan: " + e.Message);
                return null;
            }
        }

        private static Func<Component, T> CompileGetter<T>(Type drawerType, string propertyName)
        {
            try
            {
                PropertyInfo property = drawerType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (property == null || property.PropertyType != typeof(T) || property.GetGetMethod() == null)
                {
                    return null;
                }
                ParameterExpression arg = Expression.Parameter(typeof(Component), "drawer");
                Expression body = Expression.Property(Expression.Convert(arg, drawerType), property);
                return Expression.Lambda<Func<Component, T>>(body, arg).Compile();
            }
            catch (Exception e)
            {
                Log.Warning("Could not bind Drawer." + propertyName + ": " + e.Message);
                return null;
            }
        }

        private static string ReadZdoItem(Component drawer)
        {
            ZDO zdo = ZdoOf(drawer);
            string item = zdo?.GetString(ZdoKeyItem, string.Empty);
            return string.IsNullOrEmpty(item) ? null : item;
        }

        private static int ReadZdoCount(Component drawer) => ZdoOf(drawer)?.GetInt(ZdoKeyCount) ?? 0;

        private static ZDO ZdoOf(Component drawer)
        {
            ZNetView view = drawer.GetComponent<ZNetView>();
            return view != null && view.IsValid() ? view.GetZDO() : null;
        }

        /// <summary>Appends non-empty, accessible drawers within <paramref name="radius"/>.</summary>
        public void Collect(Vector3 center, float radius, List<StorageEntry> results)
        {
            if (EnabledKinds != null && !EnabledKinds.Contains(StorageKind.Drawer))
            {
                return;
            }
            IList source = _registry ?? Scan();
            float radiusSqr = radius * radius;
            for (int i = 0; i < source.Count; i++)
            {
                Component drawer = source[i] as Component;
                if (drawer == null)
                {
                    continue;
                }
                float distanceSqr = (drawer.transform.position - center).sqrMagnitude;
                if (distanceSqr <= radiusSqr && TryRead(drawer, out string item, out int count)
                    && StorageAccess.Ward(drawer.transform.position))
                {
                    results.Add(Entry(drawer, item, count, Mathf.Sqrt(distanceSqr)));
                }
            }
        }

        private static StorageEntry Entry(Component drawer, string item, int count, float distance)
        {
            return new StorageEntry
            {
                Source = drawer,
                Target = drawer.gameObject,
                Item = item,
                Count = count,
                Distance = distance,
                Kind = StorageKind.Drawer,
            };
        }

        private bool TryRead(Component drawer, out string item, out int count)
        {
            try
            {
                item = _readItem(drawer);
                count = item == null ? 0 : _readCount(drawer);
                return count > 0;
            }
            catch (Exception e)
            {
                if (!_readErrorLogged)
                {
                    _readErrorLogged = true;
                    Log.Warning("Reading a drawer failed; skipping it. " + e.GetType().Name + ": " + e.Message);
                }
                item = null;
                count = 0;
                return false;
            }
        }

        private IList Scan()
        {
            if (Time.time < _nextScan)
            {
                return _scanned;
            }
            _nextScan = Time.time + FallbackScanInterval;
            _scanned.Clear();
            UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(_drawerType, FindObjectsSortMode.None);
            foreach (UnityEngine.Object candidate in found)
            {
                if (candidate is Component component)
                {
                    _scanned.Add(component);
                }
            }
            Log.Debug("Fallback scan found " + _scanned.Count + " drawers.");
            return _scanned;
        }

        /// <summary>Total drawers currently known, regardless of distance or content.</summary>
        public int LoadedCount => (_registry ?? _scanned).Count;

        public void ResetSession()
        {
            _scanned.Clear();
            _nextScan = 0f;
        }
    }
}
