using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using Microsoft.Xna.Framework;

namespace PokemonTFT.UI;

public static class ItemModal
{
    private const int WIDTH = 760;
    private const int HEIGHT = 440;

    private const int BADGE = 150;
    private const int LINE_H = 30;

    public static void Inspect(object OWNER, Item ITEM)
    {
        GameModal.ShowTransient(OWNER, ITEM.ID.GetHashCode(), ITEM.NAME, "star", WIDTH, HEIGHT,
            (panel, width, height) => Build(panel, width, ITEM, DROPPED: false));
    }

    public static void Open(Item ITEM)
    {
        GameModal.Open(ITEM.NAME, "star", WIDTH, HEIGHT,
            (panel, width, height) => Build(panel, width, ITEM, DROPPED: false));
    }

    public static void Announce(Item ITEM)
    {
        GameModal.Open("ITEM FOUND", "star", WIDTH, HEIGHT,
            (panel, width, height) => Build(panel, width, ITEM, DROPPED: true));
    }

    private static void Build(GameInterfaceElement PANEL, int WIDTH_PX, Item ITEM, bool DROPPED)
    {
        int top = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD;
        int left = GameModal.PAD;

        GameInterfaceElement badge = UIFactory.Panel(left, top, BADGE, BADGE, PANEL, UITheme.SURFACE_DEEP);

        var icon = new GameInterfaceElement(
            (BADGE - ITEM.ICON.Width) / 2, (BADGE - ITEM.ICON.Height) / 2,
            ITEM.ICON.Width, ITEM.ICON.Height, VISIBLE: true, PARENT: badge);
        icon.SetRendererConfig(GameRendererConfig.Sprite(ItemDatabase.TEXTURE, ITEM.ICON));
        badge.AddChild(icon);

        int column = left + BADGE + GameModal.PAD * 2;
        int span = WIDTH_PX - column - GameModal.PAD;
        int y = top;

        if (DROPPED)
        {
            UIFactory.Label(ITEM.NAME, column, y, PANEL, UITheme.ACCENT, GameFonts.MEDIUM);
            y += 48;
        }

        y = UIFactory.Section("bolt", "EFFECT", column, y, span, PANEL);

        foreach (string line in UIFactory.Wrap(ITEM.Describe(), span, GameFonts.BODY))
        {
            UIFactory.Label(line, column, y, PANEL, UITheme.TEXT);
            y += LINE_H;
        }

        y += GameModal.PAD;
        y = UIFactory.Section("shield", "COMBOS", column, y, span, PANEL);

        foreach (ItemCombo combo in PairsFor(ITEM))
        {
            UIFactory.Label(ItemLogic.Name(combo), column, y, PANEL, UITheme.ACCENT);
            y += LINE_H;

            foreach (string line in UIFactory.Wrap(ItemLogic.Describe(combo), span, GameFonts.SMALL))
            {
                UIFactory.Label(line, column + 16, y, PANEL, UITheme.TEXT_DIM, GameFonts.SMALL);
                y += 20;
            }

            y += 6;
        }

        if (!DROPPED) return;

        UIFactory.Label("DRAG IT FROM THE BAG ONTO A POKEMON", left, HEIGHT - 70, PANEL, UITheme.TEXT_DIM);
    }

    private static System.Collections.Generic.List<ItemCombo> PairsFor(Item ITEM)
    {
        var pairs = new System.Collections.Generic.List<ItemCombo>();

        switch (ITEM.EFFECT)
        {
            case ItemEffect.BURN_HIT:
            case ItemEffect.POISON_HIT:
                pairs.Add(ItemCombo.PLAGUE);
                break;

            case ItemEffect.ATTACK:
            case ItemEffect.SPEED:
                pairs.Add(ItemCombo.FRENZY);
                break;

            case ItemEffect.DEFENCE:
            case ItemEffect.HEALTH:
                pairs.Add(ItemCombo.BULWARK);
                break;

            case ItemEffect.GIANT:
            case ItemEffect.SPLASH:
                pairs.Add(ItemCombo.CATACLYSM);
                break;

            case ItemEffect.LIFESTEAL:
            case ItemEffect.SPECIAL_POWER:
                pairs.Add(ItemCombo.SIPHON);
                break;
        }

        return pairs;
    }
}
