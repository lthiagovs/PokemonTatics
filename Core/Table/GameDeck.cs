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

    private static GameInterfaceElement HP_METER;
    private static GameInterfaceElement MANA_METER;
    private static GameInterfaceElement HP_TEXT;
    private static GameInterfaceElement MANA_TEXT;

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
        GameDeck.BuildHpBar(winBg);
        GameDeck.BuildManaBar(winBg);

        // PLAY BUTTON
        var playButton = new GameButton(
            (short)(bgW-40+winBg.GetPosition().X), (short) (-30 + winBg.GetPosition().Y),
            (short) (30*2), (short) (32*2), true);
        playButton.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Icons/play_button", new Rectangle(0, 0, 30, 32), false));
        playButton.CONFIG = new GameInterfaceConfig(true, false);
        playButton.SetPosition(new Point(bgW-40+winBg.GetPosition().X,-30 + winBg.GetPosition().Y));
        DECK_ELEMENTS.Add(playButton);

        int cardGap  = tileSize / 4;
        int cardH    = (int)(bgH * 0.50);
        int cardW    = cardH;
        int totalW   = (DECK_SIZE * cardW) + ((DECK_SIZE - 1) * cardGap);
        int cardY    = bgY + bgTile - 22;
        int statsW   = tileSize * 2;
        int innerW   = bgW - statsW - tileSize;
        int startX   = bgX + statsW + ((innerW - totalW) / 2);

        for (int i = 0; i < DECK_SIZE; i++)
        {
            int cardX = startX + i * (cardW + cardGap);

            var card = new GameCardElement(
                (short)cardX, (short)cardY,
                (short)cardW, (short)cardH, true);
            card.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/card_transparent", Rectangle.Empty, true) { SLICE_SIZE = 24, SLICE_PROPORTION = 2 });
            card.CONFIG = new GameInterfaceConfig(true, false);
            card.SetPokemon(PokemonDecks[i]);
            GameDeck.DECK_ELEMENTS.Add(card);

            var sub_card = new GameInterfaceElement(
                (short) (cardX-4), (short)(cardY + cardH + 5),
                (short)(76 * 2), (short)(18 * 2), true);
            sub_card.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/nameplate", new Rectangle(0, 0, 76, 18), false));
            sub_card.CONFIG = new GameInterfaceConfig(false, false);
            GameDeck.DECK_ELEMENTS.Add(sub_card);

            int nameW    = (int)(GameRenderer.GetGameFont().MeasureString(PokemonDecks[i].NAME).X);
            int nameH    = (int)(GameRenderer.GetGameFont().MeasureString(PokemonDecks[i].NAME).Y);
            int nameX    = cardX + (76 * 2 / 2) - (nameW / 2);
            int nameCenY = cardY + cardH + 5 + (18 * 2 / 2) - (nameH / 2);

            var cardName = new GameInterfaceElement(
                (short)nameX, (short) (nameCenY+2),
                (short)nameW, (short)nameH, true);
            cardName.SetRendererConfig(new GameRendererConfig(Color.White, PokemonDecks[i].NAME, null, Rectangle.Empty, false));
            GameDeck.DECK_ELEMENTS.Add(cardName);

            var portrait = new GameInterfaceElement(
                (short)(cardX+15), (short)(cardY+15),
                (short)(cardW-30), (short)(cardH-30), true);
            portrait.SetRendererConfig(new GameRendererConfig(Color.White, null, $"Pokemons/{PokemonDecks[i].NAME}/portrait", new Rectangle(0, 0, 40, 40), false));
            GameDeck.DECK_ELEMENTS.Add(portrait);

            var costPlate = new GameInterfaceElement(
                (short) (cardX+40), (short)(cardY + cardH - 20),
                (short)(32 * 2), (short)(16 * 2), true);
            costPlate.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/cost_plate", new Rectangle(0, 0, 32, 16), false));
            costPlate.CONFIG = new GameInterfaceConfig(false, false);
            GameDeck.DECK_ELEMENTS.Add(costPlate);

            var cardCost = new GameInterfaceElement(
                (short) (costPlate.GetPosition().X + (costPlate.SIZE_X/2)-4), (short) (costPlate.GetPosition().Y + (costPlate.SIZE_Y/2)-7),
                (short)nameW, (short)nameH, true);
            cardCost.SetRendererConfig(new GameRendererConfig(Color.White, PokemonDecks[i].COST.ToString(), null, Rectangle.Empty, false));
            GameDeck.DECK_ELEMENTS.Add(cardCost);

            GameDeck.BuildPokemonTypeIcon(card);
        }

        int iconW       = 20;
        int iconH       = 22;
        int iconSpacing = 10;
        int totalStatH  = (iconH * 2) + iconSpacing;
        int statStartY  = bgY + (bgH / 2) - (totalStatH / 2);
        int statIconX   = bgX + tileSize;
        int statTextX   = statIconX + iconW + 4;
        int statTextW   = statsW - iconW - 8;

    }

    public static void Reset()
    {
        GameDeck.DECK_ELEMENTS.Clear();
        GameDeck.Initialize();
    }

    public static void BuildHpBar(GameInterfaceElement WINDOW)
    {
        int scale = 3;
        int stats_bar_size = (int)(GameGlobals.PLAYER_HP / 100f * 10);

        var hp_bar = new GameInterfaceElement(
                (short)(80), (short)(-20),
                (short)(80 * scale), (short)(14 * scale), true);
            hp_bar.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/statusbar", new Rectangle(0, 0, 80, 14), false));
            hp_bar.PARENT = WINDOW;
            hp_bar.SetPosition(new Point(80, -15));
            hp_bar.CONFIG = new GameInterfaceConfig(false, false);

        var hp_meter = new GameInterfaceElement(
                (short)(30), (short)(10),
                (short)(6 * scale * stats_bar_size), (short)(8 * scale), true);
            hp_meter.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/hpbar", new Rectangle(0, 0, 6, 8), false));
            hp_meter.PARENT = hp_bar;
            hp_meter.SetPosition(new Point(30, 10));
            hp_meter.CONFIG = new GameInterfaceConfig(false, false);

        string hpText = $"{GameGlobals.PLAYER_HP}/100";
        Vector2 hpTextSize = GameRenderer.GetGameFont().MeasureString(hpText);
        var hp_text = new GameInterfaceElement(
                (short)0, (short)0,
                (short)hpTextSize.X, (short)hpTextSize.Y, true);
            hp_text.SetRendererConfig(new GameRendererConfig(Color.White, hpText, null, Rectangle.Empty, false));
            hp_text.PARENT = hp_bar;
            hp_text.SetPosition(new Point(
                (int)((80 * scale) / 2 - hpTextSize.X / 2),
                (int)((14 * scale) / 2 - hpTextSize.Y / 2)
            ));
            hp_text.CONFIG = new GameInterfaceConfig(false, false);

        GameDeck.DECK_ELEMENTS.Add(hp_bar);
        GameDeck.DECK_ELEMENTS.Add(hp_meter);
        GameDeck.DECK_ELEMENTS.Add(hp_text);
        GameDeck.HP_METER = hp_meter;
        GameDeck.HP_TEXT = hp_text;
    }

    public static void BuildManaBar(GameInterfaceElement WINDOW)
    {
        int scale = 3;
        int stats_bar_size = (int)(GameGlobals.PLAYER_MANA / 100f * 10);

        var mana_bar = new GameInterfaceElement(
                (short)(0), (short)(0),
                (short)(80 * scale), (short)(14 * scale), true);
            mana_bar.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/statusbar", new Rectangle(0, 0, 80, 14), false));
            mana_bar.PARENT = WINDOW;
            mana_bar.SetPosition(new Point(WINDOW.SIZE_X - (80 * scale) - 100, -15));
            mana_bar.CONFIG = new GameInterfaceConfig(false, false);

        var mana_meter = new GameInterfaceElement(
                (short)(30), (short)(10),
                (short)(6 * scale * stats_bar_size), (short)(8 * scale), true);
            mana_meter.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/manabar", new Rectangle(0, 0, 6, 8), false));
            mana_meter.PARENT = mana_bar;
            mana_meter.SetPosition(new Point(30, 10));
            mana_meter.CONFIG = new GameInterfaceConfig(false, false);

        string manaText = $"{GameGlobals.PLAYER_MANA}/100";
        Vector2 manaTextSize = GameRenderer.GetGameFont().MeasureString(manaText);
        var mana_text = new GameInterfaceElement(
                (short)0, (short)0,
                (short)manaTextSize.X, (short)manaTextSize.Y, true);
            mana_text.SetRendererConfig(new GameRendererConfig(Color.White, manaText, null, Rectangle.Empty, false));
            mana_text.PARENT = mana_bar;
            mana_text.SetPosition(new Point(
                (int)((80 * scale) / 2 - manaTextSize.X / 2),
                (int)((14 * scale) / 2 - manaTextSize.Y / 2)
            ));
            mana_text.CONFIG = new GameInterfaceConfig(false, false);

        GameDeck.DECK_ELEMENTS.Add(mana_bar);
        GameDeck.DECK_ELEMENTS.Add(mana_meter);
        GameDeck.DECK_ELEMENTS.Add(mana_text);
        GameDeck.MANA_METER = mana_meter;
        GameDeck.MANA_TEXT = mana_text;
    }

    public static void BuildPokemonTypeIcon(GameCardElement CARD)
    {
        Pokemon _pkmEntity = CARD.GetPokemon();

        int scale = 1;
        int assetSize = 34;
        int size = assetSize*scale;
        String _assetsName = _pkmEntity.TYPE.ToString().ToLower();

        var pkmTypeIcon = new GameInterfaceElement(
                (short) (0), (short)(0),
                (short)(size), (short)(size), true);
            pkmTypeIcon.SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Types/"+_assetsName, new Rectangle(0, 0, assetSize, assetSize), false));
            pkmTypeIcon.PARENT = CARD;
            pkmTypeIcon.SetPosition(new Point(CARD.SIZE_X-size-15,15));
            pkmTypeIcon.CONFIG = new GameInterfaceConfig(false, false);
        
        GameDeck.DECK_ELEMENTS.Add(pkmTypeIcon);
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

    public static void Update()
    {
        int scale = 3;
        int hpSize   = (int)(6 * scale * (GameGlobals.PLAYER_HP   / 100f * 10));
        int manaSize = (int)(6 * scale * (GameGlobals.PLAYER_MANA / 100f * 10));
        if(HP_METER != null)
        {
            HP_METER.SIZE_X  = (short)hpSize;
            HP_METER.VISIBLE = hpSize > 0;
            GameDeck.HP_TEXT.SetRendererConfig(new GameRendererConfig(Color.White, $"{GameGlobals.PLAYER_HP}/100", null, Rectangle.Empty, false));
        }
        if(MANA_METER != null)
        {
            MANA_METER.SIZE_X  = (short)manaSize;
            MANA_METER.VISIBLE = manaSize > 0;
            GameDeck.MANA_TEXT.SetRendererConfig(new GameRendererConfig(Color.White, $"{GameGlobals.PLAYER_MANA}/100", null, Rectangle.Empty, false));
        }
    }
    
}