using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public enum SpecialShape
{
    FOCUS,
    BLAST,
    CLEAVE,
    SHOCKWAVE
}

public readonly record struct SpecialBehaviour(
    SpecialShape SHAPE,
    float TARGET_SCALE,
    float SPLASH_SHARE,
    float RADIUS_TILES,
    float CONE_DEGREES,
    bool STATUS_ON_SPLASH);

public static class StyleBehaviour
{
    public static SpecialBehaviour Of(PokemonStyle STYLE) => STYLE switch
    {
        PokemonStyle.MAGE =>
            new(SpecialShape.FOCUS, 1.25f, 0f, 0f, 0f, false),

        PokemonStyle.MAGIC_FIGHTER =>
            new(SpecialShape.BLAST, 1.0f, 0.65f, 1.8f, 0f, true),

        PokemonStyle.FIGHTER =>
            new(SpecialShape.CLEAVE, 0.9f, 0.85f, 2.4f, 110f, false),

        PokemonStyle.PHYSICAL_TANK =>
            new(SpecialShape.SHOCKWAVE, 0.7f, 0.7f, 2.0f, 0f, false),

        PokemonStyle.MAGIC_TANK =>
            new(SpecialShape.SHOCKWAVE, 0.65f, 0.65f, 2.2f, 0f, true),

        PokemonStyle.EVASION_TANK =>
            new(SpecialShape.CLEAVE, 0.75f, 0.7f, 2.0f, 140f, true),

        _ =>
            new(SpecialShape.BLAST, 1.0f, 0.4f, 1.3f, 0f, false)
    };

    public static string Describe(PokemonStyle STYLE)
    {
        SpecialBehaviour behaviour = Of(STYLE);

        return behaviour.SHAPE switch
        {
            SpecialShape.FOCUS =>
                $"focused blast on one foe for {Percent(behaviour.TARGET_SCALE)} damage",

            SpecialShape.BLAST =>
                $"bursts on the target for {Percent(behaviour.TARGET_SCALE)} and splashes "
                + $"{Percent(behaviour.SPLASH_SHARE)} within {behaviour.RADIUS_TILES:0.#} tiles",

            SpecialShape.CLEAVE =>
                $"sweeps a {behaviour.CONE_DEGREES:0} degree arc in front, hitting every foe for "
                + $"{Percent(behaviour.SPLASH_SHARE)}",

            _ =>
                $"erupts around itself, hitting every foe within {behaviour.RADIUS_TILES:0.#} tiles for "
                + $"{Percent(behaviour.SPLASH_SHARE)}"
        };
    }

    private static string Percent(float VALUE) => $"{(int)(VALUE * 100)}%";
}
