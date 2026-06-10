namespace ENGINE.MODELS;

public class Pokemon
{

    public string NAME;
    public int HP;
    public int ATK;
    public int SPATK;
    public int DEF;
    public int SPDEF;
    public int SPEED;
    public Pokemon EVOLUTION;

    public Pokemon(string NAME, int HP, int ATK, int SPATK, int DEF, int SPDEF, int SPEED, Pokemon EVOLUTION = null)
    {
        this.NAME = NAME;
        this.HP = HP;
        this. ATK = ATK;
        this.SPATK = SPATK;
        this.DEF = DEF;
        this.SPDEF = SPDEF;
        this.SPEED = SPEED;
        this.EVOLUTION = EVOLUTION;
        
    }
}