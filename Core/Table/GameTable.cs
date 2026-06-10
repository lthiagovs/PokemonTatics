using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

namespace GAME.TABLE;

public static class GameTable
{
    private static List<GameElement> TABLE_ELEMENTS = new List<GameElement>();

    public static int TABLE_SIZE_X { get; private set; }
    public static int TABLE_SIZE_Y { get; private set; }

    public static List<GameElement> GetTable() { return GameTable.TABLE_ELEMENTS; }

    public static void Initialize()
    {
        int totalTiles = 20;
        int tileSize   = GameRenderer.GetScreenWidth() / totalTiles;
        int yTiles     = GameRenderer.GetScreenHeight() / tileSize;

        TABLE_SIZE_X = totalTiles - 2;
        TABLE_SIZE_Y = yTiles - 2;

        for (int y = 0; y < TABLE_SIZE_Y; y++)
        {
            for (int x = 0; x < TABLE_SIZE_X; x++)
            {
                int posX = tileSize + (tileSize * x);
                int posY = tileSize + (tileSize * y);

                var element = new GameInterfaceElement(
                    (short)posX, (short)posY,
                    (short)tileSize, (short)tileSize, true);

                element.MOUSE_HOVER = true;

                element.SetRendererConfig(Color.White, null, "Environment/tile");
                TABLE_ELEMENTS.Add(element);
            }
        }
    }
}