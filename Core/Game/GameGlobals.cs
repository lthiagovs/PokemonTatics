namespace PokemonTFT.Core;

public enum GameState
{
    TITLE,
    GAME
}

public static class GameGlobals
{
    public const int MAX_HP   = 100;
    public const int MAX_MANA = 100;

    public static bool GAME_STARTED;
    public static int PLAYER_HP = MAX_HP;
    public static int PLAYER_MANA = Logic.Balance.MANA_START;
    public static int LEVEL = 1;
    public static GameState STATE = GameState.TITLE;

    public static void ChangeMana(int VALUE) => PLAYER_MANA = System.Math.Clamp(PLAYER_MANA + VALUE, 0, MAX_MANA);

    public static void ChangeHp(int VALUE) => PLAYER_HP = System.Math.Clamp(PLAYER_HP + VALUE, 0, MAX_HP);

    public static bool IsDefeated() => PLAYER_HP <= 0;

    public static int GetTableSize() => Logic.Balance.TableSize(LEVEL);

    public static void ResetRun()
    {
        GAME_STARTED = false;
        PLAYER_HP    = MAX_HP;
        PLAYER_MANA  = Logic.Balance.MANA_START;
        LEVEL        = 1;
        STATE        = GameState.TITLE;
    }
}
