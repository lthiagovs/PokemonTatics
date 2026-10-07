using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
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
    private const float BUTTON_Y   = 0.52f;
    private const float HINT_Y     = 0.62f;

    private const float TITLE_WIDTH_RATIO = 0.42f;

    private const int PLAY_SRC_W = 30;
    private const int PLAY_SRC_H = 32;
    private const int PLAY_SCALE = 3;

    private static readonly List<GameElement> TITLE_ELEMENTS = [];

    public static IReadOnlyList<GameElement> GetElements() => TITLE_ELEMENTS;

    public static void Initialize()
    {
        TITLE_ELEMENTS.Clear();

        int width  = GameRenderer.GetScreenWidth();
        int height = GameRenderer.GetScreenHeight();

        BuildBackground(width, height);
        BuildTitle(width, height);
        BuildPlayButton(width, height);
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

    private static void BuildPlayButton(int WIDTH, int HEIGHT)
    {
        int buttonW = PLAY_SRC_W * PLAY_SCALE;
        int buttonH = PLAY_SRC_H * PLAY_SCALE;

        var play = new GameButton(
            WIDTH / 2 - buttonW / 2,
            (int)(HEIGHT * BUTTON_Y) - buttonH / 2,
            buttonW, buttonH, VISIBLE: true)
        {
            HOVERABLE = true,
            ON_CLICK  = () => GameGlobals.STATE = GameState.GAME
        };
        play.SetRendererConfig(GameRendererConfig.Sprite(
            "UI/Icons/play_button", new Rectangle(0, 0, PLAY_SRC_W, PLAY_SRC_H)));
        TITLE_ELEMENTS.Add(play);

        const string hint = "CLICK TO START";
        float hintScale = System.Math.Max(1f, GameFonts.MEDIUM - 1f);
        Vector2 hintSize = GameFonts.Measure(hint, hintScale);

        var label = new GameInterfaceElement(
            (int)(WIDTH / 2f - hintSize.X / 2f),
            (int)(HEIGHT * HINT_Y),
            (int)hintSize.X, (int)hintSize.Y, VISIBLE: true);
        label.SetRendererConfig(GameRendererConfig.Label(hint, Color.White, hintScale, TEXT_SHADOW: 2));
        TITLE_ELEMENTS.Add(label);
    }
}
