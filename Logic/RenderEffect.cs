using System;
using Microsoft.Xna.Framework;

namespace PokemonTFT.Logic;

public enum RenderEffectType
{
    SHAKE,
    FADE_IN,
    FADE_OUT,
    FLASH,

    IMPACT
}

public sealed class RenderEffect
{
    public readonly RenderEffectType TYPE;
    public readonly float DURATION;
    public readonly float INTENSITY;

    public float ELAPSED { get; private set; }
    public bool DONE { get; private set; }

    public Vector2 OFFSET { get; private set; } = Vector2.Zero;
    public float ALPHA { get; private set; } = 1f;
    public Color TINT { get; private set; } = Color.White;

    public float SCALE { get; private set; } = 1f;

    private readonly Color _accent;

    public RenderEffect(RenderEffectType TYPE, float DURATION, float INTENSITY = 1f, Color? ACCENT = null)
    {
        this.TYPE      = TYPE;
        this.DURATION  = DURATION > 0 ? DURATION : 0.001f;
        this.INTENSITY = INTENSITY;
        _accent        = ACCENT ?? Color.White;

        if (TYPE == RenderEffectType.FADE_IN)  ALPHA = 0f;
        if (TYPE == RenderEffectType.FADE_OUT) ALPHA = 1f;
    }

    public void Update(double DELTA)
    {
        if (DONE) return;

        ELAPSED += (float)DELTA;
        float t = Math.Clamp(ELAPSED / DURATION, 0f, 1f);

        switch (TYPE)
        {
            case RenderEffectType.SHAKE:
                if (t < 1f)
                {
                    float strength = INTENSITY * (1f - t);
                    OFFSET = new Vector2(
                        (Random.Shared.NextSingle() * 2f - 1f) * strength,
                        (Random.Shared.NextSingle() * 2f - 1f) * strength);
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
                float pulse = t < 0.5f ? 1f - t * 2f : (t - 0.5f) * 2f;
                ALPHA = 1f;
                TINT  = new Color(1f, 1f - pulse * 0.6f, 1f - pulse * 0.6f);
                break;

            case RenderEffectType.IMPACT:

                float decay = 1f - t;
                float jolt  = INTENSITY * decay;

                OFFSET = new Vector2(
                    (Random.Shared.NextSingle() * 2f - 1f) * jolt,
                    (Random.Shared.NextSingle() * 2f - 1f) * jolt);

                float blink = (float)Math.Abs(Math.Sin(t * Math.PI * 3.0)) * decay;
                TINT  = Color.Lerp(Color.White, _accent, blink);
                SCALE = 1f + 0.28f * decay;
                ALPHA = 1f;
                break;
        }

        if (t >= 1f) DONE = true;
    }
}
