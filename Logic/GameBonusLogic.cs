using System;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class GameBonusLogic
{
    private readonly record struct Rule(float STEP, float CAP, PokemonType? PARTNER = null);

    public const int GRASS_REGEN_CAP_PERCENT = 2;
    public const int ICE_SPEED_CUT = 20;
    public const int ELECTRIC_SPEED_CUT = 15;

    public static TypeBonusSnapshot SNAPSHOT { get; internal set; } = TypeBonusSnapshot.EMPTY;

    private static Rule RuleOf(PokemonType TYPE) => TYPE switch
    {
        PokemonType.FIRE     => new(0.04f, 0.24f),
        PokemonType.FIGHT    => new(0.05f, 0.25f),
        PokemonType.PSYCHIC  => new(0.06f, 0.30f),
        PokemonType.DRAGON   => new(0.09f, 0.27f),
        PokemonType.POISON   => new(0.04f, 0.18f),
        PokemonType.WATER    => new(0.08f, 0.40f),
        PokemonType.FAIRY    => new(0.05f, 0.25f),
        PokemonType.GROUND   => new(0.05f, 0.25f),
        PokemonType.ICE      => new(0.08f, 0.32f),
        PokemonType.ELECTRIC => new(0.10f, 0.40f),
        PokemonType.BUG      => new(0.05f, 0.30f),
        PokemonType.FLY      => new(0.03f, 0.15f),
        PokemonType.ROCK     => new(0.05f, 0.30f, PokemonType.STEEL),
        PokemonType.STEEL    => new(0.05f, 0.30f, PokemonType.ROCK),
        PokemonType.GHOST    => new(0.03f, 0.21f, PokemonType.DARK),
        PokemonType.DARK     => new(0.03f, 0.21f, PokemonType.GHOST),
        PokemonType.NORMAL   => new(5f, float.MaxValue),
        PokemonType.GRASS    => new(4f, float.MaxValue),
        _                    => new(0f, 0f)
    };

    public static int Units(TypeBonusSnapshot BONUS, PokemonType TYPE)
    {
        Rule rule = RuleOf(TYPE);
        int count = BONUS.Count(TYPE) + (rule.PARTNER is { } partner ? BONUS.Count(partner) : 0);
        return count >= Balance.BONUS_MIN_UNITS ? count : 0;
    }

    public static float Strength(TypeBonusSnapshot BONUS, PokemonType TYPE)
        => StrengthAt(TYPE, Units(BONUS, TYPE));

    private static float StrengthAt(PokemonType TYPE, int UNITS)
    {
        Rule rule = RuleOf(TYPE);
        return Math.Min(UNITS * rule.STEP, rule.CAP);
    }

    public static int GetTypeBonusCount(PokemonType TYPE) => SNAPSHOT.Count(TYPE);

    public static bool IsActive(PokemonType TYPE) => Units(SNAPSHOT, TYPE) > 0;

    public static float SwarmScale() => 1f - Strength(SNAPSHOT, PokemonType.BUG);

    private static int Percent(float VALUE) => (int)MathF.Round(VALUE * 100f);

    public static string Name(PokemonType TYPE) => TYPE switch
    {
        PokemonType.GHOST or PokemonType.DARK => "SHADOW",
        PokemonType.ROCK or PokemonType.STEEL => "STURDY",
        PokemonType.FIRE     => "BLAZE",
        PokemonType.WATER    => "TORRENT",
        PokemonType.GRASS    => "OVERGROW",
        PokemonType.ELECTRIC => "STATIC",
        PokemonType.BUG      => "SWARM",
        PokemonType.DRAGON   => "PRESSURE",
        PokemonType.NORMAL   => "ADAPTABILITY",
        PokemonType.GROUND   => "TREMOR",
        PokemonType.FIGHT    => "BRAWN",
        PokemonType.PSYCHIC  => "FORESIGHT",
        PokemonType.POISON   => "VENOM",
        PokemonType.FAIRY    => "CHARM",
        PokemonType.FLY      => "TAILWIND",
        PokemonType.ICE      => "FROSTBITE",
        _                    => TYPE.ToString().ToUpperInvariant()
    };

    public static string GetBonusDescription(PokemonType TYPE)
    {
        int active = Units(SNAPSHOT, TYPE);

        if (active == 0)
            return $"Needs {Balance.BONUS_MIN_UNITS} of this type on the field to activate. "
                 + $"With {Balance.BONUS_MIN_UNITS}: {Effect(TYPE, Balance.BONUS_MIN_UNITS)}";

        return Effect(TYPE, active) + " Every ally on the field benefits.";
    }

    private static string Effect(PokemonType TYPE, int UNITS)
    {
        float value = StrengthAt(TYPE, UNITS);

        return TYPE switch
        {
            PokemonType.GHOST or PokemonType.DARK => $"Allies fully evade {Percent(value)}% of incoming hits.",
            PokemonType.ROCK or PokemonType.STEEL => $"Allies gain {Percent(value)}% DEF and SPDEF.",
            PokemonType.FIRE     => $"Allies gain {Percent(value)}% ATK and SPATK.",
            PokemonType.WATER    => $"Allies charge their special {Percent(value)}% faster.",
            PokemonType.GRASS    => $"Allies regenerate {(int)value} HP per second while fighting, "
                                  + $"up to {GRASS_REGEN_CAP_PERCENT}% of their max HP.",
            PokemonType.ELECTRIC => $"Ally hits have a {Percent(value)}% chance to cut the target SPEED by {ELECTRIC_SPEED_CUT}.",
            PokemonType.BUG      => $"Allies attack {Percent(value)}% faster.",
            PokemonType.DRAGON   => $"Allies gain {Percent(value)}% ATK and SPATK.",
            PokemonType.NORMAL   => $"Allies gain {(int)value} ATK and {(int)value} DEF.",
            PokemonType.GROUND   => $"Allies deal {Percent(value)}% more damage, except to Flying foes.",
            PokemonType.FIGHT    => $"Allies gain {Percent(value)}% ATK.",
            PokemonType.PSYCHIC  => $"Allies gain {Percent(value)}% SPATK.",
            PokemonType.POISON   => $"Ally hits ignore {Percent(value)}% of the target DEF and SPDEF.",
            PokemonType.FAIRY    => $"Allies take {Percent(value)}% less special damage.",
            PokemonType.FLY      => $"Allies dodge {Percent(value)}% of all hits.",
            PokemonType.ICE      => $"Ally hits have a {Percent(value)}% chance to cut the target SPEED by {ICE_SPEED_CUT}.",
            _                    => "No bonus."
        };
    }
}
