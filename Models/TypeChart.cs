using System;

namespace PokemonTFT.Models;

public static class TypeChart
{
    private const float NEUTRAL   = 1.0f;
    private const float STRONG    = 2.0f;
    private const float WEAK      = 0.5f;
    private const float IMMUNE    = 0.0f;

    private static readonly int SIZE = Enum.GetValues<PokemonType>().Length;
    private static readonly float[] TABLE = BuildTable();

    private static float[] BuildTable()
    {
        int size  = Enum.GetValues<PokemonType>().Length;
        var table = new float[size * size];
        for (int i = 0; i < table.Length; i++) table[i] = NEUTRAL;

        void Set(PokemonType attacker, float value, params PokemonType[] defenders)
        {
            foreach (PokemonType defender in defenders)
                table[(int)attacker * size + (int)defender] = value;
        }

        Set(PokemonType.NORMAL,   WEAK,   PokemonType.ROCK, PokemonType.STEEL);
        Set(PokemonType.NORMAL,   IMMUNE, PokemonType.GHOST);

        Set(PokemonType.FIRE,     STRONG, PokemonType.GRASS, PokemonType.BUG, PokemonType.ICE, PokemonType.STEEL);
        Set(PokemonType.FIRE,     WEAK,   PokemonType.FIRE, PokemonType.WATER, PokemonType.ROCK, PokemonType.DRAGON);

        Set(PokemonType.WATER,    STRONG, PokemonType.FIRE, PokemonType.GROUND, PokemonType.ROCK);
        Set(PokemonType.WATER,    WEAK,   PokemonType.WATER, PokemonType.GRASS, PokemonType.DRAGON);

        Set(PokemonType.GRASS,    STRONG, PokemonType.WATER, PokemonType.GROUND, PokemonType.ROCK);
        Set(PokemonType.GRASS,    WEAK,   PokemonType.FIRE, PokemonType.GRASS, PokemonType.POISON, PokemonType.BUG,
                                          PokemonType.DRAGON, PokemonType.FLY, PokemonType.STEEL);

        Set(PokemonType.ELECTRIC, STRONG, PokemonType.WATER, PokemonType.FLY);
        Set(PokemonType.ELECTRIC, WEAK,   PokemonType.GRASS, PokemonType.ELECTRIC, PokemonType.DRAGON);
        Set(PokemonType.ELECTRIC, IMMUNE, PokemonType.GROUND);

        Set(PokemonType.ICE,      STRONG, PokemonType.GRASS, PokemonType.GROUND, PokemonType.FLY, PokemonType.DRAGON);
        Set(PokemonType.ICE,      WEAK,   PokemonType.FIRE, PokemonType.WATER, PokemonType.ICE, PokemonType.STEEL);

        Set(PokemonType.FIGHT,    STRONG, PokemonType.NORMAL, PokemonType.ICE, PokemonType.ROCK, PokemonType.DARK, PokemonType.STEEL);
        Set(PokemonType.FIGHT,    WEAK,   PokemonType.POISON, PokemonType.BUG, PokemonType.PSYCHIC, PokemonType.FLY, PokemonType.FAIRY);
        Set(PokemonType.FIGHT,    IMMUNE, PokemonType.GHOST);

        Set(PokemonType.POISON,   STRONG, PokemonType.GRASS, PokemonType.FAIRY);
        Set(PokemonType.POISON,   WEAK,   PokemonType.POISON, PokemonType.GROUND, PokemonType.ROCK, PokemonType.GHOST);
        Set(PokemonType.POISON,   IMMUNE, PokemonType.STEEL);

        Set(PokemonType.GROUND,   STRONG, PokemonType.FIRE, PokemonType.ELECTRIC, PokemonType.POISON, PokemonType.ROCK, PokemonType.STEEL);
        Set(PokemonType.GROUND,   WEAK,   PokemonType.GRASS, PokemonType.BUG);
        Set(PokemonType.GROUND,   IMMUNE, PokemonType.FLY);

        Set(PokemonType.FLY,      STRONG, PokemonType.GRASS, PokemonType.FIGHT, PokemonType.BUG);
        Set(PokemonType.FLY,      WEAK,   PokemonType.ELECTRIC, PokemonType.ROCK, PokemonType.STEEL);

        Set(PokemonType.PSYCHIC,  STRONG, PokemonType.FIGHT, PokemonType.POISON);
        Set(PokemonType.PSYCHIC,  WEAK,   PokemonType.PSYCHIC, PokemonType.STEEL);
        Set(PokemonType.PSYCHIC,  IMMUNE, PokemonType.DARK);

        Set(PokemonType.BUG,      STRONG, PokemonType.GRASS, PokemonType.PSYCHIC, PokemonType.DARK);
        Set(PokemonType.BUG,      WEAK,   PokemonType.FIRE, PokemonType.FIGHT, PokemonType.FLY, PokemonType.GHOST,
                                          PokemonType.STEEL, PokemonType.FAIRY);

        Set(PokemonType.ROCK,     STRONG, PokemonType.FIRE, PokemonType.ICE, PokemonType.FLY, PokemonType.BUG);
        Set(PokemonType.ROCK,     WEAK,   PokemonType.FIGHT, PokemonType.GROUND, PokemonType.STEEL);

        Set(PokemonType.GHOST,    STRONG, PokemonType.GHOST, PokemonType.PSYCHIC);
        Set(PokemonType.GHOST,    WEAK,   PokemonType.DARK);
        Set(PokemonType.GHOST,    IMMUNE, PokemonType.NORMAL);

        Set(PokemonType.DRAGON,   STRONG, PokemonType.DRAGON);
        Set(PokemonType.DRAGON,   WEAK,   PokemonType.STEEL);
        Set(PokemonType.DRAGON,   IMMUNE, PokemonType.FAIRY);

        Set(PokemonType.DARK,     STRONG, PokemonType.GHOST, PokemonType.PSYCHIC);
        Set(PokemonType.DARK,     WEAK,   PokemonType.FIGHT, PokemonType.DARK, PokemonType.FAIRY);

        Set(PokemonType.STEEL,    STRONG, PokemonType.ICE, PokemonType.ROCK, PokemonType.FAIRY);
        Set(PokemonType.STEEL,    WEAK,   PokemonType.FIRE, PokemonType.WATER, PokemonType.ELECTRIC, PokemonType.STEEL);

        Set(PokemonType.FAIRY,    STRONG, PokemonType.FIGHT, PokemonType.DRAGON, PokemonType.DARK);
        Set(PokemonType.FAIRY,    WEAK,   PokemonType.FIRE, PokemonType.POISON, PokemonType.STEEL);

        return table;
    }

    public static float GetEffectiveness(PokemonType ATTACKER, PokemonType DEFENDER)
        => TABLE[(int)ATTACKER * SIZE + (int)DEFENDER];

    public static bool IsImmune(float MULTIPLIER) => MULTIPLIER < 0.001f;
}
