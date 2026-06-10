using System.Collections.Generic;
using System.Diagnostics.Contracts;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
namespace GAME.TABLE;

public static class GameTable
{
    private static List<GameElement> TABLE_ELEMENTS = new List<GameElement>();
    public static int TABLE_SIZE_X { get; private set; }
    public static int TABLE_SIZE_Y { get; private set; }

    private static readonly Rectangle TS_TILE = new Rectangle(24, 24, 24, 24);

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
                var element = new GameTableElement(
                    (short)posX, (short)posY,
                    (short)tileSize, (short)tileSize, true);
                
                element.CONFIG = new GameInterfaceConfig(true, false);
                element.PLAYER_OWN = (y >= 3 && y <= 6);
                GameRendererConfig eConfig = new GameRendererConfig();
                eConfig.RECTANGLE = TS_TILE;
                eConfig.TEXTURE_PATH = "Environment/tileset";
                element.SetRendererConfig(eConfig);
                TABLE_ELEMENTS.Add(element);
            }
        }
    }
}