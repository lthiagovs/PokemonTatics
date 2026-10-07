using System;
using Microsoft.Xna.Framework;

namespace PokemonTFT.Logic;

public enum StatusKind
{
    BURN = 0,
    POISON = 1,
    PARALYSIS = 2
}

public sealed class StatusState
{
    private const int COUNT = 3;
    private const double TICK = 1.0;

    private readonly double[] _left = new double[COUNT];
    private double _tick;

    public bool Has(StatusKind KIND) => _left[(int)KIND] > 0;

    public bool Any => _left[0] > 0 || _left[1] > 0 || _left[2] > 0;

    public void Apply(StatusKind KIND, double SECONDS)
        => _left[(int)KIND] = Math.Max(_left[(int)KIND], SECONDS);

    public void Clear()
    {
        for (int i = 0; i < COUNT; i++) _left[i] = 0;
        _tick = 0;
    }

    public float SpeedScale => Has(StatusKind.PARALYSIS) ? Balance.PARALYSIS_SPEED_SCALE : 1f;

    public float IntervalScale => Has(StatusKind.PARALYSIS) ? Balance.PARALYSIS_INTERVAL_SCALE : 1f;

    public int Update(double DELTA, int MAX_HP)
    {
        if (!Any) return 0;

        for (int i = 0; i < COUNT; i++)
            if (_left[i] > 0) _left[i] = Math.Max(0, _left[i] - DELTA);

        _tick += DELTA;
        if (_tick < TICK) return 0;

        _tick -= TICK;

        int damage = 0;
        if (Has(StatusKind.BURN)) damage += Scaled(MAX_HP, Balance.BURN_PERCENT);
        if (Has(StatusKind.POISON)) damage += Scaled(MAX_HP, Balance.POISON_PERCENT);
        return damage;
    }

    private static int Scaled(int MAX_HP, int PERCENT) => Math.Max(1, MAX_HP * PERCENT / 100);

    private static readonly Color[] TINTS =
    [
        new(255, 86, 62),
        new(176, 84, 226),
        new(255, 222, 84)
    ];

    public Color Tint(double TIME, Color BASE)
    {
        if (!Any) return BASE;

        float pulse = 0.35f + 0.35f * (float)Math.Sin(TIME * Balance.STATUS_PULSE_SPEED);
        Color blended = BASE;

        for (int i = 0; i < COUNT; i++)
            if (_left[i] > 0) blended = Color.Lerp(blended, TINTS[i], pulse);

        return blended;
    }

    public static string Label(StatusKind KIND) => KIND switch
    {
        StatusKind.BURN => "BURN",
        StatusKind.POISON => "POISON",
        _ => "PARALYSIS"
    };

    public static string Sprite(StatusKind KIND) => KIND switch
    {
        StatusKind.BURN => "burn",
        StatusKind.POISON => "venom",
        _ => "jolt"
    };
}
