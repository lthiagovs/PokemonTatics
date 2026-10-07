using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public static class PokemonModal
{
    private const int WIDTH = 1040;
    private const int HEIGHT = 900;

    private const int ART = 250;
    private const int ROW_H = 40;
    private const int STAT_H = 40;
    private const int BAR_H = 24;
    private const int STAT_MAX = 180;
    private const int LABEL_W = 110;
    private const int VALUE_W = 76;
    private const int LINE_H = 28;
    private const int ITEM_GAP = 12;

    public static bool CanOpen => !GameMouse.HasCarry() && ItemLogic.Carried == null;

    public static void Open(Pokemon POKEMON)
    {
        GameModal.Open(POKEMON.NAME.ToUpperInvariant(), "info", WIDTH, HEIGHT,
            (panel, width, height) => Build(panel, width, POKEMON), ESCAPE_CLOSES: false);
    }

    private static void Build(GameInterfaceElement PANEL, int WIDTH_PX, Pokemon POKEMON)
    {
        int top = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD;
        int left = GameModal.PAD;

        BuildArt(PANEL, left, top, POKEMON);

        int column = left + ART + GameModal.PAD * 2;
        int span = WIDTH_PX - column - GameModal.PAD;
        int y = top;

        y = UIFactory.Section("info", "PROFILE", column, y, span, PANEL);
        y = BuildProfile(PANEL, column, y, span, POKEMON);

        y += GameModal.PAD;
        y = UIFactory.Section("shield", "STATS", column, y, span, PANEL);
        y = BuildStats(PANEL, column, y, span, POKEMON);

        y += GameModal.PAD;
        y = UIFactory.Section("bolt", "BATTLE", column, y, span, PANEL);
        y = BuildBattle(PANEL, column, y, span, POKEMON);

        foreach (ItemCombo combo in ItemLogic.Combos(POKEMON))
        {
            y += 6;
            UIFactory.IconLabel("star", $"{ItemLogic.Name(combo)}  {ItemLogic.Describe(combo)}",
                column, y, PANEL, UITheme.ACCENT);
            y += LINE_H;
        }
    }

    private static void BuildArt(GameInterfaceElement PANEL, int X, int Y, Pokemon POKEMON)
    {
        GameInterfaceElement art = UIFactory.Panel(X, Y, ART, ART, PANEL, UITheme.SURFACE_DEEP);

        var portrait = new GameInterfaceElement(UIFactory.BORDER, UIFactory.BORDER,
            ART - UIFactory.BORDER * 2, ART - UIFactory.BORDER * 2, VISIBLE: true, PARENT: art);
        portrait.SetRendererConfig(GameRendererConfig.Sprite(POKEMON.PortraitPath, new Rectangle(0, 0, 40, 40)));
        art.AddChild(portrait);

        int stageY = Y + ART + GameModal.PAD;
        GameInterfaceElement stage = UIFactory.Panel(X, stageY, ART, ART, PANEL, UITheme.TRACK);

        int slice = POKEMON.SPRITE_SLICE > 0 ? POKEMON.SPRITE_SLICE : 64;
        int view = ART - UIFactory.BORDER * 2;
        var sprite = new GameInterfaceElement(UIFactory.BORDER, UIFactory.BORDER, view, view,
            VISIBLE: true, PARENT: stage);
        sprite.SetRendererConfig(GameRendererConfig.Sprite(POKEMON.MovesetPath, new Rectangle(0, 0, slice, slice)));
        stage.AddChild(sprite);

        int itemsY = stageY + ART + GameModal.PAD;
        int slotsY = BuildItemSlots(PANEL, X, itemsY, POKEMON);

        string evolution = POKEMON.EVOLUTION == null
            ? "FINAL FORM"
            : $"EVOLVES AT LV {POKEMON.EVOLUTION_LEVEL}";

        UIFactory.LabelBox(evolution, new Rectangle(X, slotsY + 10, ART, LINE_H), PANEL,
            POKEMON.EVOLUTION == null ? UITheme.TEXT_DIM : UITheme.ACCENT);

        if (POKEMON.EVOLUTION != null)
        {
            UIFactory.LabelBox(POKEMON.NextName.ToUpperInvariant(),
                new Rectangle(X, slotsY + 10 + LINE_H, ART, LINE_H), PANEL, UITheme.TEXT);
        }
    }

    private static int BuildItemSlots(GameInterfaceElement PANEL, int X, int Y, Pokemon POKEMON)
    {
        int max = Balance.MAX_ITEMS_PER_POKEMON;
        int slot = (ART - ITEM_GAP * (max - 1)) / max;

        UIFactory.IconLabel("star", $"ITEMS {Math.Min(POKEMON.ITEMS.Count, max)}/{max}", X, Y, PANEL,
            UITheme.TEXT_DIM);

        int row = Y + LINE_H + 6;

        for (int i = 0; i < max; i++)
        {
            int x = X + i * (slot + ITEM_GAP);
            Item? item = i < POKEMON.ITEMS.Count ? Data.ItemDatabase.Find(POKEMON.ITEMS[i]) : null;

            if (item == null)
            {
                UIFactory.Solid(x, row, slot, slot, UITheme.BORDER * 0.6f, PANEL);
                UIFactory.Solid(x + UIFactory.BORDER, row + UIFactory.BORDER,
                    slot - UIFactory.BORDER * 2, slot - UIFactory.BORDER * 2, UITheme.TRACK, PANEL);
                continue;
            }

            Item captured = item;
            var button = new GameButton(x, row, slot, slot, VISIBLE: true, PARENT: PANEL)
            {
                HOVERABLE = true,
                IDLE_BOB = 0f,
                HOVER_SCALE = 1.06f,
                ON_CLICK = () => ItemModal.Open(captured)
            };
            button.SetRendererConfig(GameRendererConfig.Solid(UITheme.ACCENT));
            UIFactory.Solid(UIFactory.BORDER, UIFactory.BORDER, slot - UIFactory.BORDER * 2,
                slot - UIFactory.BORDER * 2, UITheme.SURFACE_DEEP, button);

            var icon = new GameInterfaceElement((slot - item.ICON.Width) / 2, (slot - item.ICON.Height) / 2,
                item.ICON.Width, item.ICON.Height, VISIBLE: true, PARENT: button);
            icon.SetRendererConfig(GameRendererConfig.Sprite(Data.ItemDatabase.TEXTURE, item.ICON));
            button.AddChild(icon);

            PANEL.AddChild(button);
        }

        return row + slot;
    }

    private static int BuildProfile(GameInterfaceElement PANEL, int X, int Y, int SPAN, Pokemon POKEMON)
    {
        int half = SPAN / 2;

        Row(PANEL, X, Y, half - GameModal.PAD, "TYPE", POKEMON.TYPE.ToString(), UITheme.ACCENT);
        Row(PANEL, X + half, Y, half, "STYLE", POKEMON.GetStyle().ToString().Replace("_", " "), UITheme.TEXT);
        Y += ROW_H;

        Row(PANEL, X, Y, half - GameModal.PAD, "LEVEL", $"{POKEMON.LEVEL}", UITheme.TEXT);
        Row(PANEL, X + half, Y, half, "XP", $"{POKEMON.XP}/{POKEMON.XPToNextLevel()}", UITheme.TEXT_DIM);
        Y += ROW_H;

        Row(PANEL, X, Y, half - GameModal.PAD, "COST", $"{POKEMON.COST}", UITheme.ACCENT);
        Row(PANEL, X + half, Y, half, "POWER", $"{POKEMON.BaseStatTotal}", UITheme.TEXT_DIM);

        return Y + ROW_H;
    }

    private static int BuildStats(GameInterfaceElement PANEL, int X, int Y, int SPAN, Pokemon POKEMON)
    {
        Stat(PANEL, X, Y, SPAN, "HP", POKEMON.MAX_HP, UITheme.HEALTH);
        Stat(PANEL, X, Y + STAT_H, SPAN, "ATK", POKEMON.ATK, UITheme.ACCENT);
        Stat(PANEL, X, Y + STAT_H * 2, SPAN, "DEF", POKEMON.DEF, UITheme.MANA);
        Stat(PANEL, X, Y + STAT_H * 3, SPAN, "SP.ATK", POKEMON.SPATK, UITheme.ACCENT);
        Stat(PANEL, X, Y + STAT_H * 4, SPAN, "SP.DEF", POKEMON.SPDEF, UITheme.MANA);
        Stat(PANEL, X, Y + STAT_H * 5, SPAN, "SPEED", POKEMON.SPEED, UITheme.EDGE);

        return Y + STAT_H * 6;
    }

    private static int BuildBattle(GameInterfaceElement PANEL, int X, int Y, int SPAN, Pokemon POKEMON)
    {
        PokemonStyle style = POKEMON.GetStyle();

        UIFactory.Label(BattleTactics.Name(style), X, Y, PANEL, UITheme.ACCENT);
        Y += LINE_H;

        Y = Paragraph(PANEL, BattleTactics.Describe(style), X, Y, SPAN, UITheme.TEXT);
        Y += 8;
        Y = Paragraph(PANEL, "Special: " + StyleBehaviour.Describe(style), X, Y, SPAN, UITheme.TEXT);
        Y += 8;

        return Paragraph(PANEL, TypeEffects.Describe(POKEMON.TYPE), X, Y, SPAN, UITheme.TEXT_DIM);
    }

    private static int Paragraph(GameInterfaceElement PANEL, string TEXT, int X, int Y, int SPAN, Color COLOR)
    {
        foreach (string line in UIFactory.Wrap(TEXT.ToUpperInvariant(), SPAN, GameFonts.BODY))
        {
            UIFactory.Label(line, X, Y, PANEL, COLOR);
            Y += LINE_H;
        }

        return Y;
    }

    private static void Row(GameInterfaceElement PANEL, int X, int Y, int SPAN,
                            string LABEL, string VALUE, Color COLOR)
    {
        UIFactory.LabelBox(LABEL, new Rectangle(X, Y, LABEL_W, ROW_H), PANEL, UITheme.TEXT_DIM);

        Vector2 size = GameFonts.Measure(VALUE, GameFonts.BODY);
        UIFactory.LabelBox(VALUE, new Rectangle(X + SPAN - (int)size.X, Y, (int)size.X, ROW_H), PANEL, COLOR);
    }

    private static void Stat(GameInterfaceElement PANEL, int X, int Y, int SPAN,
                             string LABEL, int VALUE, Color COLOR)
    {
        UIFactory.LabelBox(LABEL, new Rectangle(X, Y, LABEL_W, STAT_H), PANEL, UITheme.TEXT_DIM);

        int barW = SPAN - LABEL_W - VALUE_W;
        UIFactory.Bar(X + LABEL_W, Y + (STAT_H - BAR_H) / 2, barW, BAR_H, COLOR, PANEL,
            out GameInterfaceElement fill);
        fill.SIZE_X = UIFactory.BarFillWidth(barW, Math.Min(VALUE, STAT_MAX), STAT_MAX);

        string text = VALUE.ToString(CultureInfo.InvariantCulture);
        UIFactory.LabelBox(text, new Rectangle(X + SPAN - VALUE_W, Y, VALUE_W, STAT_H), PANEL, UITheme.TEXT);
    }
}
