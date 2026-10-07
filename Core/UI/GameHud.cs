using System.Collections.Generic;
using PokemonTFT.Core;
using PokemonTFT.Logic;

namespace PokemonTFT.UI;

public static class GameHud
{
    private const int MARGIN = 18;
    private const int PANEL_W = 250;
    private const int PANEL_H = 104;
    private const int ROW_H = 40;
    private const int GAP = 10;

    private static readonly List<GameElement> HUD_ELEMENTS = [];

    private static int _signature = int.MinValue;

    public static IReadOnlyList<GameElement> GetElements() => HUD_ELEMENTS;

    public static int Bottom { get; private set; } = MARGIN + PANEL_H;

    public static void Refresh()
    {
        _signature = int.MinValue;
        Update();
    }

    public static void Update()
    {
        int round = GameGlobals.LEVEL;
        int field = GameTableLogic.GetPlayerPokemonsCount();
        int limit = GameGlobals.GetTableSize();

        int signature = round * 7919 ^ field * 104729 ^ limit * 1297;
        if (signature == _signature) return;

        _signature = signature;
        Rebuild(round, field, limit);
    }

    private static void Rebuild(int ROUND, int FIELD, int LIMIT)
    {
        HUD_ELEMENTS.Clear();

        GameInterfaceElement panel = UIFactory.Panel(MARGIN, MARGIN, PANEL_W, PANEL_H);
        HUD_ELEMENTS.Add(panel);

        int right = PANEL_W - UIFactory.PAD;
        int y = UIFactory.PAD + 4;

        BuildSettingsButton();
        BuildStatsButton();

        Row("ROUND", $"{ROUND}", y, right, panel, UITheme.ACCENT);
        Row("FIELD", $"{FIELD}/{LIMIT}", y + ROW_H, right, panel,
            FIELD >= LIMIT ? UITheme.HEALTH : UITheme.TEXT);
    }

    private static void BuildSettingsButton()
    {
        GameButton button = UIFactory.IconButton("settings", SettingsModal.Open);
        button.SetPosition(new Microsoft.Xna.Framework.Point(
            GameRenderer.GetScreenWidth() - button.SIZE_X - MARGIN, MARGIN));
        HUD_ELEMENTS.Add(button);
    }

    private static void BuildStatsButton()
    {
        GameButton button = UIFactory.Button("ROUND STATS", StatsModal.Open, UITheme.GLASS, UITheme.TEXT,
            PANEL_W, ICON: "leaderboard");
        button.SetPosition(new Microsoft.Xna.Framework.Point(MARGIN, MARGIN + PANEL_H + GAP));
        HUD_ELEMENTS.Add(button);
        Bottom = MARGIN + PANEL_H + GAP + button.SIZE_Y;
    }

    private static void Row(string LABEL, string VALUE, int Y, int RIGHT,
                            GameInterfaceElement PANEL, Microsoft.Xna.Framework.Color COLOR)
    {
        UIFactory.IconLabel(LABEL == "ROUND" ? "star" : "shield", LABEL, UIFactory.PAD, Y + 6, PANEL);
        Microsoft.Xna.Framework.Vector2 size = GameFonts.Measure(VALUE, GameFonts.MEDIUM);
        UIFactory.Label(VALUE, RIGHT - (int)size.X, Y, PANEL, COLOR, GameFonts.MEDIUM);
    }
}
