using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;

namespace PokemonTFT.UI;

public static class SettingsModal
{
    private const int WIDTH = 620;
    private const int ROW_H = 64;
    private const int ACTION_H = 68;
    private const int LINE_H = 28;
    private const int TOGGLE_W = 160;
    private const int CHOICE_W = 190;

    private static bool InRun => GameGlobals.STATE == GameState.GAME;

    public static void Open()
    {
        int actions = InRun ? 3 : 1;
        int height = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD
                   + ROW_H * 4 + LINE_H * 2 + GameModal.PAD * 3 + ACTION_H * actions + LINE_H + GameModal.PAD;

        GameModal.Open("OPTIONS", "settings", WIDTH, height, Build);
    }

    private static void Build(GameInterfaceElement PANEL, int WIDTH_PX, int HEIGHT_PX)
    {
        int left = GameModal.PAD;
        int span = WIDTH_PX - GameModal.PAD * 2;
        int y = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD;

        Toggle(PANEL, left, WIDTH_PX - GameModal.PAD - TOGGLE_W, y, "music_note", "MUSIC", GameSettings.MUSIC, () =>
        {
            GameSettings.MUSIC = !GameSettings.MUSIC;
            Commit();
        });
        y += ROW_H;

        Toggle(PANEL, left, WIDTH_PX - GameModal.PAD - TOGGLE_W, y,
            GameSettings.SOUND ? "volume_up" : "volume_off", "SOUND", GameSettings.SOUND, () =>
        {
            GameSettings.SOUND = !GameSettings.SOUND;
            Commit();
        });
        y += ROW_H;

        Toggle(PANEL, left, WIDTH_PX - GameModal.PAD - TOGGLE_W, y,
            GameHost.FULLSCREEN ? "fullscreen_exit" : "fullscreen", "FULLSCREEN", GameHost.FULLSCREEN, () =>
        {
            GameHost.ToggleFullscreen();
            GameSettings.FULLSCREEN = GameHost.FULLSCREEN;
            Commit();
        });
        y += ROW_H;

        UIFactory.IconLabel("shield", "DIFFICULTY", left, y + 8, PANEL, UITheme.TEXT);
        GameButton difficulty = UIFactory.Button(GameSettings.DIFFICULTY.ToString(), CycleDifficulty,
            UITheme.ACCENT * 0.85f, UITheme.SURFACE_DEEP, CHOICE_W, PANEL);
        difficulty.SetPosition(new Point(WIDTH_PX - GameModal.PAD - CHOICE_W, y));
        PANEL.AddChild(difficulty);
        y += ROW_H;

        int interval = ShopRules.OfferInterval(GameSettings.DIFFICULTY);
        string rule = interval <= 1
            ? "ONE OF YOUR POKEMON SHOWS UP IN THE SHOP EVERY ROUND"
            : $"ONE OF YOUR POKEMON SHOWS UP IN THE SHOP AT LEAST EVERY {interval} ROUNDS";
        foreach (string line in UIFactory.Wrap(rule, span, GameFonts.BODY))
        {
            UIFactory.Label(line, left, y, PANEL, UITheme.TEXT_DIM);
            y += LINE_H;
        }

        y += GameModal.PAD;
        UIFactory.Solid(left, y, span, 2, UITheme.BORDER, PANEL);
        y += GameModal.PAD;

        if (InRun)
        {
            Action(PANEL, left, y, span, "refresh", "RESET RUN", UITheme.ACCENT, GameTableLogic.RestartRun);
            y += ACTION_H;

            Action(PANEL, left, y, span, "close", "GIVE UP", UITheme.HEALTH, () =>
            {
                GameModal.Close();
                Screens.GameOverScene.Begin();
            });
            y += ACTION_H;
        }

        Action(PANEL, left, y, span, "power_settings_new", "QUIT GAME", UITheme.TEXT_DIM, GameHost.Quit);
        y += ACTION_H;

        UIFactory.Label("ESC CLOSES THIS WINDOW", left, y, PANEL, UITheme.TEXT_DIM);
    }

    private static void CycleDifficulty()
    {
        GameSettings.DIFFICULTY = GameSettings.DIFFICULTY switch
        {
            Difficulty.EASY => Difficulty.MEDIUM,
            Difficulty.MEDIUM => Difficulty.HARD,
            _ => Difficulty.EASY
        };
        Commit();
    }

    private static void Commit()
    {
        GameSettings.ApplyAudio();
        GameSettings.Save();
        Open();
    }

    private static void Toggle(GameInterfaceElement PANEL, int LEFT, int RIGHT, int Y,
                               string ICON, string LABEL, bool ON, Action ON_CLICK)
    {
        UIFactory.IconLabel(ICON, LABEL, LEFT, Y + 8, PANEL, UITheme.TEXT);

        GameButton button = UIFactory.Button(ON ? "ON" : "OFF", ON_CLICK,
            ON ? UITheme.ACCENT * 0.85f : UITheme.GLASS,
            ON ? UITheme.SURFACE_DEEP : UITheme.TEXT_DIM,
            TOGGLE_W, PANEL, ICON: ON ? "check" : "close");

        button.SetPosition(new Point(RIGHT, Y));
        PANEL.AddChild(button);
    }

    private static void Action(GameInterfaceElement PANEL, int X, int Y, int WIDTH_PX,
                               string ICON, string LABEL, Color TINT, Action ON_CLICK)
    {
        GameButton button = UIFactory.Button(LABEL, ON_CLICK, UITheme.GLASS, TINT, WIDTH_PX, PANEL, ICON: ICON);
        button.SetPosition(new Point(X, Y));
        PANEL.AddChild(button);
    }
}
