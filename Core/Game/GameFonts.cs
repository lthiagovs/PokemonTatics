using Microsoft.Xna.Framework;

namespace PokemonTFT.Core;

public static class GameFonts
{
    public const float SMALL  = 1f;
    public const float MEDIUM = 3f;
    public const float LARGE  = 4f;
    public const float TITLE  = 6f;

    public static Vector2 Measure(string TEXT, float SCALE)
        => GameRenderer.GetGameFont().MeasureString(TEXT) * SCALE;

    public static float FitScale(string TEXT, float TARGET_WIDTH, float MAX_SCALE = TITLE, float MIN_SCALE = 1f)
    {
        float width = GameRenderer.GetGameFont().MeasureString(TEXT).X;
        if (width <= 0) return MIN_SCALE;

        float scale = (float)System.Math.Floor(TARGET_WIDTH / width);
        return System.Math.Clamp(scale, MIN_SCALE, MAX_SCALE);
    }
}
