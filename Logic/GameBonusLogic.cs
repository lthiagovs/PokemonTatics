using System;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class GameBonusLogic
{
    public static TypeBonusSnapshot SNAPSHOT { get; internal set; } = TypeBonusSnapshot.EMPTY;

    public static int GetTypeBonusCount(PokemonType TYPE) => SNAPSHOT.Count(TYPE);

    public static string GetBonusDescription(PokemonType TYPE) => TYPE switch
    {
        PokemonType.GHOST or PokemonType.DARK => ShadowDescription(),
        PokemonType.ELECTRIC                  => ElectricDescription(),
        PokemonType.GRASS                     => GrassDescription(),
        PokemonType.FIRE                      => FireDescription(),
        PokemonType.WATER                     => WaterDescription(),
        PokemonType.ROCK or PokemonType.STEEL => SturdyDescription(),
        PokemonType.BUG                       => BugDescription(),
        PokemonType.DRAGON                    => DragonDescription(),
        PokemonType.NORMAL                    => NormalDescription(),
        _                                     => DefaultDescription(TYPE)
    };

    private static string ShadowDescription()
    {
        int total  = GetTypeBonusCount(PokemonType.GHOST) + GetTypeBonusCount(PokemonType.DARK);
        int chance = Math.Min(total * 5, 40);
        return $"    SHADOW: Ghost and Dark Pokemon have a {chance}% chance to fully evade a hit (cap 40%).    ";
    }

    private static string ElectricDescription()
    {
        int chance = Math.Min(GetTypeBonusCount(PokemonType.ELECTRIC) * 15, 45);
        return $"    STATIC: Electric attacks have a {chance}% chance to reduce the target SPEED by 15 (cap 45%).    ";
    }

    private static string GrassDescription()
    {
        int regen = GetTypeBonusCount(PokemonType.GRASS) * 5;
        return $"    OVERGROW: Grass Pokemon regenerate up to {regen} HP per second while attacking (cap 2% max HP).    ";
    }

    private static string FireDescription()
    {
        int buff = GetTypeBonusCount(PokemonType.FIRE) * 5;
        return $"    BLAZE: Increases ATK and SPATK of all active Pokemon by {buff}%.    ";
    }

    private static string WaterDescription()
    {
        int reduction = Math.Min(GetTypeBonusCount(PokemonType.WATER) * 10, 50);
        return $"    TORRENT: Reduces the ENERGY needed for the special attack by {reduction}% (cap 50%).    ";
    }

    private static string SturdyDescription()
    {
        int total = GetTypeBonusCount(PokemonType.ROCK) + GetTypeBonusCount(PokemonType.STEEL);
        int buff  = Math.Min(total * 8, 64);
        return $"    STURDY: Increases DEF and SPDEF of all active Pokemon by {buff}% (cap 64%).    ";
    }

    private static string BugDescription()
    {
        int count = GetTypeBonusCount(PokemonType.BUG);
        return $"    SWARM: Active on field ({count}). No effect implemented yet.    ";
    }

    private static string DragonDescription()
        => GetTypeBonusCount(PokemonType.DRAGON) == 1
            ? "    PRESSURE: The lone Dragon on the battlefield deals double ATK and SPATK.    "
            : "    PRESSURE: Only works with exactly one Dragon on the battlefield.    ";

    private static string NormalDescription()
    {
        int buff = GetTypeBonusCount(PokemonType.NORMAL) * 8;
        return $"    ADAPTABILITY: Normal Pokemon gain +{buff} ATK when attacking and +{buff} DEF when defending.    ";
    }

    private static string DefaultDescription(PokemonType TYPE)
        => $"    {TYPE.ToString().ToUpperInvariant()}: Active on field ({GetTypeBonusCount(TYPE)}). No bonus configured yet.    ";
}
