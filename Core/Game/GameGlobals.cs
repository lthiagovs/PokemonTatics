public static class GameGlobals{

    //GLOBALS
    public static bool GAME_STARTED = false;
    public static int PLAYER_HP = 100;
    public static int PLAYER_MANA = 10;
    public static int LEVEL = 1;

    public static void ChangeMana(int VALUE)
    {
        GameGlobals.PLAYER_MANA += VALUE;
        if(GameGlobals.PLAYER_MANA > 100) GameGlobals.PLAYER_MANA = 100;
        if(GameGlobals.PLAYER_MANA < 0)   GameGlobals.PLAYER_MANA = 0;
    }
    public static void ChangeHp(int VALUE)
    {
        GameGlobals.PLAYER_HP += VALUE;
        if(GameGlobals.PLAYER_HP > 100) GameGlobals.PLAYER_HP = 100;
        if(GameGlobals.PLAYER_HP < 0)   GameGlobals.PLAYER_HP = 0;
    }

}