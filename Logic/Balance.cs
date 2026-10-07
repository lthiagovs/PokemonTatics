namespace PokemonTFT.Logic;

public static class Balance
{
    public const int FIELD_START         = 1;
    public const int FIELD_EVERY         = 5;
    public const float FIELD_SHARE       = 2f / 3f;

    public const int LEGENDARY_EVERY     = 10;
    public const int LEGENDARY_LEVEL_BONUS = 2;

    public const float LEVEL_GAIN        = 0.04f;
    public const int XP_BASE             = 15;
    public const int XP_STEP             = 2;
    public const int XP_ON_WIN           = 20;
    public const int XP_ON_LOSS          = 12;
    public const int DUPLICATE_XP        = 25;

    public const int OFFER_EVERY_EASY    = 1;
    public const int OFFER_EVERY_MEDIUM  = 3;
    public const int OFFER_EVERY_HARD    = 6;

    public const int MANA_START          = 12;
    public const int MANA_WIN_BASE       = 4;
    public const int MANA_WIN_PER        = 1;
    public const int MANA_LOSS_BASE      = 6;
    public const int MANA_LOSS_PER       = 1;
    public const int MANA_BOSS_BONUS     = 10;
    public const int MANA_SCALING_ROUNDS = 6;
    public const int MANA_PER_KILL       = 1;
    public const int REROLL_COST         = 2;
    public const int REROLLS_PER_ROUND   = 2;

    public const float ENEMY_LEVEL_RATE  = 0.5f;
    public const int ENEMY_LEVEL_MAX     = 40;

    public const int ENEMY_POWER_BASE = 190;
    public const float ENEMY_POWER_PER_ROUND = 10f;
    public const float ENEMY_POWER_SPREAD = 35f;

    public const float LEGENDARY_POWER = 1.1f;
    public const float LEGENDARY_HEALTH = 2f;
    public const float LEGENDARY_SIZE = 2f;
    public const int LEGENDARY_ESCORT_CUT = 1;

    public const float SHOP_LEVEL_RATE = 0.25f;

    public const int HP_LOSS_BASE         = 3;
    public const int HP_LOSS_PER_SURVIVOR = 2;

    public const float MOVE_TILES_PER_SECOND = 3f;

    public const float WALK_CYCLE_PIXELS = 72f;

    public const float SPEED_REFERENCE   = 70f;

    public const float ATTACK_INTERVAL   = 1.3f;
    public const float ATTACK_INTERVAL_MIN = 0.55f;
    public const float ATTACK_INTERVAL_MAX = 2.2f;
    public const float ATTACK_SPEED_WEIGHT = 0.5f;

    public const float DAMAGE_SCALE         = 6f;
    public const float SPECIAL_DAMAGE_SCALE = 20f;

    public const double HIT_STOP_SECONDS = 0.11;

    public const int BURN_PERCENT = 2;
    public const int POISON_PERCENT = 3;
    public const double BURN_SECONDS = 4.0;
    public const double POISON_SECONDS = 5.0;
    public const double PARALYSIS_SECONDS = 2.5;
    public const float PARALYSIS_SPEED_SCALE = 0.55f;
    public const float PARALYSIS_INTERVAL_SCALE = 1.35f;
    public const double STATUS_PULSE_SPEED = 5.5;

    public const int GIANT_HEALTH_BONUS = 15;
    public const int GIANT_SPEED_PENALTY = 10;

    public const int COMBO_PLAGUE_BONUS = 50;
    public const int COMBO_FRENZY_BONUS = 15;
    public const int COMBO_BULWARK_BONUS = 20;
    public const int COMBO_CATACLYSM_BONUS = 60;
    public const float ITEM_SPLASH_TILES = 1.3f;

    public const int BONUS_MIN_UNITS = 2;

    public const float MIN_TYPE_MULTIPLIER = 0.15f;
    public const int MAX_ITEMS_PER_POKEMON = 3;

    public const float EFFECT_SCALE = 2f;

    public const float RARITY_CURVE = 2.4f;
    public const float RARITY_UNLOCK_PER_ROUND = 0.012f;

    public const float DAMAGE_VARIANCE = 0.12f;

    public const float MELEE_CONTACT_INSET = 0.18f;
    public const float MELEE_HOLD_INSET = 0.06f;
    public const float RANGE_HOLD_SCALE = 1.12f;
    public const float ENGAGED_SECONDS = 2.5f;

    public const float ALLY_SPACING_TILES = 1.5f;

    public const float AVOIDANCE_WEIGHT = 1.1f;

    public const float SEPARATION_DAMPING = 0.5f;

    public const double CELEBRATION_SECONDS = 3.0;

    public const float OVERTIME_SECONDS = 30f;
    public const float OVERTIME_GROWTH = 0.1f;
    public const float OVERTIME_CAP = 1000f;

    public const double DEATH_SECONDS = 1.05;

    #region TACTICS
    public const float ENGAGE_SECONDS = 5f;

    public const float BRAWLER_DASH_SPEED = 2.2f;
    public const float BRAWLER_DASH_SECONDS = 0.4f;
    public const float BRAWLER_DASH_RECHARGE = 3.5f;
    public const float BRAWLER_DASH_MIN_TILES = 2f;
    public const float BRAWLER_CHARGE_WINDOW = 1.5f;
    public const int BRAWLER_CHARGE_BONUS = 50;

    public const float GUARDIAN_TAUNT_TILES = 2.2f;
    public const float GUARDIAN_TAUNT_SECONDS = 2.5f;
    public const float GUARDIAN_TAUNT_RECHARGE = 4f;
    public const float GUARDIAN_RETARGET = 2.5f;
    public const int GUARDIAN_GUARD = 10;

    public const float WARDEN_RANGE_TILES = 2.7f;
    public const float WARDEN_LEASH_TILES = 2.5f;
    public const float WARDEN_RETARGET = 1.5f;
    public const float WARDEN_HURT_SHARE = 0.85f;
    public const float WARDEN_AURA_TILES = 2.5f;
    public const int WARDEN_AURA = 25;

    public const float DANCER_DIVE_SPEED = 1.6f;
    public const float DANCER_ORBIT_TILES = 1.8f;
    public const float DANCER_ORBIT_PACE = 0.9f;
    public const float DANCER_ORBIT_FLIP = 2f;
    public const int DANCER_DODGE = 35;

    public const float SNIPER_RANGE_TILES = 4.5f;
    public const float SNIPER_THREAT_TILES = 1.6f;
    public const float SNIPER_BLINK_TILES = 2.5f;
    public const float SNIPER_BLINK_SECONDS = 0.3f;
    public const float SNIPER_BLINK_RECHARGE = 5f;
    public const float SNIPER_RETARGET = 1f;
    public const float SNIPER_SCAN_TILES = 1f;

    public const float BATTLEMAGE_RANGE_TILES = 4f;
    public const float BATTLEMAGE_SIDESTEP_TILES = 1.2f;
    public const float BATTLEMAGE_SIDESTEP_SECONDS = 0.3f;
    public const float BATTLEMAGE_RETARGET = 1.5f;
    public const float BATTLEMAGE_SCAN_TILES = 2f;

    public const float FLANKER_SWING = 0.9f;
    public const float FLANKER_NEAR_TILES = 1.2f;
    public const float FLANKER_FAR_TILES = 4f;
    public const int FLANKER_AMBUSH = 50;
    #endregion

    public static int FieldCap(int TILES) => System.Math.Max(FIELD_START, (int)(TILES * FIELD_SHARE));

    public static int TableSize(int LEVEL, int CAP)
        => System.Math.Clamp(FIELD_START + (LEVEL - 1) / FIELD_EVERY, FIELD_START, System.Math.Max(FIELD_START, CAP));

    public static bool IsBossRound(int LEVEL) => LEVEL > 0 && LEVEL % LEGENDARY_EVERY == 0;

    public static int EnemyLevel(int LEVEL)
    {
        int level = System.Math.Clamp(1 + (int)((LEVEL - 1) * ENEMY_LEVEL_RATE), 1, ENEMY_LEVEL_MAX);
        return IsBossRound(LEVEL) ? level + LEGENDARY_LEVEL_BONUS : level;
    }

    public static int ManaReward(int LEVEL, bool WON)
    {
        int scaled = System.Math.Min(LEVEL, MANA_SCALING_ROUNDS);
        int reward = WON
            ? MANA_WIN_BASE + scaled * MANA_WIN_PER
            : MANA_LOSS_BASE + scaled * MANA_LOSS_PER;

        return WON && IsBossRound(LEVEL) ? reward + MANA_BOSS_BONUS : reward;
    }

    public static int DefeatDamage(int LEVEL, int SURVIVORS)
        => HP_LOSS_BASE + System.Math.Max(0, SURVIVORS) * HP_LOSS_PER_SURVIVOR + LEVEL / FIELD_EVERY;

    public static float EnemyPower(int LEVEL)
        => ENEMY_POWER_BASE + ENEMY_POWER_PER_ROUND * System.Math.Max(0, LEVEL - 1);

    public static int ShopLevel(int LEVEL) => 1 + (int)(System.Math.Max(0, LEVEL - 1) * SHOP_LEVEL_RATE);

    public static float OvertimeScale(float SECONDS)
        => SECONDS <= OVERTIME_SECONDS
            ? 1f
            : System.Math.Min(OVERTIME_CAP, System.MathF.Pow(1f + OVERTIME_GROWTH, SECONDS - OVERTIME_SECONDS));

    public static float LevelGrowth(int LEVEL) => 1f + System.Math.Max(0, LEVEL - 1) * LEVEL_GAIN;

    public static int XPToNextLevel(int LEVEL) => XP_BASE + System.Math.Max(1, LEVEL) * XP_STEP;

    public static float AttackInterval(int SPEED)
    {
        float speed = System.Math.Max(1, SPEED);
        float scale = System.MathF.Pow(SPEED_REFERENCE / speed, ATTACK_SPEED_WEIGHT);
        return System.Math.Clamp(ATTACK_INTERVAL * scale, ATTACK_INTERVAL_MIN, ATTACK_INTERVAL_MAX);
    }

    public static float MoveTiles(int SPEED)
    {
        float speed = System.Math.Max(1, SPEED);
        return System.Math.Clamp(MOVE_TILES_PER_SECOND * (speed / SPEED_REFERENCE),
            MOVE_TILES_PER_SECOND * 0.55f, MOVE_TILES_PER_SECOND * 1.7f);
    }
}
