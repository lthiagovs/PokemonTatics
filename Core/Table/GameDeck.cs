using System;
using System.Collections.Generic;
using ENGINE.MODELS;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

public static class GameDeck
{
    private static List<GameElement> DECK_ELEMENTS = new List<GameElement>();
    public static List<GameElement> GetDeck() { return GameDeck.DECK_ELEMENTS; }
    public static List<Pokemon> PokemonDecks = new List<Pokemon>();

    private static int DECK_SIZE = 7;

    public static void Initialize()
    {
        int screenWidth  = GameRenderer.GetScreenWidth();
        int screenHeight = GameRenderer.GetScreenHeight();

        int tileSize = screenWidth / 20;
        short bgW    = (short)(screenWidth - tileSize * 2);   
        short bgH    = (short)(tileSize * 2);                 
        short bgX    = (short)tileSize;                       
        short bgY    = (short)(screenHeight - bgH);
        GameInterfaceElement deckBackground = new GameInterfaceElement(bgX, bgY, bgW, bgH, true);
        deckBackground.SetRendererConfig(Color.Gray);
        DECK_ELEMENTS.Add(deckBackground);

        //CARDS
        int cardGap  = tileSize / 4;                          
        int cardH    = (int)(bgH * 0.80);                     
        int cardW    = (int)(cardH * 0.65);                   
        int totalW   = (DECK_SIZE * cardW) + ((DECK_SIZE - 1) * cardGap);
        int startX   = (bgW - totalW) / 2;                    
        int cardY    = (bgH - cardH) / 2;                    

        for (int i = 0; i < DECK_SIZE; i++)
        {
            int cardX = startX + i * (cardW + cardGap);
            GameInterfaceElement card = new GameInterfaceElement(
                (short)cardX, (short)cardY,
                (short)cardW, (short)cardH,
                true, deckBackground);
            card.SetRendererConfig(Color.White);
            card.MOUSE_HOVER = true;

            //CARD INFOS
            GameInterfaceElement cardName = new GameInterfaceElement(0, 0, 0, 0, true, card);
            cardName.SetRendererConfig(Color.Black, PokemonDatabase.PokemonList[i].NAME);
            //CARD INFOS

            DECK_ELEMENTS.Add(card);
            DECK_ELEMENTS.Add(cardName);
        }

        int textH = cardH / 2;
        int textW = tileSize * 3;
        int textX = bgW - textW - cardGap;
        
        GameInterfaceElement text1 = new GameInterfaceElement(
            (short)textX, (short)cardY,
            (short)textW, (short)textH,
            true, deckBackground);
        text1.SetRendererConfig(Color.Black, "HEALTH 1: X");

        GameInterfaceElement text2 = new GameInterfaceElement(
            (short)textX, (short)(cardY + textH),
            (short)textW, (short)textH,
            true, deckBackground);
        text2.SetRendererConfig(Color.Black, "GOLD 2: X");

        DECK_ELEMENTS.Add(text1);
        DECK_ELEMENTS.Add(text2);
    }

    public static void GetRandomDecks()
    {
        Random _random = new Random();
        GameDeck.PokemonDecks.Clear();

        for(int i = 0;i < DECK_SIZE; i++)
        {
            int index = _random.Next(0, PokemonDatabase.PokemonList.Count-1);
            PokemonDecks.Add(PokemonDatabase.PokemonList[index]);

        }
    }

}