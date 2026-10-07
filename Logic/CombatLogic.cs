using System;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public readonly struct CombatResult
{
    public readonly int  DAMAGE;
    public readonly bool EVADED;
    public readonly bool IMMUNE;
    public readonly bool SPECIAL;

    public readonly float MULTIPLIER;

    public CombatResult(int DAMAGE, bool EVADED, bool IMMUNE, bool SPECIAL, float MULTIPLIER)
    {
        this.DAMAGE     = DAMAGE;
        this.EVADED     = EVADED;
        this.IMMUNE     = IMMUNE;
        this.SPECIAL    = SPECIAL;
        this.MULTIPLIER = MULTIPLIER;
    }

    public bool HIT => !EVADED && !IMMUNE;

    public static CombatResult NoHit(bool EVADED, bool IMMUNE) => new(0, EVADED, IMMUNE, false, 1f);
}

public static class CombatLogic
{
    private const int CHARGE_PER_HIT = 200;

    public static CombatResult Resolve(Pokemon ATTACKER, Pokemon DEFENDER, TypeBonusSnapshot BONUS, bool SECOND_TICK)
    {
        float roll = 1f + (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * Balance.DAMAGE_VARIANCE;

        int effectiveATK   = ATTACKER.ATK;
        int effectiveSPATK = ATTACKER.SPATK;
        int effectiveDEF   = DEFENDER.DEF;
        int effectiveSPDEF = DEFENDER.SPDEF;

        int fireBuff = BONUS.Count(PokemonType.FIRE) * 5;
        effectiveATK   = (int)(effectiveATK   * (1 + fireBuff / 100f));
        effectiveSPATK = (int)(effectiveSPATK * (1 + fireBuff / 100f));

        int rockCount    = BONUS.Count(PokemonType.ROCK);
        int steelCount   = BONUS.Count(PokemonType.STEEL);
        float sturdyBuff = Math.Min((rockCount + steelCount) * 0.08f, 0.64f);
        effectiveDEF   = (int)(effectiveDEF   * (1 + sturdyBuff));
        effectiveSPDEF = (int)(effectiveSPDEF * (1 + sturdyBuff));

        int normalBuff = BONUS.Count(PokemonType.NORMAL) * 8;
        if (ATTACKER.TYPE == PokemonType.NORMAL) effectiveATK += normalBuff;
        if (DEFENDER.TYPE == PokemonType.NORMAL) effectiveDEF += normalBuff;

        if (ATTACKER.TYPE == PokemonType.DRAGON && BONUS.Count(PokemonType.DRAGON) == 1)
        {
            effectiveATK   *= 2;
            effectiveSPATK *= 2;
        }

        int shadowCount = BONUS.Count(PokemonType.GHOST) + BONUS.Count(PokemonType.DARK);
        if (shadowCount > 0 && (DEFENDER.TYPE == PokemonType.GHOST || DEFENDER.TYPE == PokemonType.DARK))
        {
            int evasionChance = Math.Min(shadowCount * 5, 40);
            if (Random.Shared.Next(0, 100) < evasionChance) return CombatResult.NoHit(EVADED: true, IMMUNE: false);
        }

        effectiveDEF   = Math.Max(1, effectiveDEF);
        effectiveSPDEF = Math.Max(1, effectiveSPDEF);

        float totalDamage = (float)effectiveATK / effectiveDEF * Balance.DAMAGE_SCALE * roll;

        int waterCount          = BONUS.Count(PokemonType.WATER);
        float waterReduction    = Math.Min(waterCount * 0.10f, 0.50f);
        int effectiveSpecialMax = (int)(ATTACKER.SPECIAL_MAX * (1 - waterReduction));

        bool special = ATTACKER.SPECIAL_COUNTER >= effectiveSpecialMax;
        if (special)
        {
            float specialDamage = (float)effectiveSPATK / effectiveSPDEF * Balance.SPECIAL_DAMAGE_SCALE * roll;
            totalDamage = Math.Max(1, specialDamage);
            ATTACKER.SPECIAL_COUNTER = 0;
        }
        else
        {
            ATTACKER.ChargeSpecial(CHARGE_PER_HIT);
        }

        int electricCount = BONUS.Count(PokemonType.ELECTRIC);
        if (electricCount > 0 && ATTACKER.TYPE == PokemonType.ELECTRIC)
        {
            int chance = Math.Min(electricCount * 15, 45);
            if (Random.Shared.Next(0, 100) < chance)
                DEFENDER.SPEED = Math.Max(1, DEFENDER.SPEED - 15);
        }

        if (ATTACKER.TYPE == PokemonType.GRASS && SECOND_TICK)
        {
            int grassCount = BONUS.Count(PokemonType.GRASS);
            int regen      = Math.Min(grassCount * 5, (int)(ATTACKER.MAX_HP * 0.02f));
            ATTACKER.HP    = Math.Min(ATTACKER.MAX_HP, ATTACKER.HP + regen);
        }

        float typeMultiplier = TypeChart.GetEffectiveness(ATTACKER.TYPE, DEFENDER.TYPE);
        if (TypeChart.IsImmune(typeMultiplier)) return CombatResult.NoHit(EVADED: false, IMMUNE: true);

        totalDamage *= typeMultiplier;

        return new CombatResult((int)Math.Max(1, totalDamage), EVADED: false, IMMUNE: false, SPECIAL: special, typeMultiplier);
    }
}
