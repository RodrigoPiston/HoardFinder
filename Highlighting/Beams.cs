using UnityEngine;

namespace HoardFinder.Highlighting
{
    /// <summary>Owns the beam root object and the single shared beam material.</summary>
    internal sealed class Beams
    {
        private const float Height = 8f;

        private GameObject _root;
        private Material _material;
        private bool _shaderMissing;

        /// <param name="growth">0..1 fraction of the full beam height.</param>
        public LineRenderer Create(Vector3 anchor, Color color, float growth)
        {
            Material material = SharedMaterial();
            if (material == null)
            {
                return null;
            }
            EnsureRoot();
            var go = new GameObject("HoardFinder_Beam");
            go.transform.SetParent(_root.transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, anchor);
            line.SetPosition(1, anchor + Vector3.up * Mathf.Max(0.01f, Height * growth));
            line.startWidth = 0.12f;
            line.endWidth = 0.02f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        public static void SetHeight(LineRenderer beam, Vector3 anchor, float growth)
        {
            if (beam != null)
            {
                beam.SetPosition(1, anchor + Vector3.up * Mathf.Max(0.01f, Height * growth));
            }
        }

        public void Destroy(LineRenderer beam)
        {
            if (beam != null)
            {
                Object.Destroy(beam.gameObject);
            }
        }

        private Material SharedMaterial()
        {
            if (_material != null || _shaderMissing)
            {
                return _material;
            }
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                _shaderMissing = true;
                Log.Warning("Sprites/Default shader not found; beams disabled, markers and glow still work.");
                return null;
            }
            _material = new Material(shader) { name = "HoardFinder_Beam" };
            return _material;
        }

        private void EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("HoardFinder_Highlights");
                Object.DontDestroyOnLoad(_root);
            }
        }

        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }
            if (_material != null)
            {
                Object.Destroy(_material);
            }
            _root = null;
            _material = null;
        }
    }

    /// <summary>Distinct, readable marker colors; the first one is the classic gold.</summary>
    internal static class Palette
    {
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.78f, 0.2f, 1f),
            new Color(0.3f, 0.85f, 1f, 1f),
            new Color(0.55f, 1f, 0.35f, 1f),
            new Color(1f, 0.45f, 0.75f, 1f),
            new Color(0.75f, 0.55f, 1f, 1f),
            new Color(1f, 0.55f, 0.25f, 1f),
        };

        public static Color Gold => Colors[0];

        public static Color ForIndex(int index) => Colors[index % Colors.Length];
    }
}
