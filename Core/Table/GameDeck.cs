using System;
using System.Collections.Generic;
using System.Globalization;
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

    private const int PORTRAIT_SRC = 40;
    private const int TYPE_ICON    = 34;

    private const int CARD_ROW_LIFT   = 22;
    private const int WINDOW_ROWS     = 3;
    private const int STATS_PAD       = 22;
    private const int BAR_H           = 30;
    private const int BAR_GAP         = 18;
    private const int LABEL_H         = 24;
    private const int NAME_PLATE_H    = 28;
    private const int HEADER_H        = 30;
    private const int CARD_PAD        = 7;
    private const int BUTTON_MARGIN   = 16;
    private const int BUTTON_MIN_W    = 170;
    private const int STATS_TOP       = 26;
    private const int STATS_W         = 330;
    private const int RIGHT_RESERVE   = 190;

    private static readonly List<GameElement> DECK_ELEMENTS = [];
    private static readonly List<Pokemon> DECK = [];

    private static GameInterfaceElement? _hpFill;
    private static GameInterfaceElement? _manaFill;
    private static GameInterfaceElement? _hpText;
    private static GameInterfaceElement? _manaText;
    private static GameButton? _playButton;
    private static GameButton? _rerollButton;
    private static GameInterfaceElement? _rerollGlyph;
    private static Rectangle _hpBar;
    private static Rectangle _manaBar;
    private static int _lastOffer = 1;

    public static IReadOnlyList<GameElement> GetDeck() => DECK_ELEMENTS;

    public static void ReplaceCard(Pokemon CARD)
    {
        int index = DECK.IndexOf(CARD);
        if (index < 0) return;

        var taken = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < DECK.Count; i++)
            if (i != index) taken.Add(DECK[i].LINE);

        Pokemon? pick = PokemonDatabase.RandomWeighted(GameGlobals.LEVEL, taken);
        if (pick == null) return;

        DECK[index] = pick;
        MarkOffer();
        RequestRebuild();
    }

    public static void RollRound()
    {
        RollDeck();

        List<string> owned = OwnedLines();
        if (owned.Count > 0 && ShopRules.OfferDue(GameGlobals.LEVEL, _lastOffer, GameSettings.DIFFICULTY))
            ShopRules.Guarantee(DECK, owned, GameGlobals.LEVEL, Random.Shared);

        MarkOffer();
        RequestRebuild();
    }

    private static void MarkOffer()
    {
        if (ShopRules.Offers(DECK, OwnedLines())) _lastOffer = GameGlobals.LEVEL;
    }

    private static List<string> OwnedLines()
    {
        var lines = new List<string>();
        List<GameElement> entities = GameTableElement.GetTableElements();

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not PokemonEntity { ENEMY: false, POKEMON: { } pokemon }) continue;
            if (pokemon.LINE.Length > 0 && !lines.Contains(pokemon.LINE)) lines.Add(pokemon.LINE);
        }

        return lines;
    }

    public static void RollDeck()
    {
        DECK.Clear();

        var taken = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < DECK_SIZE; i++)
        {
            Pokemon? pick = PokemonDatabase.RandomWeighted(GameGlobals.LEVEL, taken);
            if (pick == null) break;

            taken.Add(pick.LINE);
            DECK.Add(pick);
        }
    }

    public static void Reset()
    {
        _lastOffer = GameGlobals.LEVEL;
        RollDeck();
        Initialize();
    }

    public static void Initialize()
    {
        DECK_ELEMENTS.Clear();
        if (DECK.Count == 0) RollDeck();

        int screenWidth  = GameRenderer.GetScreenWidth();
        int screenHeight = GameRenderer.GetScreenHeight();
        int tileSize     = screenWidth / GameTable.UI_COLUMNS;

        int windowW = screenWidth - tileSize * 2;
        int windowH = tileSize * WINDOW_ROWS;
        int windowX = tileSize;
        int windowY = screenHeight - windowH;

        GameInterfaceElement window = UIFactory.Panel(windowX, windowY, windowW, windowH);
        DECK_ELEMENTS.Add(window);

        BuildStatusPanel(window, STATS_W, windowH);

        BuildPlayButton(windowX + windowW, windowY);
        BuildRerollButton(windowX + windowW, windowY + windowH / 2);
        BuildCards(windowX, windowY, windowW, windowH, tileSize);
    }

    public static void RequestRebuild() => _rebuildRequested = true;

    private static bool _rebuildRequested;

    #region STATUS BARS
    #endregion

    #region PLAY BUTTON
    private static void BuildStatusPanel(GameInterfaceElement WINDOW, int WIDTH, int HEIGHT)
    {
        int barW = WIDTH - STATS_PAD * 2;
        int top = STATS_TOP;

        UIFactory.IconLabel("favorite", "HP", STATS_PAD, top, WINDOW);
        _hpBar = new Rectangle(STATS_PAD, top + LABEL_H, barW, BAR_H);
        UIFactory.Bar(_hpBar.X, _hpBar.Y, _hpBar.Width, _hpBar.Height, UITheme.HEALTH, WINDOW, out _hpFill);
        _hpText = UIFactory.LabelBox(string.Empty, _hpBar, WINDOW);

        int second = top + LABEL_H + BAR_H + BAR_GAP;
        UIFactory.IconLabel("bolt", "MANA", STATS_PAD, second, WINDOW);
        _manaBar = new Rectangle(STATS_PAD, second + LABEL_H, barW, BAR_H);
        UIFactory.Bar(_manaBar.X, _manaBar.Y, _manaBar.Width, _manaBar.Height, UITheme.MANA, WINDOW, out _manaFill);
        _manaText = UIFactory.LabelBox(string.Empty, _manaBar, WINDOW);

        RefreshMeters();
    }

    private static void RefreshMeters()
    {
        SetMeter(_hpFill, _hpText, _hpBar, GameGlobals.PLAYER_HP, GameGlobals.MAX_HP);
        SetMeter(_manaFill, _manaText, _manaBar, GameGlobals.PLAYER_MANA, GameGlobals.MAX_MANA);
    }

    private static void SetMeter(GameInterfaceElement? FILL, GameInterfaceElement? TEXT, Rectangle BAR,
                                 int VALUE, int MAX)
    {
        if (FILL == null || TEXT == null) return;

        int width = UIFactory.BarFillWidth(BAR.Width, VALUE, MAX);
        FILL.SIZE_X = width;
        FILL.VISIBLE = width > 0;

        TEXT.GetRendererConfig().TEXT = $"{VALUE}/{MAX}";
    }

    private static void BuildPlayButton(int RIGHT, int BOTTOM)
    {
        _playButton = UIFactory.Button("START", StartRound, UITheme.GLASS, UITheme.TEXT, BUTTON_MIN_W, ICON: "play_arrow");
        _playButton.SetPosition(new Point(RIGHT - _playButton.SIZE_X - BUTTON_MARGIN,
            BOTTOM - _playButton.SIZE_Y - BUTTON_MARGIN));
        DECK_ELEMENTS.Add(_playButton);
    }

    private static bool CanStart() => !GameGlobals.GAME_STARTED && GameTableLogic.GetPlayerPokemonsCount() > 0;

    private static void StartRound()
    {
        if (GameGlobals.GAME_STARTED) return;

        ItemLogic.ReturnCarried();
        GameGlobals.GAME_STARTED = true;
        GameBonus.Refresh();
    }

    private static void BuildRerollButton(int RIGHT, int CENTER_Y)
    {
        _rerollButton = UIFactory.IconButton("refresh", TryReroll);
        _rerollButton.SetPosition(new Point(RIGHT - _rerollButton.SIZE_X - BUTTON_MARGIN,
            CENTER_Y - _rerollButton.SIZE_Y / 2));
        _rerollGlyph = UIFactory.LastIcon;
        DECK_ELEMENTS.Add(_rerollButton);
    }

    private static void RefreshReroll()
    {
        if (_rerollButton == null) return;

        bool usable = GameGlobals.REROLLS_LEFT > 0 && !GameGlobals.GAME_STARTED;
        _rerollButton.ENABLED = usable;

        if (_rerollGlyph != null)
            _rerollGlyph.GetRendererConfig().COLOR = usable ? UITheme.TEXT : UITheme.TEXT_DIM;
    }

    private static void TryReroll()
    {
        if (GameGlobals.GAME_STARTED) return;
        if (GameGlobals.REROLLS_LEFT <= 0) return;
        if (GameGlobals.PLAYER_MANA < Balance.REROLL_COST) return;

        GameGlobals.REROLLS_LEFT--;
        GameGlobals.ChangeMana(-Balance.REROLL_COST);
        RollDeck();
        MarkOffer();
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

        int statsW = STATS_W;
        int innerW = WINDOW_W - statsW - RIGHT_RESERVE;
        int startX = WINDOW_X + statsW + (innerW - totalW) / 2;

        for (int i = 0; i < DECK_SIZE && i < DECK.Count; i++)
        {
            BuildCard(DECK[i], startX + i * (cardW + gap), cardY, cardW, cardH);
        }
    }

    private static void BuildCard(Pokemon POKEMON, int X, int Y, int W, int H)
    {
        bool affordable = POKEMON.COST <= GameGlobals.PLAYER_MANA;
        int inner = UIFactory.BORDER;
        int innerW = W - inner * 2;

        var card = new GameCardElement(X, Y, W, H, VISIBLE: true);
        card.SetRendererConfig(GameRendererConfig.Solid(affordable ? UITheme.BORDER : UITheme.TRACK));
        card.SetPokemon(POKEMON);
        DECK_ELEMENTS.Add(card);

        Add(new GameInterfaceElement(inner, inner, innerW, H - inner * 2, VISIBLE: true, PARENT: card),
            GameRendererConfig.Glass(UITheme.GLASS));

        Add(new GameInterfaceElement(inner, inner, innerW, HEADER_H, VISIBLE: true, PARENT: card),
            GameRendererConfig.Solid(UITheme.SURFACE_DEEP));

        string costLabel = POKEMON.COST.ToString(CultureInfo.InvariantCulture);
        Vector2 costSize = GameFonts.Measure(costLabel, GameFonts.BODY);
        Add(new GameInterfaceElement(inner + CARD_PAD, inner + (HEADER_H - (int)costSize.Y) / 2,
                (int)costSize.X, (int)costSize.Y, VISIBLE: true, PARENT: card),
            GameRendererConfig.Label(costLabel, affordable ? UITheme.ACCENT : UITheme.TEXT_DIM,
                GameFonts.BODY, TEXT_SHADOW: 1));

        int icon = HEADER_H - CARD_PAD;
        Add(new GameInterfaceElement(W - inner - CARD_PAD - icon, inner + (HEADER_H - icon) / 2,
                icon, icon, VISIBLE: true, PARENT: card),
            GameRendererConfig.Sprite(POKEMON.IconPath, new Rectangle(0, 0, TYPE_ICON, TYPE_ICON)));

        int artTop = inner + HEADER_H;
        int artH = H - inner * 2 - HEADER_H - NAME_PLATE_H;
        int art = Math.Min(innerW, artH);
        Add(new GameInterfaceElement(inner + (innerW - art) / 2, artTop + (artH - art) / 2,
                art, art, VISIBLE: true, PARENT: card),
            GameRendererConfig.Sprite(POKEMON.PortraitPath, new Rectangle(0, 0, PORTRAIT_SRC, PORTRAIT_SRC),
                affordable ? Color.White : Color.White * 0.45f));

        int plateTop = H - inner - NAME_PLATE_H;
        Add(new GameInterfaceElement(inner, plateTop, innerW, NAME_PLATE_H, VISIBLE: true, PARENT: card),
            GameRendererConfig.Solid(UITheme.SURFACE_DEEP));

        Vector2 nameSize = GameFonts.Measure(POKEMON.NAME, GameFonts.SMALL);
        Add(new GameInterfaceElement(W / 2 - (int)(nameSize.X / 2f),
                plateTop + (NAME_PLATE_H - (int)nameSize.Y) / 2,
                (int)nameSize.X, (int)nameSize.Y, VISIBLE: true, PARENT: card),
            GameRendererConfig.Label(POKEMON.NAME, UITheme.TEXT, GameFonts.SMALL, TEXT_SHADOW: 1));
    }

    private static void Add(GameInterfaceElement ELEMENT, GameRendererConfig CONFIG)
    {
        ELEMENT.SetRendererConfig(CONFIG);
        if (ELEMENT.PARENT is GameCardElement card) card.AddPart(ELEMENT);
        DECK_ELEMENTS.Add(ELEMENT);
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

        RefreshMeters();
        RefreshReroll();

        if (_playButton != null) _playButton.VISIBLE = CanStart();
    }
    #endregion
}
