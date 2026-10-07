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

    public static CombatResult Resolve(Pokemon ATTACKER, Pokemon DEFENDER, TypeBonusSnapshot BONUS,
                                       bool ATTACKER_ALLY)
    {
        float roll = 1f + (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * Balance.DAMAGE_VARIANCE;

        int effectiveATK   = ATTACKER.ATK;
        int effectiveSPATK = ATTACKER.SPATK;
        int effectiveDEF   = DEFENDER.DEF;
        int effectiveSPDEF = DEFENDER.SPDEF;

        bool offence = ATTACKER_ALLY;
        bool defence = !ATTACKER_ALLY;

        if (offence)
        {
            float blaze = Bonus(BONUS, PokemonType.FIRE);
            effectiveATK   = Boost(effectiveATK, blaze);
            effectiveSPATK = Boost(effectiveSPATK, blaze);

            effectiveATK = Boost(effectiveATK, Bonus(BONUS, PokemonType.FIGHT));
            effectiveSPATK = Boost(effectiveSPATK, Bonus(BONUS, PokemonType.PSYCHIC));

            float pressure = Bonus(BONUS, PokemonType.DRAGON);
            effectiveATK   = Boost(effectiveATK, pressure);
            effectiveSPATK = Boost(effectiveSPATK, pressure);

            effectiveATK += (int)Bonus(BONUS, PokemonType.NORMAL);

            float shred = Bonus(BONUS, PokemonType.POISON);
            effectiveDEF   = Boost(effectiveDEF, -shred);
            effectiveSPDEF = Boost(effectiveSPDEF, -shred);
        }

        if (defence)
        {
            float sturdy = Bonus(BONUS, PokemonType.ROCK);
            effectiveDEF   = Boost(effectiveDEF, sturdy);
            effectiveSPDEF = Boost(effectiveSPDEF, sturdy);

            effectiveDEF += (int)Bonus(BONUS, PokemonType.NORMAL);

            if (Chance(Bonus(BONUS, PokemonType.FLY)))
                return CombatResult.NoHit(EVADED: true, IMMUNE: false);

            if (Chance(Bonus(BONUS, PokemonType.GHOST)))
                return CombatResult.NoHit(EVADED: true, IMMUNE: false);
        }

        effectiveDEF   = Math.Max(1, effectiveDEF);
        effectiveSPDEF = Math.Max(1, effectiveSPDEF);

        float physical = (float)effectiveATK / effectiveDEF;
        float specialRatio = (float)effectiveSPATK / effectiveSPDEF;
        bool hybrid = ATTACKER.GetStyle() == PokemonStyle.BALANCED;

        float basic = hybrid ? Math.Max(physical, specialRatio) : physical;
        float totalDamage = basic * Balance.DAMAGE_SCALE * roll;

        float torrent = offence ? Bonus(BONUS, PokemonType.WATER) : 0f;
        int effectiveSpecialMax = (int)(ATTACKER.SPECIAL_MAX * (1 - torrent));

        bool special = ATTACKER.SPECIAL_COUNTER >= effectiveSpecialMax;
        if (special)
        {
            float power = hybrid ? Math.Max(physical, specialRatio)
                : ATTACKER.BASE_ATK > ATTACKER.BASE_SPATK ? physical : specialRatio;
            float specialDamage = power * Balance.SPECIAL_DAMAGE_SCALE * roll;

            if (defence) specialDamage *= 1f - Bonus(BONUS, PokemonType.FAIRY);

            totalDamage = Math.Max(1, specialDamage);
            ATTACKER.SPECIAL_COUNTER = 0;
        }
        else
        {
            ATTACKER.ChargeSpecial(CHARGE_PER_HIT);
        }

        if (offence)
        {
            if (DEFENDER.TYPE != PokemonType.FLY)
                totalDamage *= 1f + Bonus(BONUS, PokemonType.GROUND);

            if (Chance(Bonus(BONUS, PokemonType.ICE)))
                DEFENDER.SPEED = Math.Max(1, DEFENDER.SPEED - GameBonusLogic.ICE_SPEED_CUT);

            if (Chance(Bonus(BONUS, PokemonType.ELECTRIC)))
                DEFENDER.SPEED = Math.Max(1, DEFENDER.SPEED - GameBonusLogic.ELECTRIC_SPEED_CUT);
        }

        float typeMultiplier = Math.Max(TypeChart.GetEffectiveness(ATTACKER.TYPE, DEFENDER.TYPE),
            Balance.MIN_TYPE_MULTIPLIER);

        totalDamage *= typeMultiplier;

        return new CombatResult((int)Math.Max(1, totalDamage), EVADED: false, IMMUNE: false,
            SPECIAL: special, typeMultiplier);
    }

    private static float Bonus(TypeBonusSnapshot BONUS, PokemonType TYPE) => GameBonusLogic.Strength(BONUS, TYPE);

    public static int Regeneration(Pokemon POKEMON, TypeBonusSnapshot BONUS)
    {
        if (POKEMON.HP <= 0 || POKEMON.HP >= POKEMON.MAX_HP) return 0;

        int cap = Math.Max(1, POKEMON.MAX_HP * GameBonusLogic.GRASS_REGEN_CAP_PERCENT / 100);
        int regen = Math.Min((int)Bonus(BONUS, PokemonType.GRASS), cap);
        return Math.Min(regen, POKEMON.MAX_HP - POKEMON.HP);
    }

    private static bool Chance(float SHARE) => SHARE > 0f && Random.Shared.NextDouble() < SHARE;

    private static int Boost(int VALUE, float SCALE) => Math.Max(1, (int)(VALUE * (1 + SCALE)));
}
