using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public readonly record struct TypeEffect(string SPRITE, StatusKind STATUS, int CHANCE);

public static class TypeEffects
{
    public static TypeEffect Of(PokemonType TYPE) => TYPE switch
    {
        PokemonType.GRASS    => new("acid", StatusKind.POISON, 25),
        PokemonType.FIRE     => new("flare", StatusKind.BURN, 35),
        PokemonType.WATER    => new("wave", StatusKind.PARALYSIS, 10),
        PokemonType.ELECTRIC => new("spark", StatusKind.PARALYSIS, 35),
        PokemonType.ICE      => new("frost", StatusKind.PARALYSIS, 30),
        PokemonType.GROUND   => new("quake", StatusKind.BURN, 0),
        PokemonType.ROCK     => new("rock", StatusKind.BURN, 0),
        PokemonType.STEEL    => new("nova", StatusKind.BURN, 0),
        PokemonType.POISON   => new("sludge", StatusKind.POISON, 45),
        PokemonType.BUG      => new("sting", StatusKind.POISON, 30),
        PokemonType.FLY      => new("gale", StatusKind.BURN, 0),
        PokemonType.FAIRY    => new("glimmer", StatusKind.BURN, 0),
        PokemonType.PSYCHIC  => new("mindblast", StatusKind.PARALYSIS, 20),
        PokemonType.GHOST    => new("hex", StatusKind.POISON, 20),
        PokemonType.DARK     => new("void", StatusKind.POISON, 20),
        PokemonType.DRAGON   => new("orb", StatusKind.BURN, 20),
        PokemonType.FIGHT    => new("impact", StatusKind.BURN, 0),
        _                    => new("slash", StatusKind.BURN, 0)
    };

    public static string Describe(PokemonType TYPE)
    {
        TypeEffect effect = Of(TYPE);

        return effect.CHANCE <= 0
            ? "no status on hit"
            : $"{effect.CHANCE}% to inflict {StatusState.Label(effect.STATUS)}";
    }
}
