using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the small pixel-art sprites the asset packs don't have (coins, crystals, potions, water...).
/// </summary>
static class PixelArt
{
    static readonly Color Clear = new(0, 0, 0, 0);

    public static Texture2D New(int width, int height)
    {
        var t = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
        t.SetPixels(pixels);
        return t;
    }

    static Color C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

    /// <summary>Builds a texture from text rows (first row = top). '.' is transparent.</summary>
    public static Texture2D FromPattern(string[] rows, Dictionary<char, Color> palette)
    {
        int h = rows.Length, w = rows[0].Length;
        var t = New(w, h);
        for (int row = 0; row < h; row++)
        for (int x = 0; x < w; x++)
        {
            char ch = rows[row][x];
            if (ch != '.' && palette.TryGetValue(ch, out var color)) t.SetPixel(x, h - 1 - row, color);
        }
        t.Apply();
        return t;
    }

    /// <summary>Adds a 1px outline around every opaque pixel.</summary>
    public static void Outline(Texture2D t, Color color)
    {
        var src = t.GetPixels();
        int w = t.width, h = t.height;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (src[y * w + x].a > 0.1f) continue;
            bool edge = (x > 0 && src[y * w + x - 1].a > 0.1f) || (x < w - 1 && src[y * w + x + 1].a > 0.1f) ||
                        (y > 0 && src[(y - 1) * w + x].a > 0.1f) || (y < h - 1 && src[(y + 1) * w + x].a > 0.1f);
            if (edge) t.SetPixel(x, y, color);
        }
        t.Apply();
    }

    // ---------- Items ----------

    public static Texture2D White()
    {
        var t = New(4, 4);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
        t.Apply();
        return t;
    }

    public static Texture2D Coin()
    {
        var t = New(14, 14);
        for (int y = 0; y < 14; y++)
        for (int x = 0; x < 14; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(7f, 7f));
            if (d > 5.6f) continue;
            Color c = C(242, 194, 48);
            if (d > 3.4f && d < 4.4f) c = C(206, 146, 32);             // inner ring
            if (x + y > 14 && d > 4.4f) c = C(196, 128, 28);           // shaded bottom-right
            if (x < 6 && y > 8 && d < 4.4f) c = C(255, 242, 170);      // highlight
            t.SetPixel(x, y, c);
        }
        Outline(t, C(92, 56, 12));
        return t;
    }

    public static Texture2D Crystal()
    {
        return FromPattern(new[]
        {
            ".....k.....",
            "....kck....",
            "...kcwck...",
            "..kcwcbck..",
            ".kcwccbbck.",
            "kcwcccbbbck",
            "kcwcccbbbck",
            "kccccbbbbck",
            ".kcccbbbck.",
            "..kccbbck..",
            "...kcbck...",
            "....kck....",
            ".....k.....",
        }, new Dictionary<char, Color>
        {
            ['k'] = C(18, 66, 92), ['c'] = C(96, 222, 236), ['w'] = C(225, 255, 255), ['b'] = C(42, 150, 196)
        });
    }

    public static Texture2D Potion(Color liquid, Color liquidDark)
    {
        var t = New(14, 16);
        Color glass = C(200, 225, 235, 200), cork = C(150, 100, 55), shine = C(255, 255, 255);
        // round body
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 14; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(7f, 5.5f));
            if (d <= 5f) t.SetPixel(x, y, y <= 6 ? (x > 8 || y < 2 ? liquidDark : liquid) : glass);
        }
        // neck and cork
        for (int y = 10; y <= 12; y++) for (int x = 5; x <= 8; x++) t.SetPixel(x, y, glass);
        for (int y = 13; y <= 14; y++) for (int x = 5; x <= 8; x++) t.SetPixel(x, y, cork);
        t.SetPixel(4, 6, shine); t.SetPixel(4, 5, shine); t.SetPixel(5, 7, shine);
        t.Apply();
        Outline(t, C(40, 30, 40));
        return t;
    }

    public static Texture2D DigMark()
    {
        var t = New(18, 12);
        // dirt mound
        for (int y = 0; y < 12; y++)
        for (int x = 0; x < 18; x++)
        {
            float nx = (x + 0.5f - 9f) / 8.5f, ny = (y + 0.5f - 4f) / 3.6f;
            if (nx * nx + ny * ny <= 1f) t.SetPixel(x, y, y > 4 ? C(150, 110, 70) : C(118, 84, 52));
        }
        // red X
        Color red = C(200, 46, 36);
        for (int i = 0; i < 8; i++)
        {
            int x0 = 5 + i, y0 = 1 + i, y1 = 8 - i;
            t.SetPixel(x0, y0, red); t.SetPixel(x0 + 1, y0, red);
            t.SetPixel(x0, y1 + 1, red); t.SetPixel(x0 + 1, y1 + 1, red);
        }
        t.Apply();
        Outline(t, C(60, 36, 20));
        return t;
    }

    public static Texture2D Exclamation()
    {
        return FromPattern(new[]
        {
            ".kkk.",
            "kyyyk",
            "kyyyk",
            "kyyyk",
            ".kyk.",
            ".kyk.",
            "..k..",
            ".kkk.",
            ".kyk.",
            ".kkk.",
        }, new Dictionary<char, Color> { ['k'] = C(60, 30, 10), ['y'] = C(255, 214, 64) });
    }

    public static Texture2D MapDot()
    {
        var t = New(11, 11);
        for (int y = 0; y < 11; y++)
        for (int x = 0; x < 11; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(5.5f, 5.5f));
            if (d <= 3.6f) t.SetPixel(x, y, x < 5 && y > 5 ? C(255, 250, 200) : C(255, 214, 64));
        }
        t.Apply();
        Outline(t, C(40, 20, 10));
        return t;
    }

    /// <summary>One pixel per map tile, coloured by terrain. Used as the minimap picture.</summary>
    public static Texture2D Minimap(int width, int height, System.Func<int, int, char> terrain)
    {
        var t = New(width, height);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color c = terrain(x, y) switch
            {
                '~' => C(40, 82, 112),
                '*' => C(66, 116, 146),   // shallow water next to land
                ':' => C(198, 180, 130),
                ',' => C(112, 130, 58),
                '=' => C(156, 154, 144),
                '_' => C(136, 132, 120),
                '#' => C(52, 74, 34),
                'T' => C(70, 94, 42),
                _ => C(98, 120, 52),
            };
            t.SetPixel(x, y, c);
        }
        t.Apply();
        return t;
    }

    public static Texture2D Sparkle()
    {
        return FromPattern(new[]
        {
            "...w...",
            "...w...",
            "..wyw..",
            "wwyyyww",
            "..wyw..",
            "...w...",
            "...w...",
        }, new Dictionary<char, Color> { ['w'] = C(255, 250, 210), ['y'] = C(255, 255, 255) });
    }

    public static Texture2D Scroll()
    {
        var t = New(16, 11);
        Color paper = C(236, 214, 162), roll = C(196, 160, 106), ink = C(130, 96, 60);
        for (int y = 2; y <= 8; y++) for (int x = 2; x <= 13; x++) t.SetPixel(x, y, paper);
        for (int y = 1; y <= 9; y++) { t.SetPixel(1, y, roll); t.SetPixel(2, y, roll); t.SetPixel(13, y, roll); t.SetPixel(14, y, roll); }
        for (int x = 4; x <= 11; x += 1) { if (x % 3 != 0) { t.SetPixel(x, 6, ink); t.SetPixel(x, 4, ink); } }
        t.Apply();
        Outline(t, C(70, 48, 26));
        return t;
    }

    public static Texture2D Shadow()
    {
        var t = New(16, 8);
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 16; x++)
        {
            float nx = (x + 0.5f - 8f) / 8f, ny = (y + 0.5f - 4f) / 4f;
            float d = nx * nx + ny * ny;
            if (d <= 1f) t.SetPixel(x, y, new Color(0f, 0f, 0f, d < 0.55f ? 0.32f : 0.2f));
        }
        t.Apply();
        return t;
    }

    public static Texture2D Glow()
    {
        var t = New(64, 64);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
            if (d >= 1f) continue;
            float a = (1f - d) * (1f - d);
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        t.Apply();
        return t;
    }

    public static Texture2D PortalSwirl()
    {
        var t = New(64, 64);
        Color purple = C(120, 60, 210), cyan = C(110, 230, 255), core = C(235, 250, 255);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            Vector2 p = new(x + 0.5f - 32f, y + 0.5f - 32f);
            float r = p.magnitude / 32f;
            if (r >= 1f) continue;
            float angle = Mathf.Atan2(p.y, p.x);
            float band = 0.5f + 0.5f * Mathf.Sin(angle * 3f + r * 12f);
            band = Mathf.Round(band * 3f) / 3f; // posterize for a pixel-art look
            Color c = Color.Lerp(purple, cyan, band);
            if (r < 0.25f) c = Color.Lerp(core, c, r / 0.25f);
            c.a = Mathf.Clamp01((1f - r * r) * (0.55f + 0.45f * band) * 1.2f);
            t.SetPixel(x, y, c);
        }
        t.Apply();
        return t;
    }

    // ---------- Terrain ----------

    /// <summary>
    /// 4x4 atlas of 32px water tiles. Tile index = land mask (N=1, E=2, S=4, W=8);
    /// foam is drawn on the sides that touch land.
    /// </summary>
    public static Texture2D WaterAtlas()
    {
        var t = New(128, 128);
        Color water = C(62, 121, 153), dark = C(54, 106, 138), light = C(92, 156, 184), foam = C(214, 234, 228);
        for (int mask = 0; mask < 16; mask++)
        {
            int ox = (mask % 4) * 32, oy = (3 - mask / 4) * 32;
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                Color c = water;
                // little wave marks
                if ((y == 6 || y == 22) && (x + y * 3) % 16 < 4) c = light;
                if ((y == 14 || y == 29) && (x * 2 + y) % 20 < 3) c = dark;

                int foamWidth = 2 + (Mathf.Sin((x + y) * 0.7f) > 0.3f ? 1 : 0);
                if ((mask & 1) != 0 && y >= 32 - foamWidth) c = foam;          // north
                if ((mask & 4) != 0 && y < foamWidth) c = foam;                // south
                if ((mask & 2) != 0 && x >= 32 - foamWidth) c = foam;          // east
                if ((mask & 8) != 0 && x < foamWidth) c = foam;                // west
                t.SetPixel(ox + x, oy + y, c);
            }
        }
        t.Apply();
        return t;
    }

    public static Texture2D Sand()
    {
        var t = New(32, 32);
        var rng = new System.Random(42);
        Color sand = C(206, 186, 136), dark = C(188, 166, 118), light = C(224, 206, 160);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            int r = rng.Next(100);
            t.SetPixel(x, y, r < 7 ? dark : r < 12 ? light : sand);
        }
        t.Apply();
        return t;
    }
}
