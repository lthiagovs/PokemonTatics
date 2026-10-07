using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace PokemonTFT.Table;

public sealed class DungeonTheme
{
    public required string NAME { get; init; }
    public required int ATLAS_X { get; init; }
    public required int ATLAS_Y { get; init; }
    public required int TILE { get; init; }
    public required int DECOR_COUNT { get; init; }
    public required int CORNER_COUNT { get; init; }
    public required Color WALL_COLOR { get; init; }
    public required Color GROUND_COLOR { get; init; }
    public required int WALL_ALT_MASK { get; init; }

    private Rectangle At(int SLOT, int ROW)
        => new(ATLAS_X + SLOT * TILE, ATLAS_Y + ROW * TILE, TILE, TILE);

    public Rectangle Ground(int SLICE) => At(Math.Clamp(SLICE, 0, 8), 0);

    private static readonly int[] CORNER_FALLBACK = [8, 6, 2, 0];

    public Rectangle Wall(int SLICE)
    {
        if (SLICE < DungeonThemes.SLICE_CORNER) return At(Math.Clamp(SLICE, 0, 8), 1);

        int corner = Math.Clamp(SLICE - DungeonThemes.SLICE_CORNER, 0, 3);
        return corner < CORNER_COUNT ? At(corner, 4) : At(CORNER_FALLBACK[corner], 1);
    }

    public bool HasWallAlt(int SLICE) => (WALL_ALT_MASK & (1 << Math.Clamp(SLICE, 0, 8))) != 0;

    public Rectangle WallAlt(int SLICE) => At(Math.Clamp(SLICE, 0, 8), 2);

    public bool HasDecor => DECOR_COUNT > 0;

    public Rectangle Decor(int INDEX) => At(Math.Abs(INDEX) % Math.Max(1, DECOR_COUNT), 3);
}

public static class DungeonThemes
{
    public const string INDEX_FILE = "Data/dungeons.xml";

    public const int SLICE_CENTER = 4;

    public const int SLICE_CORNER = 9;

    private const int DECOR_CHANCE = 11;

    private static readonly List<DungeonTheme> THEMES = [];

    public static string TEXTURE { get; private set; } = "Environment/dungeons";

    public static DungeonTheme? Current { get; private set; }

    public static IReadOnlyList<DungeonTheme> All => THEMES;

    public static void Load(string? PATH = null)
    {
        THEMES.Clear();

        string path = PATH ?? Path.Combine(AppContext.BaseDirectory, INDEX_FILE);
        if (!File.Exists(path)) throw new FileNotFoundException($"dungeon index not found: {path}", path);

        XElement root = XDocument.Load(path).Root
            ?? throw new InvalidDataException($"{INDEX_FILE} is empty");

        TEXTURE = root.Attribute("texture")?.Value ?? TEXTURE;
        int tile = Number(root, "tile", 24);

        foreach (XElement node in root.Elements("Theme"))
        {
            THEMES.Add(new DungeonTheme
            {
                NAME = node.Attribute("name")?.Value ?? "unnamed",
                ATLAS_X = Number(node, "x", 0),
                ATLAS_Y = Number(node, "y", 0),
                TILE = tile,
                DECOR_COUNT = Number(node, "decor", 0),
                CORNER_COUNT = Number(node, "corners", 0),
                WALL_COLOR = Hex(node, "wall", new Color(92, 98, 112)),
                GROUND_COLOR = Hex(node, "ground", new Color(120, 112, 96)),
                WALL_ALT_MASK = Number(node, "wallAlt", 0),
            });
        }

        if (THEMES.Count == 0) throw new InvalidDataException($"{INDEX_FILE} has no themes");
        Roll();
    }

    public static void Roll()
    {
        if (THEMES.Count == 0) return;
        Current = THEMES[Random.Shared.Next(THEMES.Count)];
    }

    public static int SliceIndex(int COLUMN, int ROW, int WIDTH, int HEIGHT)
    {
        int x = COLUMN <= 0 ? 0 : COLUMN >= WIDTH - 1 ? 2 : 1;
        int y = ROW <= 0 ? 0 : ROW >= HEIGHT - 1 ? 2 : 1;
        return y * 3 + x;
    }

    public static int WallSlice(bool FLOOR_ABOVE, bool FLOOR_BELOW, bool FLOOR_LEFT, bool FLOOR_RIGHT,
                               bool FLOOR_UP_LEFT, bool FLOOR_UP_RIGHT, bool FLOOR_DOWN_LEFT, bool FLOOR_DOWN_RIGHT)
    {
        if (FLOOR_ABOVE || FLOOR_BELOW || FLOOR_LEFT || FLOOR_RIGHT)
        {
            int x = FLOOR_LEFT ? 0 : FLOOR_RIGHT ? 2 : 1;
            int y = FLOOR_ABOVE ? 0 : FLOOR_BELOW ? 2 : 1;
            return y * 3 + x;
        }

        if (FLOOR_DOWN_RIGHT) return SLICE_CORNER;
        if (FLOOR_DOWN_LEFT) return SLICE_CORNER + 1;
        if (FLOOR_UP_RIGHT) return SLICE_CORNER + 2;
        if (FLOOR_UP_LEFT) return SLICE_CORNER + 3;

        return SLICE_CENTER;
    }

    public static int Scatter(int COLUMN, int ROW, int CHANCE)
    {
        int hash = ((COLUMN * 73856093) ^ (ROW * 19349663)) & 0x7fffffff;
        return hash % Math.Max(1, CHANCE) == 0 ? hash / Math.Max(1, CHANCE) : -1;
    }

    public static int DecorAt(int COLUMN, int ROW) => Scatter(COLUMN, ROW, DECOR_CHANCE);

    private static Color Hex(XElement NODE, string ATTRIBUTE, Color FALLBACK)
    {
        string? raw = NODE.Attribute(ATTRIBUTE)?.Value;
        if (raw is not { Length: 6 }) return FALLBACK;

        return int.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)
            ? new Color((value >> 16) & 0xff, (value >> 8) & 0xff, value & 0xff)
            : FALLBACK;
    }

    private static int Number(XElement NODE, string ATTRIBUTE, int FALLBACK)
    {
        string? raw = NODE.Attribute(ATTRIBUTE)?.Value;
        return raw != null && int.TryParse(raw, out int value) ? value : FALLBACK;
    }
}
