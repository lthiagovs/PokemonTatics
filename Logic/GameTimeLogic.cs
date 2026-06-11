using Microsoft.Xna.Framework;

public static class GameTimeLogic
{
    private static double _elapsed     = 0;
    private static double _elapsedFrac = 0;
    private static int    _lastTick     = -1;
    private static int    _lastFrac     = -1;

    public static int    TICK  = 0;
    public static int    FRAC  = 0;
    public static double TOTAL = 0;

    public static bool FRAC_TICK  = false;
    public static bool SEC_TICK   = false;
    public static double DELTA = 0;

    public static void Update(GameTime gameTime)
    {
        DELTA = gameTime.ElapsedGameTime.TotalSeconds;
        TOTAL        += DELTA;
        _elapsed     += DELTA;
        _elapsedFrac += DELTA;

        SEC_TICK  = false;
        FRAC_TICK = false;

        if(_elapsed >= 1.0)     { _elapsed -= 1.0;     TICK = (TICK + 1) % 60; SEC_TICK  = true; }
        if(_elapsedFrac >= 0.1) { _elapsedFrac -= 0.1; FRAC = (FRAC + 1) % 10; FRAC_TICK = true; }
    }

    public static bool OnTick(int every = 1)
    {
        if(TICK % every == 0 && TICK != _lastTick) { _lastTick = TICK; return true; }
        return false;
    }

    public static bool OnFrac(int every = 1)
    {
        if(FRAC % every == 0 && FRAC != _lastFrac) { _lastFrac = FRAC; return true; }
        return false;
    }
}