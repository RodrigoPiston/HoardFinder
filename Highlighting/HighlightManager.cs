using System.Collections.Generic;
using HoardFinder.Storage;
using HoardFinder.Search;
using HoardFinder.UI;
using UnityEngine;

namespace HoardFinder.Highlighting
{
    /// <summary>
    /// Highlights drawers with three layers: a pulsing emission tint through Valheim's own MaterialMan property
    /// blocks (no material is cloned or modified), a vertical light beam, and a screen-space marker drawn in OnGUI
    /// that stays visible through walls. Each highlighted item gets its own color. Beams live under one root object
    /// owned by this class, never under the drawer, so BetrValheim and MaterialMan never see foreign renderers.
    /// </summary>
    internal sealed class HighlightManager
    {
        private sealed class Marker
        {
            public Component Storage;
            public GameObject Target;
            public Vector3 Anchor;
            public LineRenderer Beam;
            public bool Glow;
            public Color Color;
            public string Name;
            public string CountText;
            public float Distance;
            public int Meters = -1;
            public string Text;
        }

        private const float PulseInterval = 0.1f;
        private const int LabeledMarkers = 8;
        private const float BeamGrowSeconds = 0.45f;
        private const float PopSeconds = 0.35f;
        private const float PopStagger = 0.03f;
        private const int PopStaggerCap = 20;

        private readonly List<Marker> _markers = new List<Marker>(64);
        private readonly Dictionary<string, Color> _colorByItem = new Dictionary<string, Color>();
        private readonly ScreenMarkers _screen = new ScreenMarkers();
        private readonly Beams _beams = new Beams();
        private float _until;
        private float _nextPulse;
        private float _shownAt = float.NegativeInfinity;

        private static readonly System.Comparison<Marker> ByDistance = (a, b) => a.Distance.CompareTo(b.Distance);

        public bool Active => _markers.Count > 0;

        /// <summary>
        /// Replaces the current highlight. Storages may hold different items; each item gets a color.
        /// </summary>
        public void Show(List<StorageEntry> storages, float duration)
        {
            Clear();
            foreach (StorageEntry entry in storages)
            {
                if (entry.Source != null && entry.Target != null)
                {
                    _markers.Add(CreateMarker(entry));
                }
            }
            _until = Time.time + duration;
            _nextPulse = 0f;
            _shownAt = Anim.Now;
            Log.Info("Highlighting " + _markers.Count + " storages (" + _colorByItem.Count + " items) for "
                + duration + "s.");
        }

        private Marker CreateMarker(StorageEntry entry)
        {
            // Carts and ships: mark the container itself; glowing or measuring the whole vehicle would mislead.
            bool vehicle = entry.Kind == StorageKind.Vehicle;
            Vector3 anchor = vehicle ? entry.Source.transform.position + Vector3.up * 0.5f : TopOf(entry.Target);
            Color color = ColorFor(entry.Item);
            return new Marker
            {
                Storage = entry.Source,
                Target = entry.Target,
                Glow = !vehicle,
                Anchor = anchor,
                Beam = _beams.Create(anchor, color, 0f),
                Color = color,
                Name = ItemNames.Get(entry.Item).Display,
                CountText = entry.Count.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
                Distance = entry.Distance,
            };
        }

        private Color ColorFor(string item)
        {
            if (!_colorByItem.TryGetValue(item, out Color color))
            {
                color = Palette.ForIndex(_colorByItem.Count);
                _colorByItem[item] = color;
            }
            return color;
        }

        private static Vector3 TopOf(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return target.transform.position + Vector3.up;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        public void Tick()
        {
            if (_markers.Count == 0)
            {
                return;
            }
            if (Time.time >= _until)
            {
                Clear();
                return;
            }
            GrowBeams();
            if (Time.time >= _nextPulse)
            {
                _nextPulse = Time.time + PulseInterval;
                PruneAndMeasure();
                Pulse();
            }
        }

        /// <summary>
        /// Beams rise out of the drawers during the first <see cref="BeamGrowSeconds"/>, every frame.
        /// </summary>
        private void GrowBeams()
        {
            float t = Anim.Progress(_shownAt, BeamGrowSeconds);
            if (Anim.Now - _shownAt > BeamGrowSeconds + 0.1f)
            {
                return;
            }
            float height = Anim.EaseOutCubic(t);
            foreach (Marker marker in _markers)
            {
                Beams.SetHeight(marker.Beam, marker.Anchor, height);
            }
        }

        /// <summary>Drops destroyed drawers and keeps markers sorted nearest-first, at pulse rate (10 Hz).</summary>
        private void PruneAndMeasure()
        {
            Player player = Player.m_localPlayer;
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                Marker marker = _markers[i];
                if (marker.Storage == null || marker.Target == null)
                {
                    _beams.Destroy(marker.Beam);
                    _markers.RemoveAt(i);
                    continue;
                }
                marker.Distance = Vector3.Distance(origin, marker.Anchor);
            }
            _markers.Sort(ByDistance);
        }

        private void Pulse()
        {
            MaterialMan materials = MaterialMan.instance;
            if (materials == null)
            {
                return;
            }
            float wave = 0.35f + 0.35f * Mathf.Sin(Time.time * 6f);
            foreach (Marker marker in _markers)
            {
                if (marker.Glow)
                {
                    materials.SetValue(marker.Target, ShaderProps._EmissionColor, marker.Color * wave);
                }
            }
        }

        public void OnGUI()
        {
            if (_markers.Count == 0 || Event.current.type != EventType.Repaint)
            {
                return;
            }
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }
            float remaining = Mathf.Max(0f, _until - Time.time);
            // Draw far-to-near so the nearest (labeled) markers end up on top.
            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                Marker marker = _markers[i];
                bool labeled = i < LabeledMarkers;
                float popStart = _shownAt + Mathf.Min(i, PopStaggerCap) * PopStagger;
                float pop = Anim.EaseOutBack(Anim.Progress(popStart, PopSeconds));
                string label = labeled ? LabelOf(marker) : null;
                _screen.Draw(camera, marker.Anchor, marker.Color, label, i == 0, remaining, pop);
            }
        }

        /// <summary>Rebuilt only when the rounded distance changes, so steady frames allocate nothing.</summary>
        private static string LabelOf(Marker marker)
        {
            int meters = Mathf.RoundToInt(marker.Distance);
            if (meters != marker.Meters)
            {
                marker.Meters = meters;
                marker.Text = marker.Name + "  " + marker.CountText + "  ·  " + meters + "m";
            }
            return marker.Text;
        }

        public void Clear()
        {
            MaterialMan materials = MaterialMan.instance;
            foreach (Marker marker in _markers)
            {
                if (materials != null && marker.Glow && marker.Target != null)
                {
                    materials.ResetValue(marker.Target, ShaderProps._EmissionColor);
                }
                _beams.Destroy(marker.Beam);
            }
            _markers.Clear();
            _colorByItem.Clear();
        }

        public void Dispose()
        {
            Clear();
            _beams.Dispose();
            _screen.Dispose();
        }
    }
}
