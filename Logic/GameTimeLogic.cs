using Microsoft.Xna.Framework;

namespace PokemonTFT.Logic;

public static class GameTimeLogic
{
    private const double FRAC_INTERVAL = 0.1;
    private const int FRAC_PER_SECOND  = 10;

    private static double _elapsedSecond;
    private static double _elapsedFrac;

    public static int TICK { get; private set; }
    public static int FRAC { get; private set; }
    public static double TOTAL { get; private set; }
    public static double DELTA { get; private set; }

    public static bool SEC_TICK { get; private set; }

    public static bool FRAC_TICK { get; private set; }

    public static void Update(GameTime GAME_TIME)
    {
        DELTA = GAME_TIME.ElapsedGameTime.TotalSeconds;
        TOTAL += DELTA;
        _elapsedSecond += DELTA;
        _elapsedFrac   += DELTA;

        SEC_TICK  = false;
        FRAC_TICK = false;

        if (_elapsedSecond >= 1.0)
        {
            _elapsedSecond -= 1.0;
            TICK = (TICK + 1) % 60;
            SEC_TICK = true;
        }

        if (_elapsedFrac >= FRAC_INTERVAL)
        {
            _elapsedFrac -= FRAC_INTERVAL;
            FRAC = (FRAC + 1) % FRAC_PER_SECOND;
            FRAC_TICK = true;
        }
    }

    public static bool OnSecondBoundary() => FRAC_TICK && FRAC == 0;

    #region HIT STOP
    private static double _freezeLeft;

    public static void RequestFreeze(double SECONDS)
        => _freezeLeft = System.Math.Max(_freezeLeft, SECONDS);

    public static bool COMBAT_FROZEN => _freezeLeft > 0;

    public static void TickFreeze()
    {
        if (_freezeLeft <= 0) return;
        _freezeLeft -= DELTA;
        if (_freezeLeft < 0) _freezeLeft = 0;
    }

    public static void ClearFreeze() => _freezeLeft = 0;
    #endregion
}
