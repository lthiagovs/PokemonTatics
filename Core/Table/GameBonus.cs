using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.UI;

namespace PokemonTFT.Table;

public static class GameBonus
{
    private const int ICON_ASSET_SIZE = 34;
    private const int ICON_SIZE = 40;
    private const int SPACING_Y = 8;
    private const int MARGIN = 18;
    private const int CHIP_H = 48;
    private const int CHIP_PAD = 10;
    private const int TOP_RIGHT_RESERVE = 70;

    private static readonly List<GameElement> BONUS_ELEMENTS = [];
    private static readonly PokemonType[] TYPES = Enum.GetValues<PokemonType>();

    private static int _lastSignature = int.MinValue;

    public static IReadOnlyList<GameElement> GetBonusElements() => BONUS_ELEMENTS;

    public static void Refresh()
    {
        _lastSignature = int.MinValue;
        Update();
    }

    public static void Update()
    {
        int signature = GameBonusLogic.SNAPSHOT.Signature();
        if (signature == _lastSignature) return;

        _lastSignature = signature;
        Rebuild();
    }

    private static void Rebuild()
    {
        BONUS_ELEMENTS.Clear();

        int right = GameRenderer.GetScreenWidth() - MARGIN;
        int y = MARGIN + TOP_RIGHT_RESERVE;

        foreach (PokemonType type in TYPES)
        {
            int count = GameBonusLogic.GetTypeBonusCount(type);
            if (count == 0) continue;

            PokemonType captured = type;
            bool active = GameBonusLogic.IsActive(type);
            string label = $"x{count}";
            Vector2 size = GameFonts.Measure(label, GameFonts.BODY);
            int width = CHIP_PAD + ICON_SIZE + 8 + (int)size.X + CHIP_PAD;

            var chip = new TypeChipElement(right - width, y, width, CHIP_H, VISIBLE: true)
            {
                HOVERABLE = true,
                IDLE_BOB = 0f,
                HOVER_SCALE = 1.04f,
                TYPE = captured,
                ON_CLICK = () => TypeModal.Open(captured)
            };
            chip.SetRendererConfig(GameRendererConfig.Solid(active ? UITheme.ACCENT : UITheme.BORDER));

            var glass = new GameInterfaceElement(UIFactory.BORDER, UIFactory.BORDER,
                width - UIFactory.BORDER * 2, CHIP_H - UIFactory.BORDER * 2, VISIBLE: true, PARENT: chip);
            glass.SetRendererConfig(GameRendererConfig.Glass(UITheme.GLASS));
            chip.AddChild(glass);

            var icon = new GameInterfaceElement(CHIP_PAD, (CHIP_H - ICON_SIZE) / 2,
                ICON_SIZE, ICON_SIZE, VISIBLE: true, PARENT: chip);
            icon.SetRendererConfig(GameRendererConfig.Sprite(
                PokemonTypeAssets.IconPath(type), new Rectangle(0, 0, ICON_ASSET_SIZE, ICON_ASSET_SIZE),
                active ? Color.White : Color.White * 0.45f));
            chip.AddChild(icon);

            UIFactory.LabelBox(label,
                new Rectangle(CHIP_PAD + ICON_SIZE + 8, 0, (int)size.X, CHIP_H), chip,
                active ? UITheme.ACCENT : UITheme.TEXT_DIM);

            BONUS_ELEMENTS.Add(chip);
            y += CHIP_H + SPACING_Y;
        }
    }
}
