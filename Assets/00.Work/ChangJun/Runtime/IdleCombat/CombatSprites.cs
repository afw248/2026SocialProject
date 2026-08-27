using System.Collections.Generic;
using UnityEngine;

namespace ChangJun.IdleCombat
{
    /// <summary>
    /// 전투 프로토타입용 절차적 스프라이트. 픽셀 필터로 2.5D 방치형 톤을 맞춘다.
    /// </summary>
    public static class CombatSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Circle(string key, Color color, int size = 32, bool soft = false)
        {
            if (Cache.TryGetValue(key, out var hit) && hit != null) return hit;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = soft
                        ? Mathf.Clamp01(r - d)
                        : (d <= r - 0.6f ? 1f : Mathf.Clamp01(r - d + 0.5f));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Ring(string key, Color color, int size = 64, float thickness = 0.12f)
        {
            if (Cache.TryGetValue(key, out var hit) && hit != null) return hit;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float r = (size - 1) * 0.5f;
            float inner = r * (1f - thickness);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - (inner + r) * 0.5f) / (r - inner));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Diamond(string key, Color color, int size = 32)
        {
            if (Cache.TryGetValue(key, out var hit) && hit != null) return hit;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = Mathf.Abs(x - c) / c + Mathf.Abs(y - c) / c <= 1.05f ? 1f : 0f;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite Slash(string key, Color color, int w = 48, int h = 20)
        {
            if (Cache.TryGetValue(key, out var hit) && hit != null) return hit;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h];
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - cx) / cx;
                    float ny = (y - cy) / cy;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(ny) * 1.6f) * Mathf.Clamp01(1.1f - Mathf.Abs(nx));
                    pixels[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.15f, 0.5f), 32f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
