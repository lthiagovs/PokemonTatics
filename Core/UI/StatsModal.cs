using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;

namespace PokemonTFT.UI;

public static class StatsModal
{
    private const int WIDTH = 1000;
    private const int HEIGHT = 900;
    private const int TAB_GAP = 10;
    private const int PODIUM_H = 300;
    private const int PODIUM_W = 230;
    private const int PODIUM_GAP = 24;
    private const int PORTRAIT_SRC = 40;
    private const int PORTRAIT = 80;
    private const int LINE_H = 30;
    private const int ROW_H = 52;
    private const int ROW_GAP = 6;
    private const int ROW_PORTRAIT = 40;
    private const int RANK_W = 52;
    private const int NAME_W = 260;
    private const int VALUE_W = 110;
    private const int BAR_H = 18;
    private const int NAV_W = 150;

    private static readonly int[] PEDESTAL = [120, 88, 64];

    private static readonly Color GOLD = new(244, 196, 64);
    private static readonly Color SILVER = new(196, 204, 214);
    private static readonly Color BRONZE = new(205, 128, 72);
    private static readonly Color HEAL = new(120, 236, 150);
    private static readonly Color VICTORY = new(140, 240, 140);
    private static readonly Color DEFEAT = new(240, 120, 120);

    private static readonly BattleMetric[] TABS =
        [BattleMetric.DAMAGE, BattleMetric.TANKED, BattleMetric.KILLS, BattleMetric.HEALING];

    private static BattleMetric _tab = BattleMetric.DAMAGE;
    private static int _page;

    public static void Open()
    {
        _page = 0;
        Show();
    }

    private static void Show() => GameModal.Open("LAST ROUND", "leaderboard", WIDTH, HEIGHT, Build);

    private static void Select(BattleMetric TAB)
    {
        _tab = TAB;
        _page = 0;
        Show();
    }

    private static void Turn(int STEP)
    {
        _page += STEP;
        Show();
    }

    private static void Build(GameInterfaceElement PANEL, int WIDTH_PX, int HEIGHT_PX)
    {
        int left = GameModal.PAD;
        int span = WIDTH_PX - GameModal.PAD * 2;
        int y = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD;

        if (!BattleStats.HasRound)
        {
            UIFactory.LabelBox("NO ROUND PLAYED YET", new Rectangle(left, y, span, HEIGHT_PX - y - GameModal.PAD),
                PANEL, UITheme.TEXT_DIM, GameFonts.MEDIUM);
            return;
        }

        List<BattleRecord> ranking = BattleStats.Ranking(_tab);

        string result = BattleStats.LAST_WON ? "VICTORY" : "DEFEAT";
        UIFactory.Label($"ROUND {BattleStats.LAST_ROUND}  {result}", left, y, PANEL,
            BattleStats.LAST_WON ? VICTORY : DEFEAT);
        string count = $"{ranking.Count} POKEMON";
        UIFactory.Label(count, left + span - (int)GameFonts.Measure(count, GameFonts.BODY).X, y, PANEL, UITheme.TEXT_DIM);
        y += LINE_H + 6;

        y = BuildTabs(PANEL, left, y, span) + GameModal.PAD;

        int max = 1;
        for (int i = 0; i < ranking.Count; i++) max = Math.Max(max, ranking[i].Value(_tab));

        BuildPodium(PANEL, left, y, span, ranking);
        y += PODIUM_H + GameModal.PAD;

        BuildList(PANEL, left, y, span, HEIGHT_PX - GameModal.PAD, ranking, max);
    }

    private static int BuildTabs(GameInterfaceElement PANEL, int X, int Y, int SPAN)
    {
        int width = (SPAN - TAB_GAP * (TABS.Length - 1)) / TABS.Length;
        int height = 0;

        for (int i = 0; i < TABS.Length; i++)
        {
            BattleMetric tab = TABS[i];
            bool active = tab == _tab;

            GameButton button = UIFactory.Button(Title(tab), () => Select(tab),
                active ? UITheme.ACCENT * 0.85f : UITheme.GLASS,
                active ? UITheme.SURFACE_DEEP : UITheme.TEXT,
                width, PANEL, ICON: Icon(tab));
            button.SetPosition(new Point(X + i * (width + TAB_GAP), Y));
            PANEL.AddChild(button);
            height = button.SIZE_Y;
        }

        return Y + height;
    }

    private static void BuildPodium(GameInterfaceElement PANEL, int X, int Y, int SPAN, List<BattleRecord> RANKING)
    {
        int[] order = [1, 0, 2];
        int total = PODIUM_W * 3 + PODIUM_GAP * 2;
        int start = X + (SPAN - total) / 2;
        int floor = Y + PODIUM_H;

        UIFactory.Solid(X, floor - 2, SPAN, 2, UITheme.BORDER, PANEL);

        for (int slot = 0; slot < order.Length; slot++)
        {
            int rank = order[slot];
            if (rank >= RANKING.Count) continue;

            BattleRecord record = RANKING[rank];
            int columnX = start + slot * (PODIUM_W + PODIUM_GAP);
            int pedestal = PEDESTAL[rank];
            int pedestalY = floor - pedestal;
            Color medal = rank == 0 ? GOLD : rank == 1 ? SILVER : BRONZE;

            GameInterfaceElement block = UIFactory.Panel(columnX, pedestalY, PODIUM_W, pedestal, PANEL, UITheme.SURFACE_DEEP);
            UIFactory.Solid(UIFactory.BORDER, UIFactory.BORDER, PODIUM_W - UIFactory.BORDER * 2, 4, medal, block);
            UIFactory.LabelBox($"{rank + 1}", new Rectangle(0, 6, PODIUM_W, pedestal - 6), block, medal, GameFonts.LARGE);
            if (rank == 0) UIFactory.Icon("emoji_events", 30, pedestal / 2 + 3, UIIcons.LARGE, block, GOLD);

            int valueY = pedestalY - LINE_H - 4;
            UIFactory.LabelBox(Format(record.Value(_tab)), new Rectangle(columnX, valueY, PODIUM_W, LINE_H), PANEL,
                Accent(_tab), GameFonts.MEDIUM);

            int nameY = valueY - LINE_H;
            string name = record.NAME.ToUpperInvariant();
            float scale = GameFonts.Measure(name, GameFonts.BODY).X <= PODIUM_W - 8 ? GameFonts.BODY : GameFonts.SMALL;
            UIFactory.LabelBox(name, new Rectangle(columnX, nameY, PODIUM_W, LINE_H), PANEL,
                record.SURVIVED ? UITheme.TEXT : UITheme.TEXT_DIM, scale);

            int portraitY = nameY - PORTRAIT - 10;
            Portrait(PANEL, record, columnX + (PODIUM_W - PORTRAIT) / 2, portraitY, PORTRAIT, medal);
        }
    }

    private static void BuildList(GameInterfaceElement PANEL, int X, int Y, int SPAN, int BOTTOM,
                                  List<BattleRecord> RANKING, int MAX)
    {
        int rest = RANKING.Count - 3;
        if (rest <= 0) return;

        int navigation = ROW_H;
        int perPage = Math.Max(1, (BOTTOM - Y - navigation - GameModal.PAD + ROW_GAP) / (ROW_H + ROW_GAP));
        int pages = (rest + perPage - 1) / perPage;
        _page = Math.Clamp(_page, 0, pages - 1);

        int first = 3 + _page * perPage;
        int last = Math.Min(RANKING.Count, first + perPage);

        for (int index = first; index < last; index++)
        {
            Row(PANEL, X, Y, SPAN, index + 1, RANKING[index], MAX);
            Y += ROW_H + ROW_GAP;
        }

        if (pages <= 1) return;

        int navY = Math.Max(BOTTOM - navigation, Y);
        Nav(PANEL, X, navY, "PREV", _page > 0, () => Turn(-1));
        Nav(PANEL, X + SPAN - NAV_W, navY, "NEXT", _page < pages - 1, () => Turn(1));
        UIFactory.LabelBox($"PAGE {_page + 1}/{pages}", new Rectangle(X, navY, SPAN, navigation), PANEL, UITheme.TEXT_DIM);
    }

    private static void Row(GameInterfaceElement PANEL, int X, int Y, int SPAN, int RANK, BattleRecord RECORD, int MAX)
    {
        GameInterfaceElement row = UIFactory.Panel(X, Y, SPAN, ROW_H, PANEL, UITheme.SURFACE_DEEP);

        UIFactory.LabelBox($"{RANK}", new Rectangle(0, 0, RANK_W, ROW_H), row, UITheme.TEXT_DIM);

        int portraitX = RANK_W;
        Portrait(row, RECORD, portraitX, (ROW_H - ROW_PORTRAIT) / 2, ROW_PORTRAIT, UITheme.BORDER);

        int nameX = portraitX + ROW_PORTRAIT + 12;
        Vector2 nameSize = GameFonts.Measure(Name(RECORD), GameFonts.BODY);
        UIFactory.Label(Name(RECORD), nameX, (ROW_H - (int)nameSize.Y) / 2, row,
            RECORD.SURVIVED ? UITheme.TEXT : UITheme.TEXT_DIM);

        int barX = nameX + Math.Max(NAME_W, (int)nameSize.X + 16);
        int barW = SPAN - barX - VALUE_W - UIFactory.PAD;
        if (barW > 20)
        {
            UIFactory.Bar(barX, (ROW_H - BAR_H) / 2, barW, BAR_H, Accent(_tab), row, out GameInterfaceElement fill);
            fill.SIZE_X = UIFactory.BarFillWidth(barW, RECORD.Value(_tab), MAX);
        }

        UIFactory.LabelBox(Format(RECORD.Value(_tab)), new Rectangle(SPAN - VALUE_W - UIFactory.PAD, 0, VALUE_W, ROW_H),
            row, Accent(_tab));
    }

    private static void Nav(GameInterfaceElement PANEL, int X, int Y, string LABEL, bool ENABLED, Action ON_CLICK)
    {
        GameButton button = UIFactory.Button(LABEL, ENABLED ? ON_CLICK : () => { },
            UITheme.GLASS, ENABLED ? UITheme.TEXT : UITheme.TEXT_DIM, NAV_W, PANEL);
        button.SetPosition(new Point(X, Y));
        button.ENABLED = ENABLED;
        PANEL.AddChild(button);
    }

    private static void Portrait(GameInterfaceElement PARENT, BattleRecord RECORD, int X, int Y, int SIZE, Color FRAME)
    {
        GameInterfaceElement frame = UIFactory.Panel(X - UIFactory.BORDER, Y - UIFactory.BORDER,
            SIZE + UIFactory.BORDER * 2, SIZE + UIFactory.BORDER * 2, PARENT, UITheme.TRACK);
        frame.SetRendererConfig(GameRendererConfig.Solid(FRAME));

        var art = new GameInterfaceElement(UIFactory.BORDER, UIFactory.BORDER, SIZE, SIZE, VISIBLE: true, PARENT: frame);
        art.SetRendererConfig(GameRendererConfig.Sprite(RECORD.PORTRAIT, new Rectangle(0, 0, PORTRAIT_SRC, PORTRAIT_SRC),
            RECORD.SURVIVED ? Color.White : new Color(150, 150, 150)));
        frame.AddChild(art);
    }

    private static string Name(BattleRecord RECORD) => $"{RECORD.NAME.ToUpperInvariant()}  LV {RECORD.LEVEL}";

    private static string Format(int VALUE) => VALUE.ToString("N0", CultureInfo.InvariantCulture);

    private static string Title(BattleMetric TAB) => TAB switch
    {
        BattleMetric.TANKED  => "TANKED",
        BattleMetric.KILLS   => "KILLS",
        BattleMetric.HEALING => "HEALING",
        _                    => "DAMAGE"
    };

    private static string Icon(BattleMetric TAB) => TAB switch
    {
        BattleMetric.TANKED  => "shield",
        BattleMetric.KILLS   => "star",
        BattleMetric.HEALING => "favorite",
        _                    => "bolt"
    };

    private static Color Accent(BattleMetric TAB) => TAB switch
    {
        BattleMetric.TANKED  => UITheme.MANA,
        BattleMetric.KILLS   => UITheme.HEALTH,
        BattleMetric.HEALING => HEAL,
        _                    => UITheme.ACCENT
    };
}
