using System;
using System.Collections.Generic;
using ENGINE.MODELS;
using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.TABLE;

public static class GameBonus
{
    private static List<GameElement> BONUS_ELEMENTS = new List<GameElement>();
    public static List<GameElement> GetBonusElements() => BONUS_ELEMENTS;

    private const int ICON_ASSET_SIZE = 34;
    private const int SCALE = 1;
    private const int ICON_SIZE = ICON_ASSET_SIZE * SCALE;
    private const int SPACING_X = 4;
    private const int SPACING_Y = 6;
    private const int START_X = 20;
    private const int START_Y = 40;
    private static int _lastPokemonCount = -1;

    public static void Initialize()
    {
        BONUS_ELEMENTS.Clear();

        int currentRow = 0;

        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            int count = GameBonusLogic.GetTypeBonusCount(type);

            if (count == 0) continue;

            for (int i = 0; i < count; i++)
            {
                int posX = START_X + i * (ICON_SIZE + SPACING_X);
                int posY = START_Y + currentRow * (ICON_SIZE + SPACING_Y);

                string assetName = type.ToString().ToLower();

                var typeIcon = new GameBonusElement(
                    (short)posX, (short)posY, 
                    (short)ICON_SIZE, (short)ICON_SIZE, true
                );

                typeIcon.TYPE = type;

                typeIcon.SetRendererConfig(new GameRendererConfig(
                    Color.White, null, "UI/Types/" + assetName, 
                    new Rectangle(0, 0, ICON_ASSET_SIZE, ICON_ASSET_SIZE), false
                ));
                
                typeIcon.CONFIG = new GameInterfaceConfig(false, false);

                typeIcon.HINT = new GameHint(""); 

                BONUS_ELEMENTS.Add(typeIcon);
            }

            currentRow++;
        }
    }

    public static void Update()
    {
        if (GameTable.TABLE_ELEMENTS == null) return;

        int currentPokemonCount = 0;

        for (int i = 0; i < GameTable.TABLE_ELEMENTS.Count; i++)
        {
            if (GameTable.TABLE_ELEMENTS[i] is GameTableElement tElement && tElement.HasPokemon())
            {
                currentPokemonCount++;
            }
        }

        if (currentPokemonCount != _lastPokemonCount)
        {
            _lastPokemonCount = currentPokemonCount;
            Initialize();
        }
    }
}