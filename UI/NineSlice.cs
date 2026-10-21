using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Draws an atlas sprite as a 9-slice with GUI.DrawTextureWithTexCoords: corners keep their size, edges and
    /// center stretch. Texture coordinates start at the bottom-left, GUI rects at the top-left, so the top GUI row
    /// samples the top of the sprite.
    /// </summary>
    internal static class NineSlice
    {
        // Reused scratch arrays: IMGUI runs on the main thread only, so drawing allocates nothing.
        private static readonly float[] Xs = new float[4];
        private static readonly float[] Ys = new float[4];
        private static readonly float[] Us = new float[4];
        private static readonly float[] Vs = new float[4];

        public static void Draw(Sprite sprite, Rect uv, Rect rect, float borderScale)
        {
            Texture2D texture = sprite.texture;
            Vector4 border = sprite.border; // x = left, y = bottom, z = right, w = top (pixels)
            float tw = texture.width;
            float th = texture.height;

            float left = Mathf.Min(border.x * borderScale, rect.width / 2f);
            float right = Mathf.Min(border.z * borderScale, rect.width / 2f);
            float top = Mathf.Min(border.w * borderScale, rect.height / 2f);
            float bottom = Mathf.Min(border.y * borderScale, rect.height / 2f);

            Set(Xs, rect.x, rect.x + left, rect.xMax - right, rect.xMax);
            Set(Ys, rect.y, rect.y + top, rect.yMax - bottom, rect.yMax);
            Set(Us, uv.xMin, uv.xMin + border.x / tw, uv.xMax - border.z / tw, uv.xMax);
            // GUI rows top to bottom map to texture rows top to bottom (descending v).
            Set(Vs, uv.yMax, uv.yMax - border.w / th, uv.yMin + border.y / th, uv.yMin);
            DrawPatches(texture);
        }

        private static void DrawPatches(Texture2D texture)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var target = new Rect(Xs[col], Ys[row], Xs[col + 1] - Xs[col], Ys[row + 1] - Ys[row]);
                    if (target.width <= 0f || target.height <= 0f)
                    {
                        continue;
                    }
                    var coords = new Rect(Us[col], Vs[row + 1], Us[col + 1] - Us[col], Vs[row] - Vs[row + 1]);
                    GUI.DrawTextureWithTexCoords(target, texture, coords);
                }
            }
        }

        private static void Set(float[] target, float a, float b, float c, float d)
        {
            target[0] = a;
            target[1] = b;
            target[2] = c;
            target[3] = d;
        }

        /// <summary>
        /// Normalized rect of the sprite inside its texture, from its UVs (valid for tight packing too).
        /// <c>Sprite.uv</c> allocates, so callers cache the result.
        /// </summary>
        public static Rect UvRect(Sprite sprite)
        {
            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }
            Vector2 min = uvs[0];
            Vector2 max = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                min = Vector2.Min(min, uvs[i]);
                max = Vector2.Max(max, uvs[i]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
