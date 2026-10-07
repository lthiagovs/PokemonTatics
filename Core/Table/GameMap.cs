using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.UI;

namespace PokemonTFT.Table;

public static class GameMap
{
    private const int ALT_CHANCE = 4;

    private static readonly Rectangle[] FALLBACK = BuildFallback();

    private static readonly List<GameElement> MAP_ELEMENTS = [];

    public static IReadOnlyList<GameElement> GetMap() => MAP_ELEMENTS;

    private static Rectangle[] BuildFallback()
    {
        var rects = new Rectangle[9];
        for (int i = 0; i < 9; i++) rects[i] = new Rectangle(i % 3 * 24, i / 3 * 24, 24, 24);
        return rects;
    }

    public static void Initialize()
    {
        MAP_ELEMENTS.Clear();

        int tileSize = GameTable.TILE_SIZE;
        if (tileSize <= 0) return;

        int columns = GameRenderer.GetScreenWidth() / tileSize + 1;
        int rows = GameRenderer.GetScreenHeight() / tileSize + 1;
        Rectangle play = GameTable.PlayArea;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int x = column * tileSize;
                int y = row * tileSize;
                if (play.Contains(x + tileSize / 2, y + tileSize / 2)) continue;

                var element = new GameInterfaceElement(x, y, tileSize, tileSize, VISIBLE: true);
                element.SetRendererConfig(WallConfig(x, y, tileSize, play));
                MAP_ELEMENTS.Add(element);
            }
        }
    }

    private static GameRendererConfig WallConfig(int X, int Y, int TILE_SIZE, Rectangle PLAY)
    {
        int slice = DungeonThemes.WallSlice(
            IsFloor(X, Y - TILE_SIZE, TILE_SIZE, PLAY),
            IsFloor(X, Y + TILE_SIZE, TILE_SIZE, PLAY),
            IsFloor(X - TILE_SIZE, Y, TILE_SIZE, PLAY),
            IsFloor(X + TILE_SIZE, Y, TILE_SIZE, PLAY),
            IsFloor(X - TILE_SIZE, Y - TILE_SIZE, TILE_SIZE, PLAY),
            IsFloor(X + TILE_SIZE, Y - TILE_SIZE, TILE_SIZE, PLAY),
            IsFloor(X - TILE_SIZE, Y + TILE_SIZE, TILE_SIZE, PLAY),
            IsFloor(X + TILE_SIZE, Y + TILE_SIZE, TILE_SIZE, PLAY));

        DungeonTheme? theme = DungeonThemes.Current;
        if (theme == null) return GameRendererConfig.Sprite("Environment/tileset", FALLBACK[slice]);

        if (slice == DungeonThemes.SLICE_CENTER && theme.HasWallAlt(slice))
        {
            int scatter = DungeonThemes.Scatter(X / TILE_SIZE, Y / TILE_SIZE, ALT_CHANCE);
            if (scatter >= 0) return GameRendererConfig.Sprite(DungeonThemes.TEXTURE, theme.WallAlt(slice));
        }

        return GameRendererConfig.Sprite(DungeonThemes.TEXTURE, theme.Wall(slice));
    }

    private static bool IsFloor(int X, int Y, int TILE_SIZE, Rectangle PLAY)
        => PLAY.Contains(X + TILE_SIZE / 2, Y + TILE_SIZE / 2);
}
