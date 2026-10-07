using Microsoft.Xna.Framework;

namespace PokemonTFT.Logic;

public static class GameTimeLogic
{
    private static double _elapsedSecond;

    public static double TOTAL { get; private set; }
    public static double DELTA { get; private set; }

    public static bool SEC_TICK { get; private set; }

    public static void Update(GameTime GAME_TIME)
    {
        DELTA = GAME_TIME.ElapsedGameTime.TotalSeconds;
        TOTAL += DELTA;
        _elapsedSecond += DELTA;

        SEC_TICK = false;
        if (_elapsedSecond < 1.0) return;

        _elapsedSecond -= 1.0;
        SEC_TICK = true;
    }

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
