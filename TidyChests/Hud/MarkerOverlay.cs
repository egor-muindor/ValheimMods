using System.Collections.Generic;
using TidyChests.Find;
using UnityEngine;

namespace TidyChests.Hud
{
    /// <summary>
    /// Draws the screen markers with IMGUI: an arrow over the chest (or at the screen edge
    /// pointing at it) and a label with the item count and distance. IMGUI needs no canvas
    /// and no font asset, so it works in any build. Call from <c>OnGUI</c> only.
    /// </summary>
    internal static class MarkerOverlay
    {
        private const int ArrowTextureSize = 48;

        private const float ArrowSize = 32f;

        private const float EdgeMargin = 56f;

        private static Texture2D? _arrow;

        private static GUIStyle? _label;

        public static void Draw(IReadOnlyList<ChestHighlight> highlights, Camera camera, Vector3 playerPosition)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Texture2D arrow = _arrow ??= CreateArrow(ArrowTextureSize);
            GUIStyle label = _label ??= CreateLabelStyle();
            float width = Screen.width;
            float height = Screen.height;
            var center = new Vector2(width / 2f, height / 2f);

            foreach (ChestHighlight highlight in highlights)
            {
                Vector3 viewport = camera.WorldToViewportPoint(highlight.Position);
                ScreenMarker marker = ScreenMarkerMath.Compute(viewport.x, viewport.y, viewport.z, width, height, EdgeMargin);
                float alpha = highlight.Alpha;
                Color color = ChestHighlight.Color;
                color.a = alpha;
                float distance = Vector3.Distance(playerPosition, highlight.Position);
                var content = new GUIContent($"{highlight.Label}  {distance:0} m");
                Vector2 size = label.CalcSize(content);

                if (marker.OnScreen)
                {
                    var tip = new Vector2(marker.X, marker.Y - 10f);
                    DrawArrow(arrow, tip, marker.AngleDegrees, color);
                    DrawLabel(label, content, size, new Vector2(tip.x, tip.y - ArrowSize - 8f - size.y / 2f), alpha);
                }
                else
                {
                    var tip = new Vector2(marker.X, marker.Y);
                    DrawArrow(arrow, tip, marker.AngleDegrees, color);
                    // Past the arrow's base, then by the label's own half extent along that direction,
                    // so a wide label on a side edge does not cover the arrow.
                    Vector2 inward = (center - tip).normalized;
                    float offset = ArrowSize + 8f + Mathf.Abs(inward.x) * size.x / 2f + Mathf.Abs(inward.y) * size.y / 2f;
                    DrawLabel(label, content, size, tip + inward * offset, alpha);
                }
            }
        }

        /// <summary>Draws the arrow with its tip at <paramref name="tip"/>, rotated clockwise by <paramref name="angle"/> (0 = pointing up).</summary>
        private static void DrawArrow(Texture2D arrow, Vector2 tip, float angle, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            Color previous = GUI.color;
            GUIUtility.RotateAroundPivot(angle, tip);

            // Dark outline: the same arrow, slightly larger, behind the coloured one.
            float outline = ArrowSize * 1.25f;
            GUI.color = new Color(0f, 0f, 0f, 0.7f * color.a);
            GUI.DrawTexture(new Rect(tip.x - outline / 2f, tip.y - (outline - ArrowSize) / 2f, outline, outline), arrow);

            GUI.color = color;
            GUI.DrawTexture(new Rect(tip.x - ArrowSize / 2f, tip.y, ArrowSize, ArrowSize), arrow);

            GUI.color = previous;
            GUI.matrix = matrix;
        }

        private static void DrawLabel(GUIStyle style, GUIContent content, Vector2 size, Vector2 center, float alpha)
        {
            var rect = new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y);
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), content, style);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, content, style);

            GUI.color = previous;
        }

        private static GUIStyle CreateLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
            };
            style.normal.textColor = Color.white;
            return style;
        }

        /// <summary>A white, up-pointing triangle on a transparent background; tinted with GUI.color when drawn.</summary>
        private static Texture2D CreateArrow(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "TidyChests arrow",
            };

            // Tip at the top centre, base along the bottom; 4 samples per pixel for soft edges.
            float tipX = size / 2f;
            float tipY = size - 1f;
            float baseY = 1f;
            float halfBase = size / 2f - 1f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float px = x + 0.25f + sx * 0.5f;
                            float py = y + 0.25f + sy * 0.5f;
                            float span = (py - baseY) / (tipY - baseY);
                            float halfWidthAtY = halfBase * (1f - span);
                            if (py >= baseY && py <= tipY && Mathf.Abs(px - tipX) <= halfWidthAtY)
                            {
                                inside++;
                            }
                        }
                    }

                    byte alpha = (byte)(inside * 255 / 4);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
