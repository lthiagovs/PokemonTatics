using System;
using System.Collections.Generic;
using PokemonTFT.Core;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public enum BattleMetric
{
    DAMAGE,
    TANKED,
    KILLS,
    HEALING
}

public sealed class BattleRecord
{
    public string NAME = string.Empty;
    public string PORTRAIT = string.Empty;
    public PokemonType TYPE;
    public int LEVEL;
    public bool SURVIVED;
    public int DEALT;
    public int TAKEN;
    public int KILLS;
    public int HEALED;

    public int Value(BattleMetric METRIC) => METRIC switch
    {
        BattleMetric.TANKED  => TAKEN,
        BattleMetric.KILLS   => KILLS,
        BattleMetric.HEALING => HEALED,
        _                    => DEALT
    };
}

public static class BattleStats
{
    private static readonly Dictionary<Pokemon, BattleRecord> ACTIVE = new(ReferenceEqualityComparer.Instance);
    private static readonly List<BattleRecord> LAST = [];

    public static int LAST_ROUND { get; private set; }

    public static bool LAST_WON { get; private set; }

    public static bool HasRound => LAST_ROUND > 0;

    public static void Hit(PokemonEntity? ATTACKER, PokemonEntity DEFENDER, int DAMAGE)
    {
        if (DAMAGE <= 0) return;

        if (ATTACKER is { ENEMY: false } && Of(ATTACKER) is { } dealer) dealer.DEALT += DAMAGE;
        if (!DEFENDER.ENEMY && Of(DEFENDER) is { } target) target.TAKEN += DAMAGE;
    }

    public static void Kill(PokemonEntity? KILLER)
    {
        if (KILLER is { ENEMY: false } && Of(KILLER) is { } record) record.KILLS++;
    }

    public static void Heal(PokemonEntity UNIT, int AMOUNT)
    {
        if (AMOUNT > 0 && !UNIT.ENEMY && Of(UNIT) is { } record) record.HEALED += AMOUNT;
    }

    public static void Finish(int ROUND, bool WON, IReadOnlyList<PokemonEntity> ENTITIES)
    {
        LAST.Clear();

        for (int i = 0; i < ENTITIES.Count; i++)
        {
            PokemonEntity entity = ENTITIES[i];
            if (entity.ENEMY || entity.POKEMON == null) continue;

            BattleRecord record = Of(entity)!;
            record.NAME = entity.POKEMON.NAME;
            record.PORTRAIT = entity.POKEMON.PortraitPath;
            record.TYPE = entity.POKEMON.TYPE;
            record.LEVEL = entity.POKEMON.LEVEL;
            record.SURVIVED = entity.IsAlive;
            LAST.Add(record);
        }

        ACTIVE.Clear();
        LAST_ROUND = ROUND;
        LAST_WON = WON;
    }

    public static void Clear()
    {
        ACTIVE.Clear();
        LAST.Clear();
        LAST_ROUND = 0;
        LAST_WON = false;
    }

    public static List<BattleRecord> Ranking(BattleMetric METRIC)
    {
        var ranking = new List<BattleRecord>(LAST);
        ranking.Sort((a, b) =>
        {
            int order = b.Value(METRIC).CompareTo(a.Value(METRIC));
            if (order != 0) return order;
            order = b.DEALT.CompareTo(a.DEALT);
            return order != 0 ? order : string.Compare(a.NAME, b.NAME, StringComparison.Ordinal);
        });
        return ranking;
    }

    private static BattleRecord? Of(PokemonEntity ENTITY)
    {
        if (ENTITY.POKEMON == null) return null;

        if (!ACTIVE.TryGetValue(ENTITY.POKEMON, out BattleRecord? record))
        {
            record = new BattleRecord();
            ACTIVE[ENTITY.POKEMON] = record;
        }

        return record;
    }
}
