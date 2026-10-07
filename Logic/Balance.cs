namespace PokemonTFT.Logic;

public static class Balance
{
    public const int TABLE_SIZE_BASE     = 2;
    public const int TABLE_SIZE_PER      = 3;
    public const int TABLE_SIZE_MAX      = 7;

    public const int XP_ON_WIN           = 20;
    public const int XP_ON_LOSS          = 10;

    public const int MANA_START          = 10;
    public const int MANA_ON_WIN         = 5;
    public const int MANA_ON_LOSS        = 3;
    public const int MANA_PER_KILL       = 1;
    public const int REROLL_COST         = 2;

    public const int ENEMY_COUNT_PER     = 3;
    public const int ENEMY_COUNT_MAX     = 7;

    public const float ENEMY_LEVEL_RATE  = 0.5f;

    public const int ENEMY_STAT_BAND_BASE = 320;
    public const int ENEMY_STAT_BAND_PER  = 18;

    public const int HP_LOSS_ON_DEFEAT   = 10;

    public const float MOVE_SPEED_BASE   = 135f;

    public const float SPEED_REFERENCE   = 70f;

    public const float ATTACK_INTERVAL   = 1.3f;
    public const float ATTACK_INTERVAL_MIN = 0.55f;
    public const float ATTACK_INTERVAL_MAX = 2.2f;

    public const float DAMAGE_SCALE         = 6f;
    public const float SPECIAL_DAMAGE_SCALE = 20f;

    public const double HIT_STOP_SECONDS = 0.11;

    public const float DAMAGE_VARIANCE = 0.12f;

    public const float MELEE_CONTACT_INSET = 0f;

    public const float ALLY_SPACING_TILES = 0.9f;

    public const float AVOIDANCE_WEIGHT = 1.1f;

    public const float SEPARATION_DAMPING = 0.5f;

    public const double CELEBRATION_SECONDS = 3.0;

    public const double DEATH_SECONDS = 1.05;

    public static int TableSize(int LEVEL)
        => System.Math.Min(TABLE_SIZE_BASE + (LEVEL - 1) / TABLE_SIZE_PER, TABLE_SIZE_MAX);

    public static int EnemyCount(int LEVEL)
        => System.Math.Clamp(1 + (LEVEL - 1) / ENEMY_COUNT_PER, 1, ENEMY_COUNT_MAX);

    public static int EnemyLevel(int LEVEL)
        => System.Math.Max(1, 1 + (int)((LEVEL - 1) * ENEMY_LEVEL_RATE));

    public static int EnemyStatBand(int LEVEL)
        => ENEMY_STAT_BAND_BASE + (LEVEL - 1) * ENEMY_STAT_BAND_PER;

    #region COMBATE POR ESTILO
    public static float AttackRange(Models.PokemonStyle STYLE) => STYLE switch
    {
        Models.PokemonStyle.MAGE           => 280f,
        Models.PokemonStyle.MAGIC_FIGHTER  => 190f,
        Models.PokemonStyle.MAGIC_TANK     => 130f,
        Models.PokemonStyle.EVASION_TANK   => 110f,
        _                                  => 0f
    };

    public static float AttackInterval(int SPEED)
    {
        float speed = System.Math.Max(1, SPEED);
        return System.Math.Clamp(ATTACK_INTERVAL * (SPEED_REFERENCE / speed), ATTACK_INTERVAL_MIN, ATTACK_INTERVAL_MAX);
    }

    public static float MoveSpeed(int SPEED)
    {
        float speed = System.Math.Max(1, SPEED);
        return System.Math.Clamp(MOVE_SPEED_BASE * (speed / SPEED_REFERENCE), MOVE_SPEED_BASE * 0.55f, MOVE_SPEED_BASE * 1.7f);
    }
    #endregion
}
