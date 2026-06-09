using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

namespace GAME.TABLE;

public static class GameTable
{

    private static List<GameElement> TABLE_ELEMENTS = new List<GameElement>();
    public static int TABLE_SIZE_X = 18;
    public static int TABLE_SIZE_Y = 8;
    
    public static List<GameElement> GetTable() { return GameTable.TABLE_ELEMENTS; }
    public static void Initialize()
    {

        // BUILD TABLE GROUND
        int screen_width = GameRenderer.GetScreenWidth();
        int table_size = screen_width/20;

        for(int y = 0; y < TABLE_SIZE_Y; y++)
        {
            for(int x = 0;x < TABLE_SIZE_X; x++)
            {
                int posx = table_size + (table_size*x);
                int posy = table_size + (table_size*y);
                
                GameInterfaceElement element = new GameInterfaceElement((short) posx, (short) posy, (short) table_size, (short) table_size, true);
                element.SetRendererConfig(Color.Blue);

                GameTable.TABLE_ELEMENTS.Add(element);
            }
        }

    }


}