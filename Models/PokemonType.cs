namespace PokemonTFT.Models;

public enum PokemonType
{
    GRASS,
    FIRE,
    WATER,
    GROUND,
    ROCK,
    STEEL,
    FAIRY,
    DARK,
    GHOST,
    POISON,
    BUG,
    DRAGON,
    FLY,
    ICE,
    NORMAL,
    PSYCHIC,
    ELECTRIC,
    FIGHT
}

public enum PokemonStyle
{
    BALANCED,
    MAGIC_TANK,
    PHYSICAL_TANK,
    EVASION_TANK,
    FIGHTER,
    MAGE,
    MAGIC_FIGHTER
}

public static class PokemonTypeAssets
{
    private static readonly string[] ICON_NAME = BuildIconNames();

    private static string[] BuildIconNames()
    {
        var names = new string[System.Enum.GetValues<PokemonType>().Length];
        foreach (PokemonType type in System.Enum.GetValues<PokemonType>())
            names[(int)type] = type.ToString().ToLowerInvariant();

        names[(int)PokemonType.STEEL]    = "iron";
        names[(int)PokemonType.ELECTRIC] = "thunder";
        return names;
    }

    public static string IconPath(PokemonType TYPE) => "UI/Types/" + ICON_NAME[(int)TYPE];
}
