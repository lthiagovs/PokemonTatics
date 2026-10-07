using System;

namespace PokemonTFT.Models;

public class Pokemon
{
    public string NAME;

    public string SPRITE;

    public int SPRITE_SLICE;

    public int MAX_HP;
    public int HP;
    public int ATK;
    public int SPATK;
    public int DEF;
    public int SPDEF;
    public int SPEED;
    public int COST;
    public int LEVEL = 1;
    public int XP;
    public int EVOLUTION_LEVEL;
    public PokemonType TYPE;
    public Pokemon? EVOLUTION;
    public string EFFECT;

    public int SPECIAL_COUNTER;
    public int SPECIAL_MAX = 1000;
    public bool SPECIAL_EFFECT;

    public Pokemon(
        string NAME, int HP, int ATK, int SPATK, int DEF, int SPDEF, int SPEED,
        PokemonType TYPE, int COST, int EVOLUTION_LEVEL,
        string EFFECT = "scratch", Pokemon? EVOLUTION = null,
        string? SPRITE = null, int SPRITE_SLICE = 32)
    {
        this.NAME            = NAME;
        this.SPRITE          = SPRITE ?? NAME;
        this.SPRITE_SLICE    = SPRITE_SLICE;
        this.HP              = HP;
        this.MAX_HP          = HP;
        this.ATK             = ATK;
        this.SPATK           = SPATK;
        this.DEF             = DEF;
        this.SPDEF           = SPDEF;
        this.SPEED           = SPEED;
        this.TYPE            = TYPE;
        this.COST            = COST;
        this.EVOLUTION       = EVOLUTION;
        this.EVOLUTION_LEVEL = EVOLUTION_LEVEL;
        this.EFFECT          = EFFECT;
    }

    #region PATHS
    public string MovesetPath  => "Pokemons/" + SPRITE + "/moveset";
    public string PortraitPath => "Pokemons/" + SPRITE + "/portrait";
    public string EffectPath   => "Effects/" + EFFECT;
    public string IconPath     => PokemonTypeAssets.IconPath(TYPE);
    #endregion

    #region IDENTITY
    public Pokemon Clone() => (Pokemon)MemberwiseClone();

    public const int XP_PER_LEVEL_STEP = 30;

    public int XPToNextLevel() => LEVEL * XP_PER_LEVEL_STEP;

    public int BaseStatTotal => MAX_HP + ATK + SPATK + DEF + SPDEF + SPEED;

    public PokemonStyle GetStyle()
    {
        bool tanky = HP > 100 || DEF > 80 || SPDEF > 80;
        if (tanky)
        {
            if (SPDEF > DEF) return PokemonStyle.MAGIC_TANK;
            if (DEF > SPDEF) return PokemonStyle.PHYSICAL_TANK;
            return SPEED > 90 ? PokemonStyle.EVASION_TANK : PokemonStyle.PHYSICAL_TANK;
        }

        if (ATK > SPATK) return PokemonStyle.PHYSICAL_FIGHTER;
        if (SPATK > ATK) return SPEED > 90 ? PokemonStyle.MAGE : PokemonStyle.MAGIC_FIGHTER;

        return SPEED > 100 ? PokemonStyle.EVASION_TANK : PokemonStyle.BALANCED;
    }
    #endregion

    #region XP
    public int GainXP(int AMOUNT)
    {
        XP += AMOUNT;
        int levels = 0;
        while (XP >= XPToNextLevel())
        {
            XP -= XPToNextLevel();
            LevelUp();
            levels++;
        }
        return levels;
    }

    public void LevelUp()
    {
        LEVEL++;
        const float GROWTH = 1.10f;
        MAX_HP = (int)(MAX_HP * GROWTH);
        HP     = MAX_HP;
        ATK    = (int)(ATK   * GROWTH);
        SPATK  = (int)(SPATK * GROWTH);
        DEF    = (int)(DEF   * GROWTH);
        SPDEF  = (int)(SPDEF * GROWTH);
        SPEED  = (int)(SPEED * GROWTH);
    }

    public bool CanEvolve => EVOLUTION != null && LEVEL >= EVOLUTION_LEVEL;

    public string? NextMovesetPath => EVOLUTION == null ? null : "Pokemons/" + EVOLUTION.SPRITE + "/moveset";
    public int NextSpriteSlice     => EVOLUTION?.SPRITE_SLICE ?? SPRITE_SLICE;
    public string NextName         => EVOLUTION?.NAME ?? NAME;

    public void Evolve()
    {
        Pokemon? next = EVOLUTION;
        if (next == null) return;

        NAME            = next.NAME;
        SPRITE          = next.SPRITE;
        SPRITE_SLICE    = next.SPRITE_SLICE;
        ATK             = next.ATK;
        SPATK           = next.SPATK;
        DEF             = next.DEF;
        SPDEF           = next.SPDEF;
        SPEED           = next.SPEED;
        MAX_HP          = next.MAX_HP;
        HP              = next.MAX_HP;
        TYPE            = next.TYPE;
        EFFECT          = next.EFFECT;
        EVOLUTION_LEVEL = next.EVOLUTION_LEVEL;
        EVOLUTION       = next.EVOLUTION;
    }
    #endregion

    #region SPECIAL
    public void ChargeSpecial(int AMOUNT) => SPECIAL_COUNTER = Math.Min(SPECIAL_MAX, SPECIAL_COUNTER + AMOUNT);

    public bool IsAlive => HP > 0;
    #endregion
}
