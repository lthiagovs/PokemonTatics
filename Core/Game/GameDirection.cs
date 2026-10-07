using System;
using Microsoft.Xna.Framework;

namespace PokemonTFT.Core;

public enum GameDirection
{
    TOP,
    BOTTOM,
    LEFT,
    RIGHT,
    TOP_LEFT,
    TOP_RIGHT,
    BOTTOM_LEFT,
    BOTTOM_RIGHT
}

public static class GameDirectionExtensions
{
    private const float SECTOR = 45f;

    private const float HYSTERESIS = 14f;

    private static readonly GameDirection[] SECTOR_DIRECTION =
    [
        GameDirection.RIGHT,
        GameDirection.BOTTOM_RIGHT,
        GameDirection.BOTTOM,
        GameDirection.BOTTOM_LEFT,
        GameDirection.LEFT,
        GameDirection.TOP_LEFT,
        GameDirection.TOP,
        GameDirection.TOP_RIGHT
    ];

    public static GameDirection Spin(int STEP) => SECTOR_DIRECTION[(STEP % 8 + 8) % 8];

    public static (int Row, bool Flip) SpriteCell(GameDirection DIRECTION) => DIRECTION switch
    {
        GameDirection.BOTTOM       => (0, false),
        GameDirection.TOP          => (1, false),
        GameDirection.LEFT         => (2, false),
        GameDirection.RIGHT        => (2, true),
        GameDirection.TOP_LEFT     => (3, false),
        GameDirection.TOP_RIGHT    => (3, true),
        GameDirection.BOTTOM_LEFT  => (4, false),
        GameDirection.BOTTOM_RIGHT => (4, true),
        _                          => (0, false)
    };

    public static GameDirection Resolve(float DX, float DY, GameDirection CURRENT)
    {
        if (Math.Abs(DX) < 0.001f && Math.Abs(DY) < 0.001f) return CURRENT;

        float angle = Angle(DX, DY);
        float drift = Math.Abs(Wrap(angle - CenterAngle(CURRENT)));

        if (drift <= SECTOR / 2f + HYSTERESIS) return CURRENT;

        return SECTOR_DIRECTION[SectorOf(angle)];
    }

    private static float Angle(float DX, float DY) => MathHelper.ToDegrees((float)Math.Atan2(DY, DX));

    private static int SectorOf(float ANGLE) => ((int)Math.Round(ANGLE / SECTOR) % 8 + 8) % 8;

    private static float Wrap(float DEGREES)
    {
        DEGREES %= 360f;
        if (DEGREES >  180f) DEGREES -= 360f;
        if (DEGREES < -180f) DEGREES += 360f;
        return DEGREES;
    }

    private static float CenterAngle(GameDirection DIRECTION) => DIRECTION switch
    {
        GameDirection.RIGHT        =>    0f,
        GameDirection.BOTTOM_RIGHT =>   45f,
        GameDirection.BOTTOM       =>   90f,
        GameDirection.BOTTOM_LEFT  =>  135f,
        GameDirection.LEFT         =>  180f,
        GameDirection.TOP_LEFT     => -135f,
        GameDirection.TOP          =>  -90f,
        _                          =>  -45f
    };
}
