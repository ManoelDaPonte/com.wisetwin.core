using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WiseTwin.UI
{
    /// <summary>
    /// Builds icons as VisualElement compositions (rectangles, borders, rotations) instead
    /// of relying on Unicode glyphs in the font. Some Unicode symbols (▶ ⚠ ↻ ✕) are missing
    /// from the WebGL build's bundled font and produce empty boxes in production, even
    /// though they render fine in the Editor (which falls back to system fonts).
    ///
    /// Every method returns a fresh VisualElement sized to <c>size</c>×<c>size</c>, ready to
    /// be added to a parent. Use <see cref="UIStyles.SetButtonIcon"/> to drop the icon
    /// inside a Button and have it centered correctly.
    ///
    /// Complex shapes (Check, Reset, Warning) are baked to a small Texture2D drawn with
    /// anti-aliased line rasterisation — much more reliable than CSS-style rotations
    /// which can be fragile across UI Toolkit versions. Textures are cached by
    /// (icon type, size, color) so repeated calls reuse the same allocation.
    /// </summary>
    public static class WiseTwinIcons
    {
        // ─────────────────────────────────────────────────────────────
        //  Texture-baked icon helpers (used for shapes that are hard to
        //  draw reliably with VisualElement + transform).
        // ─────────────────────────────────────────────────────────────

        static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        static VisualElement BakedIcon(string cacheKey, int size, Color color, Action<Color[], int> drawFn)
        {
            string key = $"{cacheKey}_{size}_{(int)(color.r * 255)}_{(int)(color.g * 255)}_{(int)(color.b * 255)}_{(int)(color.a * 255)}";
            if (!_textureCache.TryGetValue(key, out var tex) || tex == null)
            {
                int dim = Mathf.Max(8, size * 2); // 2× super-sample for crisper edges after bilinear filtering
                var px = new Color[dim * dim];
                drawFn(px, dim);
                tex = new Texture2D(dim, dim, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                tex.SetPixels(px);
                tex.Apply();
                _textureCache[key] = tex;
            }

            var el = new VisualElement { name = "icon-" + cacheKey };
            el.style.width = size;
            el.style.height = size;
            el.style.backgroundImage = new StyleBackground(tex);
            return el;
        }

        /// <summary>Anti-aliased line rasterisation. (x0,y0)-(x1,y1) in pixel space, top-left origin.</summary>
        static void DrawLine(Color[] px, int dim, float x0, float y0, float x1, float y1, float thickness, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - thickness));
            int maxX = Mathf.Min(dim - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - thickness));
            int maxY = Mathf.Min(dim - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + thickness));

            float halfT = thickness * 0.5f;
            float dx = x1 - x0, dy = y1 - y0;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f) return;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float t = Mathf.Clamp01(((fx - x0) * dx + (fy - y0) * dy) / lenSq);
                    float ix = x0 + t * dx, iy = y0 + t * dy;
                    float dist = Mathf.Sqrt((fx - ix) * (fx - ix) + (fy - iy) * (fy - iy));

                    if (dist <= halfT)
                    {
                        // Texture origin is bottom-left, screen origin is top-left → flip Y
                        px[(dim - 1 - y) * dim + x] = color;
                    }
                    else if (dist < halfT + 1.5f)
                    {
                        float alpha = 1f - (dist - halfT) / 1.5f;
                        var c = new Color(color.r, color.g, color.b, color.a * alpha);
                        int idx = (dim - 1 - y) * dim + x;
                        // Alpha-blend over whatever's there
                        var existing = px[idx];
                        px[idx] = Color.Lerp(existing, c, alpha);
                    }
                }
            }
        }

        /// <summary>Filled disc, used for bullet dots inside warning ! mark.</summary>
        static void FillDisc(Color[] px, int dim, float cx, float cy, float r, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
            int maxX = Mathf.Min(dim - 1, Mathf.CeilToInt(cx + r + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
            int maxY = Mathf.Min(dim - 1, Mathf.CeilToInt(cy + r + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    if (d <= r)
                    {
                        px[(dim - 1 - y) * dim + x] = color;
                    }
                    else if (d < r + 1f)
                    {
                        float alpha = 1f - (d - r);
                        int idx = (dim - 1 - y) * dim + x;
                        px[idx] = Color.Lerp(px[idx], new Color(color.r, color.g, color.b, color.a * alpha), alpha);
                    }
                }
            }
        }

        /// <summary>Filled triangle (3 points), used for warning triangle.</summary>
        static void FillTriangle(Color[] px, int dim, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(dim - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(dim - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTri(p, a, b, c))
                    {
                        px[(dim - 1 - y) * dim + x] = color;
                    }
                }
            }
        }

        static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(neg && pos);
        }

        static float Sign(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);


        // ─────────────────────────────────────────────────────────────
        //  Triangles & arrows (drawn via the CSS border trick — universally reliable)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Right-pointing solid triangle — for "play" buttons.</summary>
        public static VisualElement PlayTriangle(int size, Color color)
        {
            var el = new VisualElement { name = "icon-play" };
            int half = Mathf.Max(1, size / 2);
            int wide = Mathf.Max(1, Mathf.RoundToInt(size * 0.85f));
            // Border trick: invisible top/bottom borders + colored left border = right-pointing triangle
            el.style.width = 0;
            el.style.height = 0;
            el.style.borderTopWidth = half;
            el.style.borderTopColor = Color.clear;
            el.style.borderBottomWidth = half;
            el.style.borderBottomColor = Color.clear;
            el.style.borderLeftWidth = wide;
            el.style.borderLeftColor = color;
            el.style.borderRightWidth = 0;
            return el;
        }

        /// <summary>Right-pointing arrow (line + chevron head). For "next" buttons.</summary>
        public static VisualElement ArrowRight(int size, Color color) => Arrow(size, color, pointsRight: true);

        /// <summary>Left-pointing arrow. For "back" buttons.</summary>
        public static VisualElement ArrowLeft(int size, Color color) => Arrow(size, color, pointsRight: false);

        static VisualElement Arrow(int size, Color color, bool pointsRight)
        {
            var container = new VisualElement { name = pointsRight ? "icon-arrow-right" : "icon-arrow-left" };
            container.style.width = size;
            container.style.height = size;

            float thickness = Mathf.Max(2f, size * 0.13f);

            // Horizontal shaft
            var shaft = new VisualElement();
            shaft.style.position = Position.Absolute;
            shaft.style.left = size * 0.10f;
            shaft.style.top = (size - thickness) * 0.5f;
            shaft.style.width = size * 0.80f;
            shaft.style.height = thickness;
            shaft.style.backgroundColor = color;
            container.Add(shaft);

            // Two diagonals forming the arrowhead
            float headLen = size * 0.45f;
            float headTop = pointsRight ? -size * 0.18f : -size * 0.18f;

            var head1 = new VisualElement();
            head1.style.position = Position.Absolute;
            head1.style.width = headLen;
            head1.style.height = thickness;
            head1.style.backgroundColor = color;
            head1.style.transformOrigin = new TransformOrigin(pointsRight ? Length.Percent(100) : Length.Percent(0), Length.Percent(50));
            head1.style.rotate = new Rotate(pointsRight ? 45f : -45f);
            head1.style.left = pointsRight ? size * 0.45f : size * 0.10f;
            head1.style.top = (size - thickness) * 0.5f;
            container.Add(head1);

            var head2 = new VisualElement();
            head2.style.position = Position.Absolute;
            head2.style.width = headLen;
            head2.style.height = thickness;
            head2.style.backgroundColor = color;
            head2.style.transformOrigin = new TransformOrigin(pointsRight ? Length.Percent(100) : Length.Percent(0), Length.Percent(50));
            head2.style.rotate = new Rotate(pointsRight ? -45f : 45f);
            head2.style.left = pointsRight ? size * 0.45f : size * 0.10f;
            head2.style.top = (size - thickness) * 0.5f;
            container.Add(head2);

            return container;
        }

        // ─────────────────────────────────────────────────────────────
        //  Crosses & checks (rotated bars)
        // ─────────────────────────────────────────────────────────────

        /// <summary>X close button (✕) — two diagonal bars forming an X.</summary>
        public static VisualElement CloseX(int size, Color color) => Cross(size, color);

        /// <summary>Cross / wrong-answer indicator (✗) — same drawing as CloseX.</summary>
        public static VisualElement Cross(int size, Color color)
        {
            var container = new VisualElement { name = "icon-cross" };
            container.style.width = size;
            container.style.height = size;

            float thickness = Mathf.Max(2f, size * 0.13f);
            float barLen = size * 0.85f;

            for (int i = 0; i < 2; i++)
            {
                var bar = new VisualElement();
                bar.style.position = Position.Absolute;
                bar.style.width = barLen;
                bar.style.height = thickness;
                bar.style.backgroundColor = color;
                bar.style.left = (size - barLen) * 0.5f;
                bar.style.top = (size - thickness) * 0.5f;
                bar.style.rotate = new Rotate(i == 0 ? 45f : -45f);
                container.Add(bar);
            }
            return container;
        }

        /// <summary>Check mark (✓) — baked to a Texture2D for pixel-perfect rendering.</summary>
        public static VisualElement Check(int size, Color color)
        {
            return BakedIcon("check", size, color, (px, dim) =>
            {
                float t = Mathf.Max(2f, dim * 0.13f);
                // Two strokes meeting at the lower-left, forming a V shape
                Vector2 hookEnd  = new Vector2(dim * 0.18f, dim * 0.50f);
                Vector2 meetPt   = new Vector2(dim * 0.40f, dim * 0.72f);
                Vector2 tipEnd   = new Vector2(dim * 0.86f, dim * 0.26f);

                DrawLine(px, dim, hookEnd.x, hookEnd.y, meetPt.x, meetPt.y, t, color);
                DrawLine(px, dim, meetPt.x, meetPt.y, tipEnd.x, tipEnd.y, t, color);
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  Reset (↻) — circular arrow approximated with a rotated U
        // ─────────────────────────────────────────────────────────────

        /// <summary>Counterclockwise circular arrow (↻) — baked to Texture2D for reliability.</summary>
        public static VisualElement Reset(int size, Color color)
        {
            return BakedIcon("reset", size, color, (px, dim) =>
            {
                float t = Mathf.Max(2f, dim * 0.11f);
                float cx = dim * 0.5f, cy = dim * 0.55f;
                float r = dim * 0.30f;

                // Arc covering ~270°, going clockwise from upper-left (~-150°) all the way
                // around to top (-90°). The 90° gap at the top-right is where the arrowhead lives.
                int segs = 90;
                for (int i = 0; i < segs; i++)
                {
                    float a0Deg = -150f + (i / (float)segs) * 270f;
                    float a1Deg = -150f + ((i + 1) / (float)segs) * 270f;
                    float a0 = a0Deg * Mathf.Deg2Rad;
                    float a1 = a1Deg * Mathf.Deg2Rad;
                    DrawLine(px, dim,
                        cx + r * Mathf.Cos(a0), cy + r * Mathf.Sin(a0),
                        cx + r * Mathf.Cos(a1), cy + r * Mathf.Sin(a1),
                        t, color);
                }

                // Big, obvious arrowhead at the upper-left end of the arc (-150°).
                // Tip points along the tangent (continuing the rotation, i.e. up-and-right);
                // the two base corners flank it perpendicular to that tangent.
                float endRad = -150f * Mathf.Deg2Rad;
                Vector2 endPt = new Vector2(cx + r * Mathf.Cos(endRad), cy + r * Mathf.Sin(endRad));
                // Tangent vector for clockwise traversal at angle θ is (sin θ, -cos θ).
                Vector2 tangent = new Vector2(Mathf.Sin(endRad), -Mathf.Cos(endRad));
                // Perpendicular (radial) — used to spread the arrowhead's base corners.
                Vector2 perp = new Vector2(-tangent.y, tangent.x);

                float headLen = dim * 0.26f;
                float headHalfWidth = dim * 0.16f;
                Vector2 tip = endPt + tangent * headLen;
                Vector2 wingA = endPt + perp * headHalfWidth;
                Vector2 wingB = endPt - perp * headHalfWidth;
                FillTriangle(px, dim, tip, wingA, wingB, color);
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  Warning triangle (⚠) — outlined triangle with a "!" inside
        // ─────────────────────────────────────────────────────────────

        /// <summary>Warning triangle (⚠) with white "!" punched out — baked to Texture2D.</summary>
        public static VisualElement Warning(int size, Color color)
        {
            return BakedIcon("warning", size, color, (px, dim) =>
            {
                // Big triangle filling almost the entire icon for visual weight
                Vector2 top = new Vector2(dim * 0.50f, dim * 0.04f);
                Vector2 left = new Vector2(dim * 0.02f, dim * 0.94f);
                Vector2 right = new Vector2(dim * 0.98f, dim * 0.94f);
                FillTriangle(px, dim, top, left, right, color);

                // Compact exclamation mark — proportionally smaller than before
                Color markColor = Color.white;
                float stemThickness = Mathf.Max(2f, dim * 0.07f);
                // Shorter stem (was 0.32-0.62, now 0.42-0.62 = 20% of icon height)
                DrawLine(px, dim, dim * 0.5f, dim * 0.42f, dim * 0.5f, dim * 0.62f, stemThickness, markColor);
                // Smaller dot tucked closer to the stem
                FillDisc(px, dim, dim * 0.5f, dim * 0.74f, stemThickness * 0.55f, markColor);
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  Small decorative shapes
        // ─────────────────────────────────────────────────────────────

        /// <summary>Right-facing single chevron (›) for breadcrumb separators.</summary>
        public static VisualElement Chevron(int size, Color color)
        {
            var container = new VisualElement { name = "icon-chevron" };
            container.style.width = size;
            container.style.height = size;

            float thickness = Mathf.Max(2f, size * 0.16f);
            float armLen = size * 0.45f;

            for (int i = 0; i < 2; i++)
            {
                var arm = new VisualElement();
                arm.style.position = Position.Absolute;
                arm.style.width = armLen;
                arm.style.height = thickness;
                arm.style.backgroundColor = color;
                arm.style.left = size * 0.35f;
                arm.style.top = (size - thickness) * 0.5f;
                arm.style.rotate = new Rotate(i == 0 ? 45f : -45f);
                arm.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
                container.Add(arm);
            }
            return container;
        }

        /// <summary>Filled circular bullet (•) for list rendering.</summary>
        public static VisualElement Bullet(int size, Color color)
        {
            var dot = new VisualElement { name = "icon-bullet" };
            dot.style.width = size;
            dot.style.height = size;
            dot.style.backgroundColor = color;
            dot.style.borderTopLeftRadius = size * 0.5f;
            dot.style.borderTopRightRadius = size * 0.5f;
            dot.style.borderBottomLeftRadius = size * 0.5f;
            dot.style.borderBottomRightRadius = size * 0.5f;
            return dot;
        }

        /// <summary>6-dot grip glyph (2 rows × 3 dots) used to hint that an element is draggable.</summary>
        public static VisualElement DragHandle(int size, Color color)
        {
            var container = new VisualElement { name = "icon-drag-handle" };
            container.style.width = size;
            container.style.height = size * 0.45f;
            container.style.flexDirection = FlexDirection.Column;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.alignItems = Align.Center;

            int dotSize = Mathf.Max(2, Mathf.RoundToInt(size * 0.13f));
            for (int row = 0; row < 2; row++)
            {
                var rowEl = new VisualElement();
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.justifyContent = Justify.SpaceBetween;
                rowEl.style.width = size * 0.6f;
                for (int col = 0; col < 3; col++)
                {
                    rowEl.Add(Bullet(dotSize, color));
                }
                container.Add(rowEl);
            }
            return container;
        }
    }
}
