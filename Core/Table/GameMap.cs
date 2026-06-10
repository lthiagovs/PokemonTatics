using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

public static class GameMap
{
    private static List<GameElement> MAP_ELEMENTS = new List<GameElement>();

    private static readonly Rectangle TS_TOP_LEFT     = new Rectangle( 0,  0, 24, 24);
    private static readonly Rectangle TS_TOP_BORDER   = new Rectangle(24,  0, 24, 24);
    private static readonly Rectangle TS_TOP_RIGHT    = new Rectangle(48,  0, 24, 24);
    private static readonly Rectangle TS_LEFT         = new Rectangle( 0, 24, 24, 24);
    private static readonly Rectangle TS_TILE         = new Rectangle(24, 24, 24, 24);
    private static readonly Rectangle TS_RIGHT        = new Rectangle(48, 24, 24, 24);
    private static readonly Rectangle TS_BOTTOM_LEFT  = new Rectangle( 0, 48, 24, 24);
    private static readonly Rectangle TS_BOTTOM       = new Rectangle(24, 48, 24, 24);
    private static readonly Rectangle TS_BOTTOM_RIGHT = new Rectangle(48, 48, 24, 24);

    public static List<GameElement> GetMap() { return GameMap.MAP_ELEMENTS; }

    public static void Initialize()
    {
        int tileNumber  = 20;
        int mapTileSize = GameRenderer.GetScreenWidth() / tileNumber;
        int yTiles      = GameRenderer.GetScreenHeight() / mapTileSize;

        // Top row
        for (int x = 0; x < tileNumber; x++)
        {
            var element = new GameInterfaceElement(
                (short)(x * mapTileSize), 0,
                (short)mapTileSize, (short)mapTileSize, true);
            if      (x == 0)            element.SetRendererConfig(Color.White, TS_TOP_LEFT,  null, "Environment/tileset");
            else if (x == tileNumber-1) element.SetRendererConfig(Color.White, TS_TOP_RIGHT, null, "Environment/tileset");
            else                        element.SetRendererConfig(Color.White, TS_TOP_BORDER,null, "Environment/tileset");
            MAP_ELEMENTS.Add(element);
        }
        // Bottom row
        for (int x = 0; x < tileNumber; x++)
        {
            var element = new GameInterfaceElement(
                (short)(x * mapTileSize), (short)((yTiles - 1) * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            if      (x == 0)            element.SetRendererConfig(Color.White, TS_BOTTOM_LEFT,  null, "Environment/tileset");
            else if (x == tileNumber-1) element.SetRendererConfig(Color.White, TS_BOTTOM_RIGHT, null, "Environment/tileset");
            else                        element.SetRendererConfig(Color.White, TS_BOTTOM,        null, "Environment/tileset");
            MAP_ELEMENTS.Add(element);
        }
        // Left column
        for (int y = 1; y < yTiles - 1; y++)
        {
            var element = new GameInterfaceElement(
                0, (short)(y * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            element.SetRendererConfig(Color.White, TS_LEFT, null, "Environment/tileset");
            MAP_ELEMENTS.Add(element);
        }
        // Right column
        for (int y = 1; y < yTiles - 1; y++)
        {
            var element = new GameInterfaceElement(
                (short)((tileNumber - 1) * mapTileSize), (short)(y * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            element.SetRendererConfig(Color.White, TS_RIGHT, null, "Environment/tileset");
            MAP_ELEMENTS.Add(element);
        }
    }
}