using System;
using Microsoft.Xna.Framework;

namespace ENGINE.MODELS;

public class Pokemon
{

    public string NAME;
    public int MAX_HP;
    public int LEVEL = 1;
    public int XP = 0;
    public int EVOLUTION_LEVEL = 100;
    public int HP;
    public int ATK;
    public int SPATK;
    public int DEF;
    public int SPDEF;
    public int SPEED;
    public int COST;
    public int SPECIAL_COUNTER = 0;
    public int SPECIAL_MAX = 1000;
    public PokemonType TYPE;
    public Pokemon EVOLUTION;
    public bool SPECIAL_EFFECT = false;
    public PokemonStyle STYLE = PokemonStyle.BALANCED;
    public String EFFECT = "scratch";

    public int XPToNextLevel() => (int)(LEVEL * LEVEL * 10 * 1.5f);

    public Pokemon(string NAME, int HP, int ATK, int SPATK, int DEF, int SPDEF, int SPEED, PokemonType TYPE, int COST, int EVOLUTION_LEVEL, String EFFECT = "scratch", Pokemon EVOLUTION = null)
    {
        this.NAME = NAME;
        this.HP = HP;
        this.MAX_HP = HP;
        this.ATK = ATK;
        this.SPATK = SPATK;
        this.DEF = DEF;
        this.SPDEF = SPDEF;
        this.SPEED = SPEED;
        this.TYPE = TYPE;
        this.COST = COST;
        this.EVOLUTION = EVOLUTION;
        this.EVOLUTION_LEVEL= EVOLUTION_LEVEL;
        this.EFFECT = EFFECT;
        this.STYLE = this.GetPokemonStyle();
        
    }

    #region HELPERS

    public PokemonStyle GetPokemonStyle()
    {
        if (HP > 100 || DEF > 80 || SPDEF > 80)
        {
            if (SPDEF > DEF) return PokemonStyle.MAGIC_TANK;
            if (DEF > SPDEF) return PokemonStyle.PHYSICAL_TANK;
            if (SPEED > 90)  return PokemonStyle.EVASION_TANK;
            return PokemonStyle.PHYSICAL_TANK;
        }

        if (ATK > SPATK)
        {
            return PokemonStyle.PHYSICAL_FIGHTER;
        }

        if (SPATK > ATK)
        {
            if (SPEED > 90) return PokemonStyle.MAGE;
            return PokemonStyle.MAGIC_FIGHTER;
        }

        if (SPEED > 100)
        {
            return PokemonStyle.EVASION_TANK;
        }


        return PokemonStyle.BALANCED;
    }

    public string BuildPokemonHint()
    {

        string hintTexto = 
            $"\n"+
            $"    --- {NAME.ToUpper()} ---    \n" +
            $"    STYLE: {this.GetPokemonStyle().ToString().Replace("_", " ")}    \n" +
            $"    TYPE: {TYPE}\n" +
            $"    ---------------------    \n" +
            $"    LVL: {LEVEL} | XP: {XP}/{XPToNextLevel()}    \n" +
            $"    HP: {HP}/{MAX_HP}    \n" +
            $"    ATK: {ATK} | DEF: {DEF}    \n" +
            $"    SPATK: {SPATK} | SPDEF: {SPDEF}    \n" +
            $"    SPEED: {SPEED}    \n" +
            $"    MANA COST: {COST}    " +
            $"\n";

        return hintTexto;
    }
    #endregion

    #region XP
    public void GainXP(int amount)
    {
        XP += amount;
        while(XP >= XPToNextLevel())
        {
            XP -= XPToNextLevel();
            LevelUp();
        }
    }

    private void LevelUp()
    {
        LEVEL++;
        float growth = 1.10f;
        MAX_HP = (int)(MAX_HP * growth);
        HP     = MAX_HP;
        ATK    = (int)(ATK   * growth);
        SPATK  = (int)(SPATK * growth);
        DEF    = (int)(DEF   * growth);
        SPDEF  = (int)(SPDEF * growth);
        SPEED  = (int)(SPEED * growth);
        GameMusic.PlayLevelUp();

        if(EVOLUTION != null && LEVEL >= EVOLUTION_LEVEL) Evolve();
    }

    private void Evolve()
    {
        if(this.EVOLUTION == null ) return;
        
        this.NAME   = EVOLUTION.NAME;
        this.ATK    = EVOLUTION.ATK;
        this.SPATK  = EVOLUTION.SPATK;
        this.DEF    = EVOLUTION.DEF;
        this.SPDEF  = EVOLUTION.SPDEF;
        this.SPEED  = EVOLUTION.SPEED;
        this.MAX_HP = EVOLUTION.MAX_HP;
        this.HP     = EVOLUTION.MAX_HP;
        this.TYPE   = EVOLUTION.TYPE;
        this.EVOLUTION = EVOLUTION.EVOLUTION;
    }
    #endregion

    #region COMBAT

    public void ChargeAttack(int COUNT) { this.SPECIAL_COUNTER+=COUNT; }

    public static float GetTypeEffectiveness(PokemonType attacker, PokemonType defender)
    {
        switch (attacker)
        {
            case PokemonType.FIRE:
                switch (defender)
                {
                    case PokemonType.GRASS:
                    case PokemonType.BUG:
                    case PokemonType.ICE:
                    case PokemonType.STEEL:  return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.WATER:
                    case PokemonType.ROCK:
                    case PokemonType.DRAGON: return 0.5f;
                }
                break;
            case PokemonType.WATER:
                switch (defender)
                {
                    case PokemonType.FIRE:
                    case PokemonType.GROUND:
                    case PokemonType.ROCK:   return 2.0f;
                    case PokemonType.WATER:
                    case PokemonType.GRASS:
                    case PokemonType.DRAGON: return 0.5f;
                }
                break;
            case PokemonType.GRASS:
                switch (defender)
                {
                    case PokemonType.WATER:
                    case PokemonType.GROUND:
                    case PokemonType.ROCK:   return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.GRASS:
                    case PokemonType.POISON:
                    case PokemonType.BUG:
                    case PokemonType.DRAGON:
                    case PokemonType.FLY:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
            case PokemonType.ELECTRIC:
                switch (defender)
                {
                    case PokemonType.WATER:
                    case PokemonType.FLY:    return 2.0f;
                    case PokemonType.GRASS:
                    case PokemonType.ELECTRIC:
                    case PokemonType.DRAGON: return 0.5f;
                    case PokemonType.GROUND: return 0.0f;
                }
                break;
            case PokemonType.ICE:
                switch (defender)
                {
                    case PokemonType.GRASS:
                    case PokemonType.GROUND:
                    case PokemonType.FLY:
                    case PokemonType.DRAGON: return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.WATER:
                    case PokemonType.ICE:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
            case PokemonType.FIGHT:
                switch (defender)
                {
                    case PokemonType.NORMAL:
                    case PokemonType.ICE:
                    case PokemonType.ROCK:
                    case PokemonType.DARK:
                    case PokemonType.STEEL:  return 2.0f;
                    case PokemonType.POISON:
                    case PokemonType.BUG:
                    case PokemonType.PSYCHIC:
                    case PokemonType.FLY:
                    case PokemonType.FAIRY:  return 0.5f;
                    case PokemonType.GHOST:  return 0.0f;
                }
                break;
            case PokemonType.POISON:
                switch (defender)
                {
                    case PokemonType.GRASS:
                    case PokemonType.FAIRY:  return 2.0f;
                    case PokemonType.POISON:
                    case PokemonType.GROUND:
                    case PokemonType.ROCK:
                    case PokemonType.GHOST:  return 0.5f;
                    case PokemonType.STEEL:  return 0.0f;
                }
                break;
            case PokemonType.GROUND:
                switch (defender)
                {
                    case PokemonType.FIRE:
                    case PokemonType.ELECTRIC:
                    case PokemonType.POISON:
                    case PokemonType.ROCK:
                    case PokemonType.STEEL:  return 2.0f;
                    case PokemonType.GRASS:
                    case PokemonType.BUG:    return 0.5f;
                    case PokemonType.FLY:    return 0.0f;
                }
                break;
            case PokemonType.FLY:
                switch (defender)
                {
                    case PokemonType.GRASS:
                    case PokemonType.FIGHT:
                    case PokemonType.BUG:    return 2.0f;
                    case PokemonType.ELECTRIC:
                    case PokemonType.ROCK:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
            case PokemonType.PSYCHIC:
                switch (defender)
                {
                    case PokemonType.FIGHT:
                    case PokemonType.POISON: return 2.0f;
                    case PokemonType.PSYCHIC:
                    case PokemonType.STEEL:  return 0.5f;
                    case PokemonType.DARK:   return 0.0f;
                }
                break;
            case PokemonType.BUG:
                switch (defender)
                {
                    case PokemonType.GRASS:
                    case PokemonType.PSYCHIC:
                    case PokemonType.DARK:   return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.FIGHT:
                    case PokemonType.FLY:
                    case PokemonType.GHOST:
                    case PokemonType.STEEL:
                    case PokemonType.FAIRY:  return 0.5f;
                }
                break;
            case PokemonType.ROCK:
                switch (defender)
                {
                    case PokemonType.FIRE:
                    case PokemonType.ICE:
                    case PokemonType.FLY:
                    case PokemonType.BUG:    return 2.0f;
                    case PokemonType.FIGHT:
                    case PokemonType.GROUND:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
            case PokemonType.GHOST:
                switch (defender)
                {
                    case PokemonType.GHOST:
                    case PokemonType.PSYCHIC: return 2.0f;
                    case PokemonType.DARK:    return 0.5f;
                    case PokemonType.NORMAL:  return 0.0f;
                }
                break;
            case PokemonType.DRAGON:
                switch (defender)
                {
                    case PokemonType.DRAGON: return 2.0f;
                    case PokemonType.STEEL:  return 0.5f;
                    case PokemonType.FAIRY:  return 0.0f;
                }
                break;
            case PokemonType.DARK:
                switch (defender)
                {
                    case PokemonType.GHOST:
                    case PokemonType.PSYCHIC: return 2.0f;
                    case PokemonType.FIGHT:
                    case PokemonType.DARK:
                    case PokemonType.FAIRY:   return 0.5f;
                }
                break;
            case PokemonType.STEEL:
                switch (defender)
                {
                    case PokemonType.ICE:
                    case PokemonType.ROCK:
                    case PokemonType.FAIRY:  return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.WATER:
                    case PokemonType.ELECTRIC:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
            case PokemonType.FAIRY:
                switch (defender)
                {
                    case PokemonType.FIGHT:
                    case PokemonType.DRAGON:
                    case PokemonType.DARK:   return 2.0f;
                    case PokemonType.FIRE:
                    case PokemonType.POISON:
                    case PokemonType.STEEL:  return 0.5f;
                }
                break;
        }
        return 1.0f; // neutral
    }

    public void Attack(Pokemon TARGET)
    {
        Random _random = new Random();
        float roll     = (float)(_random.NextDouble() * 0.15f + 0.85f);
        int effectiveATK   = ATK;
        int effectiveSPATK = SPATK;
        int effectiveDEF   = TARGET.DEF;
        int effectiveSPDEF = TARGET.SPDEF;

        // (FIRE BONUS) +ATK and +SPATK for all active pokemon
        int fireBuff = GameBonusLogic.GetTypeBonusCount(PokemonType.FIRE) * 5;
        effectiveATK   = (int)(effectiveATK   * (1 + fireBuff / 100f));
        effectiveSPATK = (int)(effectiveSPATK * (1 + fireBuff / 100f));

        // (ROCK+STEEL BONUS) percentual DEF/SPDEF bonus, capped at 64%
        int rockCount    = GameBonusLogic.GetTypeBonusCount(PokemonType.ROCK);
        int steelCount   = GameBonusLogic.GetTypeBonusCount(PokemonType.STEEL);
        float sturdyBuff = Math.Min((rockCount + steelCount) * 0.08f, 0.64f);
        effectiveDEF   = (int)(effectiveDEF   * (1 + sturdyBuff));
        effectiveSPDEF = (int)(effectiveSPDEF * (1 + sturdyBuff));

        // (NORMAL BONUS) +ATK if attacker, +DEF if target
        int normalBuff = GameBonusLogic.GetTypeBonusCount(PokemonType.NORMAL) * 8;
        if (this.TYPE   == PokemonType.NORMAL) effectiveATK += normalBuff;
        if (TARGET.TYPE == PokemonType.NORMAL) effectiveDEF += normalBuff;

        // (DRAGON BONUS) +100% ATK and SPATK only if unique on field
        if (this.TYPE == PokemonType.DRAGON && GameBonusLogic.GetTypeBonusCount(PokemonType.DRAGON) == 1)
        {
            effectiveATK   *= 2;
            effectiveSPATK *= 2;
        }

        // (GHOST+DARK BONUS) evasion chance cancels all damage, capped at 40%
        int shadowCount = GameBonusLogic.GetTypeBonusCount(PokemonType.GHOST) + GameBonusLogic.GetTypeBonusCount(PokemonType.DARK);
        if (shadowCount > 0 && (TARGET.TYPE == PokemonType.GHOST || TARGET.TYPE == PokemonType.DARK))
        {
            int evasionChance = Math.Min(shadowCount * 5, 40);
            if (_random.Next(0, 100) < evasionChance) return;
        }

        // normal attack: pure physical — ATK vs DEF, ~8-10 hits to kill equivalent pokemon
        float atkRatio    = (float)effectiveATK / effectiveDEF;
        float totalDamage = atkRatio * 10f * roll;

        // (WATER BONUS) reduces energy cost to trigger special, capped at 50%
        int waterCount          = GameBonusLogic.GetTypeBonusCount(PokemonType.WATER);
        float waterReduction    = Math.Min(waterCount * 0.10f, 0.50f);
        int effectiveSpecialMax = (int)(this.SPECIAL_MAX * (1 - waterReduction));

        if (this.SPECIAL_COUNTER >= effectiveSpecialMax)
        {
            // special attack: pure magical — SPATK vs SPDEF, hits hard
            float spRatio       = (float)effectiveSPATK / effectiveSPDEF;
            float specialDamage = spRatio * 35f * roll;
            totalDamage = Math.Max(1, specialDamage);
            this.SPECIAL_EFFECT = true;
            SPECIAL_COUNTER     = 0;
        }
        else
        {
            this.ChargeAttack(200);
        }

        // (ELECTRIC BONUS) chance to reduce target SPEED on hit, capped at 45%
        int electricCount = GameBonusLogic.GetTypeBonusCount(PokemonType.ELECTRIC);
        if (electricCount > 0 && this.TYPE == PokemonType.ELECTRIC)
        {
            int chance = Math.Min(electricCount * 15, 45);
            if (_random.Next(0, 100) < chance)
                TARGET.SPEED = Math.Max(1, TARGET.SPEED - 15);
        }

        // (GRASS BONUS) regen HP once per second, capped at 2% max HP per tick
        if (this.TYPE == PokemonType.GRASS && GameTimeLogic.SEC_TICK)
        {
            int grassCount = GameBonusLogic.GetTypeBonusCount(PokemonType.GRASS);
            int regen      = Math.Min(grassCount * 5, (int)(this.MAX_HP * 0.02f));
            this.HP        = Math.Min(this.MAX_HP, this.HP + regen);
        }

        // (BUG BONUS) handled externally via tick system
        
        // (TYPE EFFECTIVENESS) 2x, 0.5x or 0x multiplier
        float typeMultiplier = GetTypeEffectiveness(this.TYPE, TARGET.TYPE);
        if (typeMultiplier == 0.0f) return; // immune
        totalDamage *= typeMultiplier;

        TARGET.HP -= (int)Math.Max(1, totalDamage);
    }

    #endregion

}

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
    PHYSICAL_FIGHTER,
    MAGE,
    MAGIC_FIGHTER
}