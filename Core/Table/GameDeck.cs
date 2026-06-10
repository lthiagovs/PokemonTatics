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

    private static readonly Dictionary<string, Rectangle> WIN = new()
    {
        { "top_left_corner",     new Rectangle( 0,  0, 16, 16) },
        { "top_border",          new Rectangle(16,  0, 16, 16) },
        { "top_right_corner",    new Rectangle(32,  0, 16, 16) },
        { "left_border",         new Rectangle( 0, 16, 16, 16) },
        { "tile",                new Rectangle(16, 16, 16, 16) },
        { "right_border",        new Rectangle(32, 16, 16, 16) },
        { "bottom_left_corner",  new Rectangle( 0, 32, 16, 16) },
        { "bottom_border",       new Rectangle(16, 32, 16, 16) },
        { "bottom_right_corner", new Rectangle(32, 32, 16, 16) },
    };

    public static void Initialize()
    {
        int screenWidth  = GameRenderer.GetScreenWidth();
        int screenHeight = GameRenderer.GetScreenHeight();
        int tileSize = screenWidth / 20;
        int bgTile   = tileSize / 2;

        short bgW = (short)(screenWidth - tileSize * 2);
        short bgH = (short)(tileSize * 3);
        short bgX = (short)tileSize;
        short bgY = (short)(screenHeight - bgH);

        int tilesX = bgW / bgTile;
        int tilesY = bgH / bgTile;

        // WINDOW BORDER
        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                int posX = bgX + (x * bgTile);
                int posY = bgY + (y * bgTile);

                bool isTop    = y == 0;
                bool isBottom = y == tilesY - 1;
                bool isLeft   = x == 0;
                bool isRight  = x == tilesX - 1;

                Rectangle src;
                if      (isTop    && isLeft)  src = WIN["top_left_corner"];
                else if (isTop    && isRight) src = WIN["top_right_corner"];
                else if (isBottom && isLeft)  src = WIN["bottom_left_corner"];
                else if (isBottom && isRight) src = WIN["bottom_right_corner"];
                else if (isTop)               src = WIN["top_border"];
                else if (isBottom)            src = WIN["bottom_border"];
                else if (isLeft)              src = WIN["left_border"];
                else if (isRight)             src = WIN["right_border"];
                else                          src = WIN["tile"];

                var element = new GameInterfaceElement(
                    (short)posX, (short)posY,
                    (short)bgTile, (short)bgTile, true);
                element.SetRendererConfig(Color.White, src, null, "UI/Windows/window");
                DECK_ELEMENTS.Add(element);
            }
        }

        // CARDS
        int cardGap = tileSize / 4;
        int cardH   = (int)(bgH * 0.75);
        int cardW   = (int)(cardH * 0.65);
        int totalW  = (DECK_SIZE * cardW) + ((DECK_SIZE - 1) * cardGap);
        int cardY   = (bgH - cardH) / 2;

        int statsW  = tileSize * 2;
        int innerW  = bgW - statsW - tileSize;
        int startX  = bgX + statsW + ((innerW - totalW) / 2);

        for (int i = 0; i < DECK_SIZE; i++)
        {
            int cardX = startX + i * (cardW + cardGap);
            GameInterfaceElement card = new GameInterfaceElement(
                (short)cardX, (short)(bgY + cardY),
                (short)cardW, (short)cardH,
                true);
            card.SetRendererConfig(Color.White, Rectangle.Empty);
            card.MOUSE_HOVER = true;

            int nameH = (int)(cardH * 0.20);
            int nameY = (int)(cardH * 0.04);
            GameInterfaceElement cardName = new GameInterfaceElement(
                0, (short)nameY,
                (short)cardW, (short)nameH,
                true, card);
            cardName.SetRendererConfig(Color.White, Rectangle.Empty, PokemonDecks[i].NAME);

            DECK_ELEMENTS.Add(card);
            DECK_ELEMENTS.Add(cardName);
        }

        // STATS
        int iconSize    = 20;
        int iconSpacing = 6;
        int totalStatH  = (iconSize * 2) + iconSpacing;
        int statStartY  = bgY + (bgH / 2) - (totalStatH / 2);
        int statIconX   = bgX + tileSize;
        int statTextX   = statIconX + iconSize + (cardGap / 2);
        int statTextW   = statsW - iconSize - cardGap;

        // HP
        var iconHp = new GameInterfaceElement(
            (short)statIconX, (short)statStartY,
            (short)iconSize, (short)iconSize, true);
        iconHp.SetRendererConfig(Color.White, Rectangle.Empty, null, "UI/Icons/hp");

        var textHp = new GameInterfaceElement(
            (short)statTextX, (short)statStartY,
            (short)statTextW, (short)iconSize, true);
        textHp.SetRendererConfig(Color.White, Rectangle.Empty, "HEALTH: X");

        // MANA
        var iconMana = new GameInterfaceElement(
            (short)statIconX, (short)(statStartY + iconSize + iconSpacing),
            (short)iconSize, (short)iconSize, true);
        iconMana.SetRendererConfig(Color.White, Rectangle.Empty, null, "UI/Icons/mana");

        var textMana = new GameInterfaceElement(
            (short)statTextX, (short)(statStartY + iconSize + iconSpacing),
            (short)statTextW, (short)iconSize, true);
        textMana.SetRendererConfig(Color.White, Rectangle.Empty, "MANA: X");

        DECK_ELEMENTS.Add(iconHp);
        DECK_ELEMENTS.Add(textHp);
        DECK_ELEMENTS.Add(iconMana);
        DECK_ELEMENTS.Add(textMana);
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