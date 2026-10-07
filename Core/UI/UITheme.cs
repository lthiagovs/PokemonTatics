using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Table;

namespace PokemonTFT.UI;

public static class UITheme
{
    private const float MIN_CONTRAST = 0.34f;
    private const float CONTRAST_STEP = 0.06f;

    private static readonly Color FIXED_TEXT = new(255, 255, 255);
    private static readonly Color FIXED_TEXT_DIM = new(198, 206, 220);
    private static readonly Color FIXED_ACCENT = new(255, 198, 88);

    public static Color SURFACE { get; private set; } = new(16, 18, 26);
    public static Color SURFACE_DEEP { get; private set; } = new(9, 10, 15);
    public static Color BORDER { get; private set; } = new(96, 108, 132);
    public static Color EDGE { get; private set; } = new(150, 164, 192);
    public static Color ACCENT { get; private set; } = new(255, 198, 88);
    public static Color TEXT { get; private set; } = new(255, 255, 255);
    public static Color TEXT_DIM { get; private set; } = new(198, 206, 220);
    public static Color HEALTH { get; private set; } = new(216, 72, 76);
    public static Color MANA { get; private set; } = new(74, 150, 236);
    public static Color TRACK { get; private set; } = new(9, 10, 15);
    public static Color GLASS { get; private set; } = new Color(16, 18, 26) * 0.74f;
    public static Color GLASS_DEEP { get; private set; } = new Color(9, 10, 15) * 0.86f;

    public static bool DARK { get; private set; } = true;

    public static void Rebuild()
    {
        Color reference = DungeonThemes.Current?.WALL_COLOR ?? new Color(92, 98, 112);

        ToHsv(reference, out float hue, out _, out _);
        float mapLuma = Luma(reference);

        DARK = true;
        _ = mapLuma;

        float surface = DARK ? 0.10f : 0.95f;
        while (Math.Abs(surface - mapLuma) < MIN_CONTRAST && surface > 0.03f && surface < 0.99f)
        {
            surface += DARK ? -CONTRAST_STEP : CONTRAST_STEP;
        }

        SURFACE = FromHsv(hue, DARK ? 0.34f : 0.07f, surface);
        SURFACE_DEEP = FromHsv(hue, DARK ? 0.40f : 0.11f, surface * (DARK ? 0.52f : 0.86f));
        TRACK = SURFACE_DEEP;
        GLASS = SURFACE * 0.74f;
        GLASS_DEEP = SURFACE_DEEP * 0.88f;

        BORDER = FromHsv(hue, 0.30f, DARK ? 0.44f : 0.52f);
        EDGE = FromHsv(hue, 0.18f, DARK ? 0.76f : 0.34f);
        ACCENT = FIXED_ACCENT;

        TEXT = FIXED_TEXT;
        TEXT_DIM = FIXED_TEXT_DIM;

        HEALTH = FromHsv(0.99f, 0.74f, DARK ? 0.92f : 0.78f);
        MANA = FromHsv(0.58f, 0.72f, DARK ? 0.94f : 0.80f);
    }

    public static float Luma(Color VALUE)
        => (VALUE.R * 0.2126f + VALUE.G * 0.7152f + VALUE.B * 0.0722f) / 255f;

    private static float Wrap(float VALUE) => VALUE - (float)Math.Floor(VALUE);

    private static void ToHsv(Color VALUE, out float HUE, out float SATURATION, out float BRIGHTNESS)
    {
        float red = VALUE.R / 255f;
        float green = VALUE.G / 255f;
        float blue = VALUE.B / 255f;

        float max = Math.Max(red, Math.Max(green, blue));
        float min = Math.Min(red, Math.Min(green, blue));
        float delta = max - min;

        BRIGHTNESS = max;
        SATURATION = max <= 0f ? 0f : delta / max;

        if (delta <= 0f)
        {
            HUE = 0f;
            return;
        }

        float hue = max == red ? (green - blue) / delta
                  : max == green ? 2f + (blue - red) / delta
                  : 4f + (red - green) / delta;

        HUE = Wrap(hue / 6f);
    }

    private static Color FromHsv(float HUE, float SATURATION, float BRIGHTNESS)
    {
        float hue = Wrap(HUE) * 6f;
        int sector = (int)hue;
        float fraction = hue - sector;

        float p = BRIGHTNESS * (1f - SATURATION);
        float q = BRIGHTNESS * (1f - SATURATION * fraction);
        float t = BRIGHTNESS * (1f - SATURATION * (1f - fraction));

        (float red, float green, float blue) = sector switch
        {
            0 => (BRIGHTNESS, t, p),
            1 => (q, BRIGHTNESS, p),
            2 => (p, BRIGHTNESS, t),
            3 => (p, q, BRIGHTNESS),
            4 => (t, p, BRIGHTNESS),
            _ => (BRIGHTNESS, p, q)
        };

        return new Color(Channel(red), Channel(green), Channel(blue));
    }

    private static int Channel(float VALUE) => Math.Clamp((int)(VALUE * 255f + 0.5f), 0, 255);
}
