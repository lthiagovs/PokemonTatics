using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

public static class GameMap
{
    private static List<GameElement> MAP_ELEMENTS = new List<GameElement>();

    public static List<GameElement> GetMap()
    {
        return GameMap.MAP_ELEMENTS;
    }

    public static void Initialize()
    {
        int tileNumber = 20;
        int mapTileSize = GameRenderer.GetScreenWidth() / tileNumber;
        int yTiles = GameRenderer.GetScreenHeight() / mapTileSize;

        // Top row
        for (int x = 0; x < tileNumber; x++)
        {
            var element = new GameInterfaceElement(
                (short)(x * mapTileSize), 0,
                (short)mapTileSize, (short)mapTileSize, true);
            if(x == 0)
                element.SetRendererConfig(Color.White, null, "Environment/top_left_corner");

            else if(x == tileNumber - 1)
                element.SetRendererConfig(Color.White, null, "Environment/top_right_corner");

            else
                element.SetRendererConfig(Color.White, null, "Environment/top_border");

            MAP_ELEMENTS.Add(element);
        }

        // Bottom row
        for (int x = 0; x < tileNumber; x++)
        {
            var element = new GameInterfaceElement(
                (short)(x * mapTileSize), (short)((yTiles - 1) * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            if(x == 0)
                element.SetRendererConfig(Color.White, null, "Environment/bottom_left_corner");

            else if(x == tileNumber - 1)
                element.SetRendererConfig(Color.White, null, "Environment/bottom_right_corner");

            else
                element.SetRendererConfig(Color.White, null, "Environment/bottom_border");
            MAP_ELEMENTS.Add(element);
        }

        // Left column
        for (int y = 1; y < yTiles - 1; y++)
        {
            var element = new GameInterfaceElement(
                0, (short)(y * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            element.SetRendererConfig(Color.White, null, "Environment/left_border");
            MAP_ELEMENTS.Add(element);
        }

        // Right column
        for (int y = 1; y < yTiles - 1; y++)
        {
            var element = new GameInterfaceElement(
                (short)((tileNumber - 1) * mapTileSize), (short)(y * mapTileSize),
                (short)mapTileSize, (short)mapTileSize, true);
            element.SetRendererConfig(Color.White, null, "Environment/right_border");
            MAP_ELEMENTS.Add(element);
        }
    }
}