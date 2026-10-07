using System;
using System.Collections.Generic;

namespace PokemonTFT.Models;

public class Pokemon
{
    public string NAME;

    public string SPRITE;

    public int SPRITE_SLICE;

    public int SPRITE_SCALE;

    public int FOOT_Y;
    public int FOOT_W;

    public int BASE_HP;
    public int BASE_ATK;
    public int BASE_SPATK;
    public int BASE_DEF;
    public int BASE_SPDEF;
    public int BASE_SPEED;

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
    public bool LEGENDARY;
    public string LINE = string.Empty;
    public float SIZE_SCALE = 1f;
    public List<string> ITEMS = [];
    public Pokemon? EVOLUTION;

    public int SPECIAL_COUNTER;
    public int SPECIAL_MAX = 1000;

    public Pokemon(
        string NAME, int HP, int ATK, int SPATK, int DEF, int SPDEF, int SPEED,
        PokemonType TYPE, int COST, int EVOLUTION_LEVEL, Pokemon? EVOLUTION = null,
        string? SPRITE = null, int SPRITE_SLICE = 64, int SPRITE_SCALE = 2,
        int FOOT_Y = 0, int FOOT_W = 0)
    {
        this.NAME            = NAME;
        this.SPRITE          = SPRITE ?? NAME;
        this.SPRITE_SLICE    = SPRITE_SLICE;
        this.SPRITE_SCALE    = SPRITE_SCALE;
        this.FOOT_Y          = FOOT_Y;
        this.FOOT_W          = FOOT_W;
        this.TYPE            = TYPE;
        this.COST            = COST;
        this.EVOLUTION       = EVOLUTION;
        this.EVOLUTION_LEVEL = EVOLUTION_LEVEL;

        BASE_HP    = HP;
        BASE_ATK   = ATK;
        BASE_SPATK = SPATK;
        BASE_DEF   = DEF;
        BASE_SPDEF = SPDEF;
        BASE_SPEED = SPEED;

        RecalculateStats();
        this.HP = MAX_HP;
    }

    #region PATHS
    public string MovesetPath  => "Pokemons/" + SPRITE + "/moveset";
    public string PortraitPath => "Pokemons/" + SPRITE + "/portrait";
    public string IconPath     => PokemonTypeAssets.IconPath(TYPE);
    #endregion

    #region IDENTITY
    public Pokemon Clone()
    {
        var copy = (Pokemon)MemberwiseClone();
        copy.ITEMS = [.. ITEMS];
        return copy;
    }

    public int XPToNextLevel() => Logic.Balance.XPToNextLevel(LEVEL);

    public int BaseStatTotal => BASE_HP + BASE_ATK + BASE_SPATK + BASE_DEF + BASE_SPDEF + BASE_SPEED;

    public PokemonStyle GetStyle()
    {
        int offence = Math.Max(BASE_ATK, BASE_SPATK);
        int guard = Math.Max(BASE_DEF, BASE_SPDEF);

        if (BASE_SPEED >= 100 && BASE_SPEED > offence && BASE_SPEED > guard) return PokemonStyle.EVASION_TANK;

        if (guard >= offence || BASE_HP * 10 >= offence * 13)
            return BASE_SPDEF > BASE_DEF ? PokemonStyle.MAGIC_TANK : PokemonStyle.PHYSICAL_TANK;

        if (BASE_ATK > BASE_SPATK) return PokemonStyle.FIGHTER;
        if (BASE_SPATK > BASE_ATK) return BASE_SPEED > 90 ? PokemonStyle.MAGE : PokemonStyle.MAGIC_FIGHTER;

        return PokemonStyle.BALANCED;
    }
    #endregion

    #region STATS
    public void RecalculateStats()
    {
        float growth = Logic.Balance.LevelGrowth(LEVEL);
        StatModifiers bonus = Logic.ItemLogic.Modifiers(ITEMS);

        float share = MAX_HP > 0 ? Math.Clamp(HP / (float)MAX_HP, 0f, 1f) : 1f;

        MAX_HP = Scale(BASE_HP, growth * bonus.HP);
        ATK    = Scale(BASE_ATK, growth * bonus.ATK);
        SPATK  = Scale(BASE_SPATK, growth * bonus.SPATK);
        DEF    = Scale(BASE_DEF, growth * bonus.DEF);
        SPDEF  = Scale(BASE_SPDEF, growth * bonus.SPDEF);
        SPEED  = Scale(BASE_SPEED, growth * bonus.SPEED);

        SIZE_SCALE = bonus.SIZE;
        HP = Math.Clamp((int)MathF.Round(MAX_HP * share), 0, MAX_HP);
    }

    private static int Scale(int BASE, float FACTOR) => Math.Max(1, (int)MathF.Round(BASE * FACTOR));
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
        RecalculateStats();
        HP = MAX_HP;
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
        SPRITE_SCALE    = next.SPRITE_SCALE;
        FOOT_Y          = next.FOOT_Y;
        FOOT_W          = next.FOOT_W;
        TYPE            = next.TYPE;
        LEGENDARY       = next.LEGENDARY;
        EVOLUTION_LEVEL = next.EVOLUTION_LEVEL;
        EVOLUTION       = next.EVOLUTION;

        BASE_HP    = next.BASE_HP;
        BASE_ATK   = next.BASE_ATK;
        BASE_SPATK = next.BASE_SPATK;
        BASE_DEF   = next.BASE_DEF;
        BASE_SPDEF = next.BASE_SPDEF;
        BASE_SPEED = next.BASE_SPEED;

        RecalculateStats();
        HP = MAX_HP;
    }
    #endregion

    #region SPECIAL
    public void ChargeSpecial(int AMOUNT) => SPECIAL_COUNTER = Math.Min(SPECIAL_MAX, SPECIAL_COUNTER + AMOUNT);

    public bool IsAlive => HP > 0;
    #endregion
}

public readonly record struct StatModifiers(float HP, float ATK, float SPATK, float DEF, float SPDEF,
                                            float SPEED, float SIZE)
{
    public static readonly StatModifiers NONE = new(1f, 1f, 1f, 1f, 1f, 1f, 1f);
}
