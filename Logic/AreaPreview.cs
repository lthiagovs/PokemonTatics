using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class AreaPreview
{
    private const double PULSE_SPEED = 4.0;
    private const float TINT_BASE = 0.14f;
    private const float TINT_SWING = 0.2f;

    private static readonly Color BUFF = new(90, 230, 120);
    private static readonly Color HARM = new(240, 80, 70);

    private static bool _active;
    private static Vector2 _origin;
    private static float _aura;
    private static bool _special;
    private static SpecialShape _shape;
    private static Vector2 _center;
    private static float _radius;
    private static Vector2 _facing;
    private static float _cone;

    public static void Clear() => _active = false;

    public static void Show(PokemonEntity UNIT, PokemonEntity? TARGET, int TILE)
    {
        if (UNIT.POKEMON == null)
        {
            _active = false;
            return;
        }

        PokemonStyle style = UNIT.POKEMON.GetStyle();
        SpecialBehaviour special = StyleBehaviour.Of(style);
        float tile = Math.Max(1, TILE);

        _origin = UNIT.Center.ToVector2();
        _aura = BattleTactics.Of(style).KIND == TacticKind.WARDEN ? Balance.WARDEN_AURA_TILES * tile : 0f;

        _shape = special.SHAPE;
        _radius = special.RADIUS_TILES * tile;
        _cone = MathF.Cos(MathHelper.ToRadians(special.CONE_DEGREES / 2f));

        Vector2 aim = TARGET != null ? TARGET.Center.ToVector2() - _origin : Vector2.Zero;
        _facing = aim.LengthSquared() > 0.001f ? Vector2.Normalize(aim) : Facing(UNIT.DIRECTION);

        _center = _shape == SpecialShape.BLAST && TARGET != null ? TARGET.Center.ToVector2() : _origin;
        _special = _radius > 0f && _shape != SpecialShape.FOCUS && (_shape != SpecialShape.BLAST || TARGET != null);

        _active = _aura > 0f || _special;
    }

    public static bool TryTint(Rectangle TILE, out Color TINT)
    {
        TINT = Color.White;
        if (!_active) return false;

        Vector2 point = TILE.Center.ToVector2();
        bool buff = _aura > 0f && Vector2.DistanceSquared(point, _origin) <= _aura * _aura;
        bool harm = _special && Reaches(point);
        if (!buff && !harm) return false;

        float pulse = 0.5f + 0.5f * MathF.Sin((float)(GameTimeLogic.TOTAL * PULSE_SPEED));
        Color color = buff && harm ? Color.Lerp(BUFF, HARM, pulse) : buff ? BUFF : HARM;

        TINT = Color.Lerp(Color.White, color, TINT_BASE + TINT_SWING * pulse);
        return true;
    }

    private static bool Reaches(Vector2 POINT)
    {
        Vector2 offset = POINT - _center;
        float distance = offset.Length();
        if (distance > _radius) return false;
        if (_shape != SpecialShape.CLEAVE || distance < 0.001f) return true;

        return Vector2.Dot(offset / distance, _facing) >= _cone;
    }

    private static Vector2 Facing(GameDirection DIRECTION) => DIRECTION switch
    {
        GameDirection.TOP          => new Vector2(0f, -1f),
        GameDirection.BOTTOM       => new Vector2(0f, 1f),
        GameDirection.LEFT         => new Vector2(-1f, 0f),
        GameDirection.RIGHT        => new Vector2(1f, 0f),
        GameDirection.TOP_LEFT     => Vector2.Normalize(new Vector2(-1f, -1f)),
        GameDirection.TOP_RIGHT    => Vector2.Normalize(new Vector2(1f, -1f)),
        GameDirection.BOTTOM_LEFT  => Vector2.Normalize(new Vector2(-1f, 1f)),
        _                          => Vector2.Normalize(new Vector2(1f, 1f))
    };
}
