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
    private const int ICON_SIZE = ICON_ASSET_SIZE;
    private const int SPACING_X = 4;
    private const int SPACING_Y = 6;
    private const int START_X = 20;
    private const int START_Y = 40;
    private const int HEADER_X = 20;
    private const int HEADER_Y = 20;

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

        var header = new GameInterfaceElement(HEADER_X, HEADER_Y, 100, 100, VISIBLE: true);
        header.SetRendererConfig(GameRendererConfig.Label($"TABLE SIZE: {GameGlobals.GetTableSize()}"));
        BONUS_ELEMENTS.Add(header);

        int row = 0;
        foreach (PokemonType type in TYPES)
        {
            int count = GameBonusLogic.GetTypeBonusCount(type);
            if (count == 0) continue;

            for (int i = 0; i < count; i++)
            {
                var icon = new GameBonusElement(
                    START_X + i * (ICON_SIZE + SPACING_X),
                    START_Y + row * (ICON_SIZE + SPACING_Y),
                    ICON_SIZE, ICON_SIZE, VISIBLE: true)
                {
                    TYPE = type
                };
                icon.SetRendererConfig(GameRendererConfig.Sprite(
                    PokemonTypeAssets.IconPath(type),
                    new Rectangle(0, 0, ICON_ASSET_SIZE, ICON_ASSET_SIZE)));

                BONUS_ELEMENTS.Add(icon);
            }

            row++;
        }
    }
}
