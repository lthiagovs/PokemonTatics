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
        int mapTileSize = GameRenderer.GetScreenWidth()/tileNumber;
        int yTimes = GameRenderer.GetScreenHeight()/mapTileSize;

        //Left
        for(short y = 1; y<9; y++)
        {
            short posX = 0;
            Color tileColor;

            GameInterfaceElement element = new GameInterfaceElement( posX, (short) (y*mapTileSize), (short) mapTileSize, (short) mapTileSize, true);
            tileColor = Color.Azure;

            element.SetRendererConfig(tileColor);
            MAP_ELEMENTS.Add(element);
        }

        //Right
        for(short y = 1; y<9; y++)
        {
            Color tileColor;

            GameInterfaceElement element = new GameInterfaceElement( (short) (GameRenderer.GetScreenWidth()-mapTileSize), 
            (short) (y*mapTileSize), (short) mapTileSize, (short) mapTileSize, true);
            tileColor = Color.Azure;

            element.SetRendererConfig(tileColor);
            MAP_ELEMENTS.Add(element);
        }

        

        //Top
        for(short x = 0; x < tileNumber; x++)
        {
            short posX = (short)(x * mapTileSize);
            Color tileColor;

            GameInterfaceElement element = new GameInterfaceElement( posX, 0, (short) mapTileSize, (short) mapTileSize, true);
            if(x == 0 || x == tileNumber-1) tileColor = Color.Aquamarine;
            else tileColor = Color.Azure;

            element.SetRendererConfig(tileColor);
            MAP_ELEMENTS.Add(element);
        }

        //Bottom
        for(short x = 0; x < tileNumber; x++)
        {
            short posX = (short)(x * mapTileSize);
            Color tileColor;

            GameInterfaceElement element = new GameInterfaceElement( posX, (short) (GameRenderer.GetScreenHeight()-mapTileSize*2), (short) mapTileSize, (short) mapTileSize, true);
            if(x == 0 || x == tileNumber-1) tileColor = Color.Aquamarine;
            else tileColor = Color.Azure;

            element.SetRendererConfig(tileColor);
            MAP_ELEMENTS.Add(element);
        }

    }

}