using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.UI;

namespace PokemonTFT.Table;

public static class GameMap
{
    private const int TILE = 24;

    private static readonly Rectangle TS_TOP_LEFT     = new(0,        0, TILE, TILE);
    private static readonly Rectangle TS_TOP          = new(TILE,     0, TILE, TILE);
    private static readonly Rectangle TS_TOP_RIGHT    = new(TILE * 2, 0, TILE, TILE);
    private static readonly Rectangle TS_LEFT         = new(0,        TILE, TILE, TILE);
    private static readonly Rectangle TS_RIGHT        = new(TILE * 2, TILE, TILE, TILE);
    private static readonly Rectangle TS_BOTTOM_LEFT  = new(0,        TILE * 2, TILE, TILE);
    private static readonly Rectangle TS_BOTTOM       = new(TILE,     TILE * 2, TILE, TILE);
    private static readonly Rectangle TS_BOTTOM_RIGHT = new(TILE * 2, TILE * 2, TILE, TILE);

    private static readonly List<GameElement> MAP_ELEMENTS = [];

    public static IReadOnlyList<GameElement> GetMap() => MAP_ELEMENTS;

    public static void Initialize()
    {
        MAP_ELEMENTS.Clear();

        int columns  = GameTable.TILE_COLUMNS;
        int tileSize = GameRenderer.GetScreenWidth() / columns;
        int rows     = GameRenderer.GetScreenHeight() / tileSize;

        for (int x = 0; x < columns; x++)
        {
            Add(x * tileSize, 0, tileSize,
                x == 0 ? TS_TOP_LEFT : x == columns - 1 ? TS_TOP_RIGHT : TS_TOP);

            Add(x * tileSize, (rows - 1) * tileSize, tileSize,
                x == 0 ? TS_BOTTOM_LEFT : x == columns - 1 ? TS_BOTTOM_RIGHT : TS_BOTTOM);
        }

        for (int y = 1; y < rows - 1; y++)
        {
            Add(0, y * tileSize, tileSize, TS_LEFT);
            Add((columns - 1) * tileSize, y * tileSize, tileSize, TS_RIGHT);
        }
    }

    private static void Add(int X, int Y, int SIZE, Rectangle SOURCE)
    {
        var element = new GameInterfaceElement(X, Y, SIZE, SIZE, VISIBLE: true);
        element.SetRendererConfig(GameRendererConfig.Sprite("Environment/tileset", SOURCE));
        MAP_ELEMENTS.Add(element);
    }
}
