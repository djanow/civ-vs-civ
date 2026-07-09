using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Generates procedural 256x256 textures for each terrain type.
    /// Uses Perlin noise, sine waves, and random scatter patterns.
    /// Each texture is unique per session (seeded from system time).
    /// </summary>
    public static class TerrainTextureGenerator
    {
        private const int SIZE = 256;
        private const float INV = 1f / SIZE;

        // ────────────────────────────────────────────────────────────────
        // Sea — Blue with subtle horizontal wave lines
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateSea()
        {
            var tex = CreateBaseTexture("Tex_Sea");
            var pixels = new Color[SIZE * SIZE];
            float time = Random.value * 100f;

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Horizontal wave lines using sine + noise
                    float wave = Mathf.Sin(ny * 12f * Mathf.PI + time) * 0.08f;
                    wave += Mathf.PerlinNoise(nx * 3f + time, ny * 2f) * 0.12f;

                    float r = 0.12f + wave * 0.4f;
                    float g = 0.40f + wave * 0.5f;
                    float b = 0.82f + wave * 0.18f;

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Ocean — Deep blue, darker, larger wave patterns
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateOcean()
        {
            var tex = CreateBaseTexture("Tex_Ocean");
            var pixels = new Color[SIZE * SIZE];
            float time = Random.value * 100f;

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Slower, broader waves
                    float wave = Mathf.PerlinNoise(nx * 1.5f + time, ny * 1.5f) * 0.15f;
                    wave += Mathf.Sin(ny * 6f * Mathf.PI + time * 0.5f) * 0.04f;

                    float r = 0.06f + wave * 0.2f;
                    float g = 0.18f + wave * 0.3f;
                    float b = 0.48f + wave * 0.35f;

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Mountain — Gray with rocky noise, occasional snow peaks
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateMountain()
        {
            var tex = CreateBaseTexture("Tex_Mountain");
            var pixels = new Color[SIZE * SIZE];

            // Pre-generate random snow patches
            Vector2[] snowPatches = new Vector2[12];
            for (int i = 0; i < snowPatches.Length; i++)
                snowPatches[i] = new Vector2(Random.value, Random.value);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Multi-octave noise for rocky detail
                    float n1 = Mathf.PerlinNoise(nx * 6f, ny * 6f);
                    float n2 = Mathf.PerlinNoise(nx * 12f, ny * 12f);
                    float n3 = Mathf.PerlinNoise(nx * 24f, ny * 24f);

                    // Quantize for rocky strata effect
                    float rocky = Mathf.Round(n1 * 5f) / 5f;

                    float gray = 0.45f + rocky * 0.30f + n2 * 0.10f + n3 * 0.05f;

                    // Snow on high-altitude areas (bright noise peaks)
                    float altitude = Mathf.PerlinNoise(nx * 2f + 50f, ny * 2f + 50f);
                    float snowAmount = Mathf.Max(0f, (altitude - 0.6f) * 3f);
                    gray += snowAmount * 0.3f;

                    // Individual snow patch peaks
                    float patchInfluence = 0f;
                    foreach (var patch in snowPatches)
                    {
                        float dx = nx - patch.x;
                        float dy = ny - patch.y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < 0.08f)
                            patchInfluence += (1f - dist / 0.08f) * 0.5f;
                    }
                    gray += patchInfluence;

                    gray = Mathf.Clamp01(gray);
                    float c = gray;
                    pixels[y * SIZE + x] = new Color(c, c * 0.92f, c * 0.85f);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Hill — Light green with small bumps
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateHill()
        {
            var tex = CreateBaseTexture("Tex_Hill");
            var pixels = new Color[SIZE * SIZE];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Gentle rolling hills noise
                    float n1 = Mathf.PerlinNoise(nx * 4f, ny * 4f);
                    float n2 = Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.15f;

                    float bump = n1 * 0.25f + n2;

                    float r = 0.30f + bump * 0.50f;
                    float g = 0.65f + bump * 0.35f;
                    float b = 0.22f + bump * 0.30f;

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Forest — Dark green with tree-like dark dots
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateForest()
        {
            var tex = CreateBaseTexture("Tex_Forest");
            var pixels = new Color[SIZE * SIZE];

            // Scatter tree positions
            int treeCount = Random.Range(100, 140);
            Vector2[] trees = new Vector2[treeCount];
            for (int i = 0; i < treeCount; i++)
                trees[i] = new Vector2(Random.value, Random.value);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Base forest floor
                    float n1 = Mathf.PerlinNoise(nx * 5f, ny * 5f) * 0.12f;
                    float r = 0.08f + n1;
                    float g = 0.38f + n1;
                    float b = 0.12f + n1;

                    // Check proximity to trees
                    float closestTree = 0.05f;
                    foreach (var t in trees)
                    {
                        float dx = nx - t.x;
                        float dy = ny - t.y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < closestTree)
                            closestTree = dist;
                    }

                    // Dark circles for tree trunks
                    float treeDarkness = Mathf.Max(0f, (0.05f - closestTree) * 8f);
                    r -= treeDarkness * 0.4f;
                    g -= treeDarkness * 0.3f;
                    b -= treeDarkness * 0.3f;

                    // Light dappling (sunlight through canopy)
                    float dapple = Mathf.PerlinNoise(nx * 10f + 100f, ny * 10f + 100f);
                    if (dapple > 0.7f && closestTree > 0.03f)
                    {
                        float light = (dapple - 0.7f) * 1.5f;
                        r += light * 0.2f;
                        g += light * 0.3f;
                        b += light * 0.1f;
                    }

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Plain — Light green-yellow with subtle variation
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GeneratePlain()
        {
            var tex = CreateBaseTexture("Tex_Plain");
            var pixels = new Color[SIZE * SIZE];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Gentle noise for subtle grassland variation
                    float n1 = Mathf.PerlinNoise(nx * 4f, ny * 4f) * 0.08f;
                    float n2 = Mathf.PerlinNoise(nx * 15f, ny * 15f) * 0.04f;

                    float r = 0.52f + n1 + n2;
                    float g = 0.74f + n1 + n2;
                    float b = 0.32f + n1 * 0.5f + n2;

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Desert — Sandy with small dots
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateDesert()
        {
            var tex = CreateBaseTexture("Tex_Desert");
            var pixels = new Color[SIZE * SIZE];

            // Pre-generate sand grain positions
            int grainCount = Random.Range(300, 500);
            Vector2[] grains = new Vector2[grainCount];
            for (int i = 0; i < grainCount; i++)
                grains[i] = new Vector2(Random.value, Random.value);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Dune-like wave patterns
                    float dune = Mathf.PerlinNoise(nx * 2.5f, ny * 1.5f) * 0.12f;
                    float ripple = Mathf.Sin(ny * 20f * Mathf.PI + nx * 5f) * 0.03f;

                    float r = 0.82f + dune + ripple;
                    float g = 0.72f + dune + ripple;
                    float b = 0.42f + dune * 0.5f;

                    // Sand grains (small dark dots)
                    float grainDarkness = 0f;
                    foreach (var g2 in grains)
                    {
                        float dx = nx - g2.x;
                        float dy = ny - g2.y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < 0.005f)
                        {
                            grainDarkness = 0.3f;
                            break;
                        }
                    }
                    r -= grainDarkness;
                    g -= grainDarkness * 0.8f;
                    b -= grainDarkness * 0.5f;

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Marsh — Brown-green with random dark spots (puddles)
        // ────────────────────────────────────────────────────────────────
        public static Texture2D GenerateMarsh()
        {
            var tex = CreateBaseTexture("Tex_Marsh");
            var pixels = new Color[SIZE * SIZE];

            // Pre-generate puddle positions
            int puddleCount = Random.Range(40, 60);
            Vector2[] puddles = new Vector2[puddleCount];
            for (int i = 0; i < puddleCount; i++)
                puddles[i] = new Vector2(Random.value, Random.value);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * INV, ny = y * INV;

                    // Swampy noise
                    float n1 = Mathf.PerlinNoise(nx * 3f, ny * 3f) * 0.15f;
                    float n2 = Mathf.PerlinNoise(nx * 7f + 30f, ny * 7f + 30f) * 0.08f;

                    float r = 0.30f + n1 + n2;
                    float g = 0.42f + n1 + n2;
                    float b = 0.18f + n1 * 0.5f + n2;

                    // Dark puddles
                    float closestPuddle = 0.06f;
                    foreach (var p in puddles)
                    {
                        float dx = nx - p.x;
                        float dy = ny - p.y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < closestPuddle)
                            closestPuddle = dist;
                    }

                    float puddleDark = Mathf.Max(0f, (0.06f - closestPuddle) * 6f);
                    r -= puddleDark * 0.3f;
                    g -= puddleDark * 0.2f;
                    b += puddleDark * 0.1f; // slightly bluish puddles

                    // Occasional brighter patches (moss/lichen)
                    float lightPatch = Mathf.PerlinNoise(nx * 8f + 60f, ny * 8f + 60f);
                    if (lightPatch > 0.65f)
                    {
                        float brightness = (lightPatch - 0.65f) * 1.5f;
                        r += brightness * 0.15f;
                        g += brightness * 0.25f;
                    }

                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    pixels[y * SIZE + x] = new Color(r, g, b);
                }
            }

            ApplyPixels(tex, pixels);
            return tex;
        }

        // ────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────

        private static Texture2D CreateBaseTexture(string name)
        {
            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        private static void ApplyPixels(Texture2D tex, Color[] pixels)
        {
            tex.SetPixels(pixels);
            tex.Apply();
        }
    }
}
