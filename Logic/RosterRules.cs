using System;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class RosterRules
{
    public const int MIN_STAT = 1;
    public const int MIN_SLICE = 8;
    public const int MIN_SCALE = 1;

    private const int COST_MIN = 6;
    private const int COST_MAX = 16;
    private const int COST_STEPS = 5;

    private const int EVOLUTION_MIN = 5;
    private const int EVOLUTION_MAX = 30;
    private const int EVOLUTION_STEP = 8;
    private const float EVOLUTION_POWER_WEIGHT = 20f;

    public static int Cost(int BASE_TOTAL, int WEAKEST, int STRONGEST)
    {
        if (BASE_TOTAL <= 0) return COST_MIN;

        int span = Math.Max(1, STRONGEST - WEAKEST);
        float position = Math.Clamp((BASE_TOTAL - WEAKEST) / (float)span, 0f, 1f);

        int step = Math.Clamp((int)(position * COST_STEPS), 0, COST_STEPS - 1);
        return COST_MIN + step * (COST_MAX - COST_MIN) / (COST_STEPS - 1);
    }

    public static int EvolutionLevel(Pokemon FROM, Pokemon TO, int PREVIOUS)
    {
        float ratio = FROM.BaseStatTotal <= 0
            ? 1f
            : TO.BaseStatTotal / (float)FROM.BaseStatTotal;

        float scaled = EVOLUTION_MIN + Math.Max(0f, ratio - 1f) * EVOLUTION_POWER_WEIGHT;

        return Math.Clamp(
            Math.Max((int)MathF.Round(scaled), PREVIOUS + EVOLUTION_STEP),
            EVOLUTION_MIN, EVOLUTION_MAX);
    }

    public static int SafeStat(int VALUE) => Math.Max(MIN_STAT, VALUE);
}
