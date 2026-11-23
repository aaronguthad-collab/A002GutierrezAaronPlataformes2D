using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArtGenerator
{
    public const string Folder = "Assets/Art/Generated";

    public static Sprite LogoEmblem => Load("GinebroGamesEmblem");
    public static Sprite Heart => Load("Heart");
    public static Sprite HeartEmpty => Load("HeartEmpty");
    public static Sprite UiPanel => Load("UiPanel");
    public static Sprite Portal => Load("PortalSwirl");
    public static Sprite PortalGlow => Load("PortalGlow");
    public static Sprite Pixel => Load("Pixel");

    public static Sprite[] CoinFrames => new[] { Load("Coin0"), Load("Coin1"), Load("Coin2"), Load("Coin1") };

    public static void GenerateAll()
    {
        Directory.CreateDirectory(Folder);
        Save("GinebroGamesEmblem", Emblem(256), 100, FilterMode.Bilinear);
        Save("Heart", HeartTexture(new Color32(229, 57, 70, 255), new Color32(255, 160, 160, 255)), 16, FilterMode.Point);
        Save("HeartEmpty", HeartTexture(new Color32(60, 60, 70, 255), new Color32(90, 90, 100, 255)), 16, FilterMode.Point);
        Save("UiPanel", Panel(32), 100, FilterMode.Point, new Vector4(10, 10, 10, 10));
        Save("PortalSwirl", PortalTexture(48, false), 16, FilterMode.Point);
        Save("PortalGlow", PortalTexture(48, true), 16, FilterMode.Point);
        Save("Pixel", Solid(4, Color.white), 4, FilterMode.Point);
        for (int i = 0; i < 3; i++) Save("Coin" + i, CoinTexture(i), 16, FilterMode.Point);
        AssetDatabase.Refresh();
    }

    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

    private static void Save(string name, Texture2D tex, float ppu, FilterMode filter, Vector4 border = default)
    {
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = border;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Texture2D New(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color32[w * h];
        tex.SetPixels32(clear);
        return tex;
    }

    private static Texture2D Solid(int size, Color c)
    {
        var tex = New(size, size);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) tex.SetPixel(x, y, c);
        tex.Apply();
        return tex;
    }

    private static void Blend(Texture2D tex, int x, int y, Color c, float a)
    {
        if (x < 0 || y < 0 || x >= tex.width || y >= tex.height || a <= 0f) return;
        Color dst = tex.GetPixel(x, y);
        a = Mathf.Clamp01(a) * c.a;
        float outA = a + dst.a * (1f - a);
        Color o = outA > 0f ? (c * a + dst * dst.a * (1f - a)) / outA : Color.clear;
        o.a = outA;
        tex.SetPixel(x, y, o);
    }

    private static void Disc(Texture2D tex, Vector2 c, float r, Color col)
    {
        for (int y = (int)(c.y - r - 2); y <= c.y + r + 2; y++)
            for (int x = (int)(c.x - r - 2); x <= c.x + r + 2; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                Blend(tex, x, y, col, r - d + 0.5f);
            }
    }

    private static void Ring(Texture2D tex, Vector2 c, float r, float width, Color col)
    {
        for (int y = (int)(c.y - r - width - 2); y <= c.y + r + width + 2; y++)
            for (int x = (int)(c.x - r - width - 2); x <= c.x + r + width + 2; x++)
            {
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) - r);
                Blend(tex, x, y, col, width * 0.5f - d + 0.5f);
            }
    }

    private static void Line(Texture2D tex, Vector2 a, Vector2 b, float width, Color col)
    {
        int minX = (int)(Mathf.Min(a.x, b.x) - width - 2), maxX = (int)(Mathf.Max(a.x, b.x) + width + 2);
        int minY = (int)(Mathf.Min(a.y, b.y) - width - 2), maxY = (int)(Mathf.Max(a.y, b.y) + width + 2);
        Vector2 ab = b - a;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = Vector2.Distance(p, a + ab * t);
                Blend(tex, x, y, col, width * 0.5f - d + 0.5f);
            }
    }

    private static Texture2D Emblem(int size)
    {
        var tex = New(size, size);
        Vector2 c = new(size / 2f, size / 2f);
        Color dark = new(0.10f, 0.20f, 0.15f);
        Color mid = new(0.16f, 0.36f, 0.24f);
        Color gold = new(0.95f, 0.78f, 0.35f);
        Color needle = new(0.45f, 0.75f, 0.42f);
        Color needleDark = new(0.25f, 0.55f, 0.32f);
        Color wood = new(0.45f, 0.30f, 0.18f);
        Color berry = new(0.30f, 0.36f, 0.70f);
        Color berryShade = new(0.20f, 0.22f, 0.48f);

        Disc(tex, c, size * 0.48f, gold);
        Disc(tex, c, size * 0.44f, dark);
        Disc(tex, c, size * 0.40f, mid);
        Ring(tex, c, size * 0.36f, 2f, new Color(1f, 1f, 1f, 0.15f));

        Vector2 a = new(size * 0.28f, size * 0.24f), b = new(size * 0.70f, size * 0.76f);
        Line(tex, a, b, 7f, wood);
        var rng = new System.Random(7);
        for (int i = 0; i < 9; i++)
        {
            float t = 0.12f + i * 0.095f;
            Vector2 p = Vector2.Lerp(a, b, t);
            Vector2 dir = (b - a).normalized;
            Vector2 n = new(-dir.y, dir.x);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 tip = p + (n * side * (22f + (float)rng.NextDouble() * 10f)) + dir * 14f;
                Line(tex, p, tip, 4f, side > 0 ? needle : needleDark);
            }
        }

        Vector2[] berries =
        {
            new(0.46f, 0.36f), new(0.56f, 0.40f), new(0.50f, 0.46f), new(0.38f, 0.48f),
            new(0.62f, 0.52f), new(0.44f, 0.56f),
        };
        foreach (var bp in berries)
        {
            Vector2 p = new(bp.x * size, bp.y * size);
            Disc(tex, p, 13f, berryShade);
            Disc(tex, p + new Vector2(-1.5f, 1.5f), 11f, berry);
            Disc(tex, p + new Vector2(-4f, 4f), 3.5f, new Color(1f, 1f, 1f, 0.75f));
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D HeartTexture(Color32 fill, Color32 shine)
    {
        string[] rows =
        {
            "................",
            "..XXXX....XXXX..",
            ".XOOOOX..XOOOOX.",
            "XOSSOOOXXOOOOOOX",
            "XOSOOOOOOOOOOOOX",
            "XOOOOOOOOOOOOOOX",
            "XOOOOOOOOOOOOOOX",
            ".XOOOOOOOOOOOOX.",
            "..XOOOOOOOOOOX..",
            "...XOOOOOOOOX...",
            "....XOOOOOOX....",
            ".....XOOOOX.....",
            "......XOOX......",
            ".......XX.......",
            "................",
            "................",
        };
        var tex = New(16, 16);
        Color32 outline = new(40, 10, 20, 255);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                char ch = rows[15 - y][x];
                if (ch == 'X') tex.SetPixel(x, y, (Color)outline);
                else if (ch == 'O') tex.SetPixel(x, y, (Color)fill);
                else if (ch == 'S') tex.SetPixel(x, y, (Color)shine);
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D CoinTexture(int phase)
    {
        var tex = New(16, 16);
        float rx = phase == 0 ? 6.5f : phase == 1 ? 4.2f : 1.6f;
        const float ry = 6.5f;
        Color edge = new(0.62f, 0.38f, 0.05f);
        Color gold = new(1f, 0.80f, 0.18f);
        Color light = new(1f, 0.95f, 0.6f);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = (x + 0.5f - 8f) / rx, dy = (y + 0.5f - 8f) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 1f)
                {
                    float inner = ((x + 0.5f - 8f) / Mathf.Max(0.5f, rx - 1.2f)) * ((x + 0.5f - 8f) / Mathf.Max(0.5f, rx - 1.2f)) +
                                  ((y + 0.5f - 8f) / (ry - 1.2f)) * ((y + 0.5f - 8f) / (ry - 1.2f));
                    Color col = inner <= 1f ? gold : edge;
                    if (inner <= 1f && x < 8 && y > 8 && phase < 2) col = light;
                    tex.SetPixel(x, y, col);
                }
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D Panel(int size)
    {
        var tex = New(size, size);
        Color border = new(0.55f, 0.55f, 0.6f);
        Color fill = Color.white;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int cx = Mathf.Min(x, size - 1 - x), cy = Mathf.Min(y, size - 1 - y);
                if (cx + cy < 3) continue;
                bool isBorder = cx < 3 || cy < 3 || cx + cy < 5;
                tex.SetPixel(x, y, isBorder ? border : fill);
            }
        tex.Apply();
        return tex;
    }

    private static Texture2D PortalTexture(int size, bool glow)
    {
        var tex = New(size, size);
        Vector2 c = new(size / 2f, size / 2f);
        if (glow)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size * 0.5f);
                    if (d < 1f) tex.SetPixel(x, y, new Color(0.55f, 0.35f, 1f, Mathf.Pow(1f - d, 1.6f) * 0.9f));
                }
        }
        else
        {
            for (int arm = 0; arm < 3; arm++)
            {
                for (float t = 0f; t < 1f; t += 0.004f)
                {
                    float ang = arm * Mathf.PI * 2f / 3f + t * Mathf.PI * 2.2f;
                    float r = t * size * 0.46f;
                    Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    Color col = Color.Lerp(new Color(1f, 1f, 1f), new Color(0.6f, 0.4f, 1f), t);
                    Disc(tex, p, 1.2f + t * 2.2f, col);
                }
            }
        }
        tex.Apply();
        return tex;
    }
}
