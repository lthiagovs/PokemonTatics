using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.UI;

namespace PokemonTFT.Table;

public static class GameDeck
{
    public const int DECK_SIZE = 7;

    private const int STATUSBAR_W = 80;
    private const int STATUSBAR_H = 14;
    private const int METER_W     = 6;
    private const int METER_H     = 8;
    private const int NAMEPLATE_W = 76;
    private const int NAMEPLATE_H = 18;
    private const int COSTPLATE_W = 32;
    private const int COSTPLATE_H = 16;
    private const int PORTRAIT_SRC = 40;
    private const int TYPE_ICON    = 34;
    private const int PLAY_W = 30;
    private const int PLAY_H = 32;

    private const int BAR_SCALE       = 3;
    private const int UI_SCALE        = 2;
    private const int METER_INSET_X   = 30;
    private const int METER_INSET_Y   = 10;
    private const int BAR_OFFSET_Y    = -15;
    private const int HP_BAR_X        = 80;
    private const int MANA_BAR_MARGIN = 100;
    private const int PLAY_OFFSET_X   = -40;
    private const int PLAY_OFFSET_Y   = -30;
    private const int CARD_ROW_LIFT   = 22;
    private const int NAMEPLATE_GAP   = 5;
    private const int PORTRAIT_INSET  = 15;
    private const int TYPE_ICON_INSET = 15;
    private const int COST_OFFSET_X   = 40;
    private const int COST_OFFSET_Y   = -20;
    private const int WINDOW_ROWS     = 3;

    private const int REROLL_RIGHT_MARGIN = 40;

    private static readonly List<GameElement> DECK_ELEMENTS = [];
    private static readonly List<Pokemon> DECK = [];

    private static GameInterfaceElement? _hpMeter;
    private static GameInterfaceElement? _manaMeter;
    private static GameInterfaceElement? _hpText;
    private static GameInterfaceElement? _manaText;

    public static IReadOnlyList<GameElement> GetDeck() => DECK_ELEMENTS;
    public static IReadOnlyList<Pokemon> GetDeckPokemons() => DECK;

    public static void RollDeck()
    {
        DECK.Clear();
        for (int i = 0; i < DECK_SIZE; i++) DECK.Add(PokemonDatabase.PokemonList[System.Random.Shared.Next(PokemonDatabase.PokemonList.Count)]);
    }

    public static void Reset()
    {
        RollDeck();
        Initialize();
    }

    public static void Initialize()
    {
        DECK_ELEMENTS.Clear();
        if (DECK.Count == 0) RollDeck();

        int screenWidth  = GameRenderer.GetScreenWidth();
        int screenHeight = GameRenderer.GetScreenHeight();
        int tileSize     = screenWidth / GameTable.TILE_COLUMNS;

        int windowW = screenWidth - tileSize * 2;
        int windowH = tileSize * WINDOW_ROWS;
        int windowX = tileSize;
        int windowY = screenHeight - windowH;

        var window = new GameInterfaceElement(windowX, windowY, windowW, windowH, VISIBLE: true);
        window.SetRendererConfig(GameRendererConfig.NineSlice("UI/Windows/window", SLICE_SIZE: 16, SLICE_PROPORTION: 4));
        DECK_ELEMENTS.Add(window);

        BuildStatusBar(window, "UI/Windows/hpbar", HP_BAR_X, GameGlobals.PLAYER_HP,
            out _hpMeter, out _hpText);

        BuildStatusBar(window, "UI/Windows/manabar", windowW - STATUSBAR_W * BAR_SCALE - MANA_BAR_MARGIN, GameGlobals.PLAYER_MANA,
            out _manaMeter, out _manaText);

        BuildPlayButton(windowX + windowW + PLAY_OFFSET_X, windowY + PLAY_OFFSET_Y);
        BuildRerollButton(
            windowX + windowW - NAMEPLATE_W * UI_SCALE - REROLL_RIGHT_MARGIN,
            windowY + windowH / 2 - NAMEPLATE_H * UI_SCALE / 2);
        BuildCards(windowX, windowY, windowW, windowH, tileSize);
    }

    public static void RequestRebuild() => _rebuildRequested = true;

    private static bool _rebuildRequested;

    #region STATUS BARS
    private static void BuildStatusBar(
        GameInterfaceElement WINDOW, string METER_TEXTURE, int LOCAL_X, int VALUE,
        out GameInterfaceElement METER, out GameInterfaceElement TEXT)
    {
        var bar = new GameInterfaceElement(LOCAL_X, BAR_OFFSET_Y,
            STATUSBAR_W * BAR_SCALE, STATUSBAR_H * BAR_SCALE, VISIBLE: true, PARENT: WINDOW);
        bar.SetRendererConfig(GameRendererConfig.Sprite("UI/Windows/statusbar", new Rectangle(0, 0, STATUSBAR_W, STATUSBAR_H)));

        METER = new GameInterfaceElement(METER_INSET_X, METER_INSET_Y,
            MeterWidth(VALUE), METER_H * BAR_SCALE, VISIBLE: true, PARENT: bar);
        METER.SetRendererConfig(GameRendererConfig.Sprite(METER_TEXTURE, new Rectangle(0, 0, METER_W, METER_H)));

        string label = $"{VALUE}/{GameGlobals.MAX_HP}";
        Vector2 size = GameRenderer.GetGameFont().MeasureString(label);

        TEXT = new GameInterfaceElement(
            (int)(STATUSBAR_W * BAR_SCALE / 2 - size.X / 2),
            (int)(STATUSBAR_H * BAR_SCALE / 2 - size.Y / 2),
            (int)size.X, (int)size.Y, VISIBLE: true, PARENT: bar);
        TEXT.SetRendererConfig(GameRendererConfig.Label(label));

        DECK_ELEMENTS.Add(bar);
        DECK_ELEMENTS.Add(METER);
        DECK_ELEMENTS.Add(TEXT);
    }

    private static int MeterWidth(int VALUE) => (int)(METER_W * BAR_SCALE * (VALUE / 100f * 10));
    #endregion

    #region PLAY BUTTON
    private static void BuildPlayButton(int X, int Y)
    {
        var play = new GameButton(X, Y, PLAY_W * UI_SCALE, PLAY_H * UI_SCALE, VISIBLE: true)
        {
            HOVERABLE = true,
            ON_CLICK  = StartRound
        };
        play.SetRendererConfig(GameRendererConfig.Sprite("UI/Icons/play_button", new Rectangle(0, 0, PLAY_W, PLAY_H)));
        DECK_ELEMENTS.Add(play);
    }

    private static void StartRound()
    {
        if (GameGlobals.GAME_STARTED) return;
        GameGlobals.GAME_STARTED = true;
        GameBonus.Refresh();
    }

    private static void BuildRerollButton(int X, int Y)
    {
        var reroll = new GameButton(X, Y, NAMEPLATE_W * UI_SCALE, NAMEPLATE_H * UI_SCALE, VISIBLE: true)
        {
            HOVERABLE   = true,
            IDLE_BOB    = 0f,
            HOVER_SCALE = 1.08f,
            ON_CLICK    = TryReroll
        };
        reroll.SetRendererConfig(GameRendererConfig.Sprite("UI/Windows/nameplate", new Rectangle(0, 0, NAMEPLATE_W, NAMEPLATE_H)));

        string label = $"REROLL {Balance.REROLL_COST}";
        Vector2 size = GameFonts.Measure(label, GameFonts.SMALL);
        var text = new GameInterfaceElement(
            (int)(NAMEPLATE_W * UI_SCALE / 2 - size.X / 2),
            (int)(NAMEPLATE_H * UI_SCALE / 2 - size.Y / 2),
            (int)size.X, (int)size.Y, VISIBLE: true, PARENT: reroll);
        text.SetRendererConfig(GameRendererConfig.Label(label));

        reroll.AddChild(text);
        DECK_ELEMENTS.Add(reroll);
    }

    private static void TryReroll()
    {
        if (GameGlobals.GAME_STARTED) return;
        if (GameGlobals.PLAYER_MANA < Balance.REROLL_COST) return;

        GameGlobals.ChangeMana(-Balance.REROLL_COST);
        RollDeck();
        RequestRebuild();
    }
    #endregion

    #region CARDS
    private static void BuildCards(int WINDOW_X, int WINDOW_Y, int WINDOW_W, int WINDOW_H, int TILE_SIZE)
    {
        int cardH  = (int)(WINDOW_H * 0.50);
        int cardW  = cardH;
        int gap    = TILE_SIZE / 4;
        int totalW = DECK_SIZE * cardW + (DECK_SIZE - 1) * gap;
        int cardY  = WINDOW_Y + TILE_SIZE / 2 - CARD_ROW_LIFT;

        int statsW = TILE_SIZE * 2;
        int innerW = WINDOW_W - statsW - TILE_SIZE;
        int startX = WINDOW_X + statsW + (innerW - totalW) / 2;

        for (int i = 0; i < DECK_SIZE && i < DECK.Count; i++)
        {
            BuildCard(DECK[i], startX + i * (cardW + gap), cardY, cardW, cardH);
        }
    }

    private static void BuildCard(Pokemon POKEMON, int X, int Y, int W, int H)
    {
        var card = new GameCardElement(X, Y, W, H, VISIBLE: true);
        card.SetRendererConfig(GameRendererConfig.NineSlice("UI/Windows/card_transparent", SLICE_SIZE: 24, SLICE_PROPORTION: 2));
        card.SetPokemon(POKEMON);
        DECK_ELEMENTS.Add(card);

        var nameplate = new GameInterfaceElement(X - 4, Y + H + NAMEPLATE_GAP,
            NAMEPLATE_W * UI_SCALE, NAMEPLATE_H * UI_SCALE, VISIBLE: true);
        nameplate.SetRendererConfig(GameRendererConfig.Sprite("UI/Windows/nameplate", new Rectangle(0, 0, NAMEPLATE_W, NAMEPLATE_H)));
        DECK_ELEMENTS.Add(nameplate);

        Vector2 nameSize = GameRenderer.GetGameFont().MeasureString(POKEMON.NAME);
        var name = new GameInterfaceElement(
            (int)(NAMEPLATE_W * UI_SCALE / 2 - nameSize.X / 2),
            (int)(NAMEPLATE_H * UI_SCALE / 2 - nameSize.Y / 2) + 2,
            (int)nameSize.X, (int)nameSize.Y, VISIBLE: true, PARENT: nameplate);
        name.SetRendererConfig(GameRendererConfig.Label(POKEMON.NAME));
        DECK_ELEMENTS.Add(name);

        var portrait = new GameInterfaceElement(PORTRAIT_INSET, PORTRAIT_INSET,
            W - PORTRAIT_INSET * 2, H - PORTRAIT_INSET * 2, VISIBLE: true, PARENT: card);
        portrait.SetRendererConfig(GameRendererConfig.Sprite(POKEMON.PortraitPath, new Rectangle(0, 0, PORTRAIT_SRC, PORTRAIT_SRC)));
        DECK_ELEMENTS.Add(portrait);

        var costPlate = new GameInterfaceElement(X + COST_OFFSET_X, Y + H + COST_OFFSET_Y,
            COSTPLATE_W * UI_SCALE, COSTPLATE_H * UI_SCALE, VISIBLE: true);
        costPlate.SetRendererConfig(GameRendererConfig.Sprite("UI/Windows/cost_plate", new Rectangle(0, 0, COSTPLATE_W, COSTPLATE_H)));
        DECK_ELEMENTS.Add(costPlate);

        string costLabel = POKEMON.COST.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Vector2 costSize = GameRenderer.GetGameFont().MeasureString(costLabel);
        var cost = new GameInterfaceElement(
            (int)(COSTPLATE_W * UI_SCALE / 2 - costSize.X / 2),
            (int)(COSTPLATE_H * UI_SCALE / 2 - costSize.Y / 2),
            (int)costSize.X, (int)costSize.Y, VISIBLE: true, PARENT: costPlate);
        cost.SetRendererConfig(GameRendererConfig.Label(costLabel));
        DECK_ELEMENTS.Add(cost);

        var typeIcon = new GameInterfaceElement(
            W - TYPE_ICON - TYPE_ICON_INSET, TYPE_ICON_INSET,
            TYPE_ICON, TYPE_ICON, VISIBLE: true, PARENT: card);
        typeIcon.SetRendererConfig(GameRendererConfig.Sprite(POKEMON.IconPath, new Rectangle(0, 0, TYPE_ICON, TYPE_ICON)));
        DECK_ELEMENTS.Add(typeIcon);
    }
    #endregion

    #region UPDATE
    public static void Update()
    {
        if (_rebuildRequested)
        {
            _rebuildRequested = false;
            Initialize();
        }

        UpdateMeter(_hpMeter,   _hpText,   GameGlobals.PLAYER_HP);
        UpdateMeter(_manaMeter, _manaText, GameGlobals.PLAYER_MANA);
    }

    private static void UpdateMeter(GameInterfaceElement? METER, GameInterfaceElement? TEXT, int VALUE)
    {
        if (METER == null) return;

        int width = MeterWidth(VALUE);
        METER.SIZE_X  = width;
        METER.VISIBLE = width > 0;

        if (TEXT != null) TEXT.GetRendererConfig().TEXT = $"{VALUE}/{GameGlobals.MAX_HP}";
    }
    #endregion
}
