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
    public PokemonType TYPE;
    public Pokemon EVOLUTION;

    public int XPToNextLevel() => (int)(LEVEL * LEVEL * 10 * 1.5f);

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

    public Pokemon(string NAME, int HP, int ATK, int SPATK, int DEF, int SPDEF, int SPEED, PokemonType TYPE, int COST, int EVOLUTION_LEVEL, Pokemon EVOLUTION = null)
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
        
    }
}

public enum PokemonType
{
    GRASS,
    FIRE,
    WATER,
    GROUND,
    ROCK,
    IRON,
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
    THUNDER,
    FIGHT
}