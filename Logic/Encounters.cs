using System;
using System.Collections.Generic;
using PokemonTFT.Data;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class Encounters
{
    public static List<Pokemon> Team(int ROUND, int SLOTS)
    {
        var team = new List<Pokemon>();
        if (SLOTS <= 0) return team;

        bool boss = Balance.IsBossRound(ROUND);
        int count = boss ? Math.Max(1, SLOTS - Balance.LEGENDARY_ESCORT_CUT) : SLOTS;

        float target = Balance.EnemyPower(ROUND);
        int level = Balance.EnemyLevel(ROUND);
        level += CatchUpLevels(target, level);

        if (boss && PokemonDatabase.RandomLegendary() is { } legend)
            team.Add(Crown(legend, target, level));

        while (team.Count < count)
        {
            Pokemon? pick = PokemonDatabase.RandomNearPower(target, level, Balance.ENEMY_POWER_SPREAD);
            if (pick == null) break;

            Raise(pick, level);
            team.Add(pick);
        }

        return team;
    }

    private static int CatchUpLevels(float TARGET, int LEVEL)
    {
        int ceiling = PokemonDatabase.PowerCeiling(LEVEL);
        if (ceiling <= 0 || TARGET <= ceiling) return 0;

        return (int)MathF.Round((TARGET / ceiling - 1f) / Balance.LEVEL_GAIN);
    }

    private static void Raise(Pokemon POKEMON, int LEVEL)
    {
        while (POKEMON.LEVEL < LEVEL) POKEMON.LevelUp();
        for (int guard = 0; guard < 4 && POKEMON.CanEvolve; guard++) POKEMON.Evolve();
    }

    private static Pokemon Crown(Pokemon LEGEND, float TARGET, int LEVEL)
    {
        float factor = Math.Min(1f, TARGET * Balance.LEGENDARY_POWER / Math.Max(1, LEGEND.BaseStatTotal));

        LEGEND.BASE_HP    = Scale(LEGEND.BASE_HP, factor * Balance.LEGENDARY_HEALTH);
        LEGEND.BASE_ATK   = Scale(LEGEND.BASE_ATK, factor);
        LEGEND.BASE_SPATK = Scale(LEGEND.BASE_SPATK, factor);
        LEGEND.BASE_DEF   = Scale(LEGEND.BASE_DEF, factor);
        LEGEND.BASE_SPDEF = Scale(LEGEND.BASE_SPDEF, factor);
        LEGEND.BASE_SPEED = Scale(LEGEND.BASE_SPEED, factor);

        while (LEGEND.LEVEL < LEVEL) LEGEND.LevelUp();
        LEGEND.RecalculateStats();
        LEGEND.HP = LEGEND.MAX_HP;
        return LEGEND;
    }

    private static int Scale(int VALUE, float FACTOR) => Math.Max(1, (int)MathF.Round(VALUE * FACTOR));
}
