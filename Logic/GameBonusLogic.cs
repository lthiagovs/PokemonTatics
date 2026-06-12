using ENGINE.MODELS;
using GAME.TABLE;
using System;

public static class GameBonusLogic
{
    public static int GetTypeBonusCount(PokemonType TYPE)
    {
        if (GameTable.TABLE_ELEMENTS == null) return 0;

        int count = GameTableElement.GetTableElements()
            .FindAll(p => p is PokemonEntity pi && pi.POKEMON != null && pi.POKEMON.TYPE == TYPE).Count;

        return count;
    }

    public static string GetGhostDarkDescription()
    {
        int ghostCount = GetTypeBonusCount(PokemonType.GHOST);
        int darkCount = GetTypeBonusCount(PokemonType.DARK);
        int total = ghostCount + darkCount;
        int chance = total * 5;

        return $"    SHADOW: Ghost and Dark Pokemon gain {chance}% evasion chance based on total battlefield shadow icons.    ";
    }

    public static string GetElectricDescription()
    {
        int electricCount = GetTypeBonusCount(PokemonType.ELECTRIC);
        int chance = electricCount * 15;

        return $"    STATIC: Electric attacks have a {chance}% chance to reduce the target SPEED by 15.    ";
    }

    public static string GetGrassDescription()
    {
        int count = GetTypeBonusCount(PokemonType.GRASS);
        int regen = count * 5;

        return $"    OVERGROW: Grass Pokemon regenerate {regen} HP per second.    ";
    }

    public static string GetFireDescription()
    {
        int count = GetTypeBonusCount(PokemonType.FIRE);
        int buff = count * 5;

        return $"    BLAZE: Increases ATK and SPATK of all active Pokemon by {buff}%.    ";
    }

    public static string GetWaterDescription()
    {
        int count = GetTypeBonusCount(PokemonType.WATER);
        int reduction = count * 10;

        return $"    TORRENT: Reduces the ENERGY COST to use attacks by{reduction}%.    ";
    }

    public static string GetRockSteelDescription()
    {
        int rockCount = GetTypeBonusCount(PokemonType.ROCK);
        int steelCount = GetTypeBonusCount(PokemonType.STEEL);
        int total = rockCount + steelCount;
        int buff = total * 15;

        return $"    STURDY: Increases DEF and SPDEF of all active Pokemon by {buff}.    ";
    }

    public static string GetBugDescription()
    {
        int count = GetTypeBonusCount(PokemonType.BUG);
        int speedBuff = count * 10;

        return $"    SWARM: Increases the attack SPEED of all Bug Pokemon by {speedBuff}%.    ";
    }

    public static string GetDragonDescription()
    {
        int count = GetTypeBonusCount(PokemonType.DRAGON);
        
        if (count == 1)
        {
            return "    PRESSURE: Active Dragon Pokemon gains 100% bonus to all stats because it is unique on the battlefield.    ";
        }
        
        return "    PRESSURE: Dragon Pokemon stats return to normal when more than one Dragon is on the battlefield.    ";
    }

    public static string GetNormalDescription()
    {
        int count = GetTypeBonusCount(PokemonType.NORMAL);
        int buff = count * 10;

        return $"ADAPTABILITY: Normal Pokemon gain +{buff} to ATK, DEF, and SPEED for each Normal Pokemon on the battlefield.";
    }

    public static string GetBonusDescription(PokemonType TYPE)
    {
        switch (TYPE)
        {
            case PokemonType.GHOST:
            case PokemonType.DARK:
                return GetGhostDarkDescription();
            case PokemonType.ELECTRIC:
                return GetElectricDescription();
            case PokemonType.GRASS:
                return GetGrassDescription();
            case PokemonType.FIRE:
                return GetFireDescription();
            case PokemonType.WATER:
                return GetWaterDescription();
            case PokemonType.ROCK:
            case PokemonType.STEEL:
                return GetRockSteelDescription();
            case PokemonType.BUG:
                return GetBugDescription();
            case PokemonType.DRAGON:
                return GetDragonDescription();
            case PokemonType.NORMAL:
                return GetNormalDescription();
            default:
                int count = GetTypeBonusCount(TYPE);
                return $"{TYPE.ToString().ToUpper()}: Active on field ({count}). No special bonus effect configured yet.";
        }
    }
}