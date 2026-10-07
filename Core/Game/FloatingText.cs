using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Logic;

namespace PokemonTFT.Core;

public sealed class FloatingText
{
    private static readonly List<FloatingText> ITEMS = [];

    public static IReadOnlyList<FloatingText> All => ITEMS;

    public readonly string TEXT;
    public readonly Color COLOR;
    public readonly float SCALE;
    public readonly bool CENTERED;

    private readonly Vector2 _origin;
    private readonly float _rise;
    private readonly double _life;
    private double _elapsed;

    public bool DONE => _elapsed >= _life;

    private FloatingText(string TEXT, Vector2 ORIGIN, Color COLOR, float SCALE, float RISE, double LIFE, bool CENTERED)
    {
        this.TEXT     = TEXT;
        this.COLOR    = COLOR;
        this.SCALE    = SCALE;
        this.CENTERED = CENTERED;
        _origin       = ORIGIN;
        _rise         = RISE;
        _life         = LIFE;
    }

    public float Progress => _life <= 0 ? 1f : (float)System.Math.Clamp(_elapsed / _life, 0, 1);

    public Vector2 Position => new(_origin.X, _origin.Y - _rise * Progress);

    public float Alpha => Progress < 0.66f ? 1f : 1f - (Progress - 0.66f) / 0.34f;

    public static void SpawnDamage(int AMOUNT, Point WORLD_POSITION, Color COLOR, bool BIG)
    {
        ITEMS.Add(new FloatingText(
            AMOUNT.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new Vector2(WORLD_POSITION.X, WORLD_POSITION.Y),
            COLOR,
            BIG ? GameFonts.MEDIUM : GameFonts.SMALL * 2f,
            RISE: BIG ? 70f : 45f,
            LIFE: BIG ? 1.0 : 0.7,
            CENTERED: true));
    }

    public static void SpawnStatus(string TEXT, Point WORLD_POSITION, Color COLOR)
    {
        ITEMS.Add(new FloatingText(
            TEXT,
            new Vector2(WORLD_POSITION.X, WORLD_POSITION.Y),
            COLOR,
            GameFonts.SMALL * 2f,
            RISE: 40f,
            LIFE: 0.8,
            CENTERED: true));
    }

    public static void SpawnBanner(string TEXT, Color COLOR)
    {
        ITEMS.Add(new FloatingText(
            TEXT,
            new Vector2(GameRenderer.GetScreenWidth() / 2f, GameRenderer.GetScreenHeight() * 0.30f),
            COLOR,
            GameFonts.LARGE,
            RISE: 30f,
            LIFE: 1.8,
            CENTERED: true));
    }

    public static void UpdateAll()
    {
        for (int i = ITEMS.Count - 1; i >= 0; i--)
        {
            ITEMS[i]._elapsed += GameTimeLogic.DELTA;
            if (ITEMS[i].DONE) ITEMS.RemoveAt(i);
        }
    }

    public static void Clear() => ITEMS.Clear();
}
