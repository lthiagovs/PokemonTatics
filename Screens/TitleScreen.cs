using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.UI;

namespace PokemonTFT.Screens;

public static class TitleScreen
{
    public const string GAME_TITLE = "POKEMON TACTICS";
    private const string SUBTITLE  = "AUTO BATTLER";

    private const int WALLPAPER_W = 1920;
    private const int WALLPAPER_H = 1200;

    private const float TITLE_Y    = 0.12f;
    private const float SUBTITLE_Y = 0.22f;
    private const float HINT_Y     = 0.62f;
    private const float MENU_Y     = 0.56f;

    private const float TITLE_WIDTH_RATIO = 0.42f;

    private const int MENU_W   = 300;
    private const int MENU_GAP = 22;

    private static readonly List<GameElement> TITLE_ELEMENTS = [];
    private static readonly List<GameElement> MENU = [];

    public static IReadOnlyList<GameElement> GetElements() => TITLE_ELEMENTS;

    public static IReadOnlyList<GameElement> GetMenu() => MENU;

    public static void Initialize()
    {
        TITLE_ELEMENTS.Clear();
        MENU.Clear();

        int width  = GameRenderer.GetScreenWidth();
        int height = GameRenderer.GetScreenHeight();

        BuildBackground(width, height);
        BuildTitle(width, height);
        BuildMenu(width, height);
    }

    private static void BuildBackground(int WIDTH, int HEIGHT)
    {
        var background = new GameInterfaceElement(0, 0, WIDTH, HEIGHT, VISIBLE: true);

        background.SetRendererConfig(GameRendererConfig.Sprite(
            "Environment/wallpaper",
            GameRenderer.CoverSource(WALLPAPER_W, WALLPAPER_H, WIDTH, HEIGHT)));

        TITLE_ELEMENTS.Add(background);
    }

    private static void BuildTitle(int WIDTH, int HEIGHT)
    {
        float titleScale = GameFonts.FitScale(GAME_TITLE, WIDTH * TITLE_WIDTH_RATIO, GameFonts.TITLE);
        Vector2 titleSize = GameFonts.Measure(GAME_TITLE, titleScale);

        var title = new GameInterfaceElement(
            (int)(WIDTH / 2f - titleSize.X / 2f),
            (int)(HEIGHT * TITLE_Y),
            (int)titleSize.X, (int)titleSize.Y, VISIBLE: true);

        title.SetRendererConfig(GameRendererConfig.Label(
            GAME_TITLE, new Color(255, 250, 215), titleScale, TEXT_SHADOW: (int)titleScale));
        TITLE_ELEMENTS.Add(title);

        float subtitleScale = System.Math.Max(1f, titleScale / 2f);
        Vector2 subtitleSize = GameFonts.Measure(SUBTITLE, subtitleScale);

        var subtitle = new GameInterfaceElement(
            (int)(WIDTH / 2f - subtitleSize.X / 2f),
            (int)(HEIGHT * SUBTITLE_Y),
            (int)subtitleSize.X, (int)subtitleSize.Y, VISIBLE: true);
        subtitle.SetRendererConfig(GameRendererConfig.Label(
            SUBTITLE, new Color(210, 240, 255), subtitleScale, TEXT_SHADOW: (int)subtitleScale));
        TITLE_ELEMENTS.Add(subtitle);
    }

    private static void BuildMenu(int WIDTH, int HEIGHT)
    {
        int y = (int)(HEIGHT * MENU_Y);

        if (PokemonDatabase.Ready)
        {
            y = AddButton("START", Start, WIDTH, y) + MENU_GAP;
        }
        else
        {
            y = BuildRosterWarning(WIDTH, HEIGHT) + MENU_GAP;
        }

        y = AddButton("OPTIONS", UI.SettingsModal.Open, WIDTH, y) + MENU_GAP;
        AddButton("QUIT", GameHost.Quit, WIDTH, y);
    }

    private static int AddButton(string TEXT, System.Action ON_CLICK, int WIDTH, int Y)
    {
        GameButton button = UIFactory.Button(TEXT, ON_CLICK, UITheme.GLASS, UITheme.TEXT, MENU_W);
        button.SetPosition(new Point(WIDTH / 2 - button.SIZE_X / 2, Y));
        MENU.Add(button);
        return Y + button.SIZE_Y;
    }

    private static void Start()
    {
        if (!PokemonDatabase.Ready) return;
        GameGlobals.STATE = GameState.GAME;
    }

    private static int BuildRosterWarning(int WIDTH, int HEIGHT)
    {
        string problem = PokemonDatabase.PROBLEM.Length > 0
            ? PokemonDatabase.PROBLEM
            : "NO POKEMON AVAILABLE";

        float scale = System.Math.Max(1f, GameFonts.MEDIUM - 1f);
        int y = (int)(HEIGHT * HINT_Y);

        Add("NO ROSTER LOADED", WIDTH, y, scale, new Color(226, 86, 72));
        y += (int)GameFonts.Measure("X", scale).Y + 12;

        foreach (string line in UIFactory.Wrap(problem, (int)(WIDTH * 0.7f), GameFonts.BODY))
        {
            Add(line, WIDTH, y, GameFonts.BODY, Color.White);
            y += (int)GameFonts.Measure("X", GameFonts.BODY).Y + 6;
        }

        y += 14;
        Add("ADD POKEMON TO Data/pokemons.xml AND RESTART", WIDTH, y, GameFonts.BODY,
            new Color(198, 206, 220));

        return y + (int)GameFonts.Measure("X", GameFonts.BODY).Y;
    }

    private static void Add(string TEXT, int WIDTH, int Y, float SCALE, Color COLOR)
    {
        Vector2 size = GameFonts.Measure(TEXT, SCALE);

        var label = new GameInterfaceElement(
            (int)(WIDTH / 2f - size.X / 2f), Y, (int)size.X, (int)size.Y, VISIBLE: true);
        label.SetRendererConfig(GameRendererConfig.Label(TEXT, COLOR, SCALE, TEXT_SHADOW: 2));
        TITLE_ELEMENTS.Add(label);
    }
}
