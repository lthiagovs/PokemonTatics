using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

public static class GameDeck
{
    
    private static List<GameElement> DECK_ELEMENTS = new List<GameElement>();

    public static List<GameElement> GetDeck() { return GameDeck.DECK_ELEMENTS; }

    private static int DECK_SIZE = 7;

    public static void Initialize()
    {
        
        int screenWidth = GameRenderer.GetScreenWidth();
        int screenHeight = GameRenderer.GetScreenHeight();

        //BACKGROUND
        short sizeX = (short) (screenWidth/1.5);
        short sizeY = (short) (screenHeight/6);

        short posX = (short) ((screenWidth/2) - (sizeX/2));
        short posY = (short) (screenHeight - (sizeY*1.5));

        GameInterfaceElement deckBackground = new GameInterfaceElement(posX, posY, sizeX, sizeY, true);
        deckBackground.SetRendererConfig(Color.Gray);

        DECK_ELEMENTS.Add(deckBackground);

        //DECKS
        for(int i = 0; i < GameDeck.DECK_SIZE; i++){
            GameInterfaceElement decks = new GameInterfaceElement((short) (i*sizeX*0.15), (short)(sizeY * 0.1), (short) (sizeX*0.10), (short) (sizeY-(sizeY*0.2)), true, deckBackground);
            decks.SetRendererConfig(Color.White);
            decks.MOUSE_HOVER = true;

            DECK_ELEMENTS.Add(decks);
        }

        //TEXT
        GameInterfaceElement text1 = new GameInterfaceElement((short) 0, 0, (short) (sizeX*0.10), (short) (sizeY-(sizeY*0.2)), true, deckBackground);
            text1.SetRendererConfig(Color.Black, "STATS 1: X");

        GameInterfaceElement text2 = new GameInterfaceElement((short) 100, 0, (short) (sizeX*0.10), (short) (sizeY-(sizeY*0.2)), true, deckBackground);
            text2.SetRendererConfig(Color.Black, "STATS 2: X");

        DECK_ELEMENTS.Add(text1);
        DECK_ELEMENTS.Add(text2);


    }

}