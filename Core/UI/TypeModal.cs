using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public static class TypeModal
{
    private const int WIDTH = 780;
    private const int HEIGHT = 480;

    private const int BADGE = 128;
    private const int LINE_H = 30;

    public static void Open(PokemonType TYPE)
    {
        int count = GameBonusLogic.GetTypeBonusCount(TYPE);

        GameModal.Open(TYPE.ToString().ToUpperInvariant(), "shield", WIDTH, HEIGHT,
            (panel, width, height) => Build(panel, width, TYPE, count));
    }

    private static void Build(GameInterfaceElement PANEL, int WIDTH_PX, PokemonType TYPE, int COUNT)
    {
        int top = UIFactory.BORDER + GameModal.TITLE_H + GameModal.PAD;
        int left = GameModal.PAD;

        GameInterfaceElement badge = UIFactory.Panel(left, top, BADGE, BADGE, PANEL, UITheme.SURFACE_DEEP);

        var icon = new GameInterfaceElement(UIFactory.BORDER, UIFactory.BORDER,
            BADGE - UIFactory.BORDER * 2, BADGE - UIFactory.BORDER * 2, VISIBLE: true, PARENT: badge);
        icon.SetRendererConfig(GameRendererConfig.Sprite(PokemonTypeAssets.IconPath(TYPE), new Rectangle(0, 0, 34, 34)));
        badge.AddChild(icon);

        int column = left + BADGE + GameModal.PAD * 2;
        int span = WIDTH_PX - column - GameModal.PAD;
        int y = top;

        y = UIFactory.Section("star", "ON THE FIELD", column, y, span, PANEL);

        UIFactory.LabelBox("UNITS", new Rectangle(column, y, 140, LINE_H), PANEL, UITheme.TEXT_DIM);
        Vector2 size = GameFonts.Measure($"{COUNT}", GameFonts.MEDIUM);
        UIFactory.LabelBox($"{COUNT}", new Rectangle(column + span - (int)size.X, y - 6, (int)size.X, LINE_H + 12),
            PANEL, UITheme.ACCENT, GameFonts.MEDIUM);
        y += LINE_H + GameModal.PAD + 10;

        y = UIFactory.Section("shield", "BONUS", column, y, span, PANEL);

        foreach (string line in UIFactory.Wrap(
                     GameBonusLogic.GetBonusDescription(TYPE).Trim().ToUpperInvariant(), span, GameFonts.BODY))
        {
            UIFactory.Label(line, column, y, PANEL, UITheme.TEXT);
            y += LINE_H;
        }
    }
}
