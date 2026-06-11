using System;
using Microsoft.Xna.Framework;

public enum RenderEffectType
{
    SHAKE,
    FADE_IN,
    FADE_OUT,
    FLASH
}

public class RenderEffect
{
    public RenderEffectType TYPE;
    public float DURATION;
    public float ELAPSED  = 0;
    public bool  DONE     = false;
    public float INTENSITY = 1f;

    public Vector2 OFFSET  = Vector2.Zero;
    public float   ALPHA   = 1f;
    public Color  TINT   = Color.White;

    private static Random _random = new Random();

    public RenderEffect(RenderEffectType type, float duration, float intensity = 1f)
    {
        TYPE      = type;
        DURATION  = duration;
        INTENSITY = intensity;

        if(TYPE == RenderEffectType.FADE_IN)  ALPHA = 0f;
        if(TYPE == RenderEffectType.FADE_OUT) ALPHA = 1f;
    }

    public void Update(double delta)
    {
        if(DONE) return;

        ELAPSED += (float)delta;
        float t  = Math.Clamp(ELAPSED / DURATION, 0f, 1f);

        switch(TYPE)
        {
            case RenderEffectType.SHAKE:
                if(t < 1f)
                {
                    float strength = INTENSITY * (1f - t);
                    OFFSET = new Vector2(
                        (_random.NextSingle() * 2f - 1f) * strength,
                        (_random.NextSingle() * 2f - 1f) * strength
                    );
                }
                else OFFSET = Vector2.Zero;
                break;

            case RenderEffectType.FADE_IN:
                ALPHA = t;
                break;

            case RenderEffectType.FADE_OUT:
                ALPHA = 1f - t;
                break;

            case RenderEffectType.FLASH:
                float pulse = t < 0.5f ? 1f - (t * 2f) : (t - 0.5f) * 2f;
                ALPHA  = 1f;
                TINT   = new Color(1f, 1f - pulse * 0.6f, 1f - pulse * 0.6f);
                break;
        }

        if(t >= 1f) DONE = true;
    }
}