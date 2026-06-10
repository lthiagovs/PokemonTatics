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
        int tileSize     = screenWidth / 20;
        int bgTile       = tileSize / 2;

        int bgW = screenWidth - tileSize * 2;
        int bgH = tileSize * 3;
        int bgX = tileSize;
        int bgY = screenHeight - bgH;

        var winBg = new GameInterfaceElement(
            (short)bgX, (short)bgY,
            (short)bgW, (short)bgH, true);
        winBg.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/window", Rectangle.Empty, true) { SLICE_SIZE = 16, SLICE_PROPORTION = 4 });
        DECK_ELEMENTS.Add(winBg);

        int cardGap      = tileSize / 4;
        int cardH        = (int)(bgH * 0.50);
        int cardW        = (int)(cardH * 0.90);
        int totalW       = (DECK_SIZE * cardW) + ((DECK_SIZE - 1) * cardGap);
        int cardY        = bgY + bgTile;
        int statsW       = tileSize * 2;
        int innerW       = bgW - statsW - tileSize;
        int startX       = bgX + statsW + ((innerW - totalW) / 2);
        int portraitSize = 40;

        for (int i = 0; i < DECK_SIZE; i++)
        {
            int cardX = startX + i * (cardW + cardGap);

            var card = new GameInterfaceElement(
                (short)cardX, (short)cardY,
                (short)cardW, (short)cardH, true);
            card.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/card", Rectangle.Empty, true) { SLICE_SIZE = 24, SLICE_PROPORTION = 2 });
            DECK_ELEMENTS.Add(card);

            int nameH   = (int)(cardH * 0.18);
            int nameY   = cardY - nameH - 2;

            var cardName = new GameInterfaceElement(
                (short)cardX, (short)nameY,
                (short)cardW, (short)nameH, true);
            cardName.SetRendererConfig(new GameRendererConfig(Color.White, PokemonDecks[i].NAME, null, Rectangle.Empty, false));
            DECK_ELEMENTS.Add(cardName);

            int portraitX = cardX + (cardW / 2) - (portraitSize / 2);
            int portraitY = cardY + (cardH / 2) - (portraitSize / 2);

            var portrait = new GameInterfaceElement(
                (short)portraitX, (short)portraitY,
                (short)portraitSize, (short)portraitSize, true);
            portrait.SetRendererConfig(new GameRendererConfig(Color.White, null, $"Pokemons/{PokemonDecks[i].NAME}/portrait", new Rectangle(0, 0, portraitSize, portraitSize), false));
            DECK_ELEMENTS.Add(portrait);
        }

        int iconW      = 20;
        int iconH      = 22;
        int iconSpacing = 10;
        int totalStatH = (iconH * 2) + iconSpacing;
        int statStartY = bgY + (bgH / 2) - (totalStatH / 2);
        int statIconX  = bgX + tileSize;
        int statTextX  = statIconX + iconW + 4;
        int statTextW  = statsW - iconW - 8;

        var iconHp = new GameInterfaceElement(
            (short)statIconX, (short)statStartY,
            (short)iconW, (short)iconH, true);
        iconHp.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Icons/hp", new Rectangle(0, 0, 20, 22), false));

        var textHp = new GameInterfaceElement(
            (short)statTextX, (short)(statStartY + (iconH / 2) - (iconH / 2)),
            (short)statTextW, (short)iconH, true);
        textHp.SetRendererConfig(new GameRendererConfig(Color.White, "HEALTH : X", null, Rectangle.Empty, false));

        var iconMana = new GameInterfaceElement(
            (short)statIconX, (short)(statStartY + iconH + iconSpacing),
            (short)iconW, (short)iconH, true);
        iconMana.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Icons/mana", new Rectangle(0, 0, 20, 22), false));

        var textMana = new GameInterfaceElement(
            (short)statTextX, (short)(statStartY + iconH + iconSpacing),
            (short)statTextW, (short)iconH, true);
        textMana.SetRendererConfig(new GameRendererConfig(Color.White, "MANA : X", null, Rectangle.Empty, false));

        DECK_ELEMENTS.Add(iconHp);
        DECK_ELEMENTS.Add(textHp);
        DECK_ELEMENTS.Add(iconMana);
        DECK_ELEMENTS.Add(textMana);
    }

    public static void GetRandomDecks()
    {
        Random _random = new Random();
        GameDeck.PokemonDecks.Clear();

        for (int i = 0; i < DECK_SIZE; i++)
        {
            int index = _random.Next(0, PokemonDatabase.PokemonList.Count);
            PokemonDecks.Add(PokemonDatabase.PokemonList[index]);
        }
    }
}