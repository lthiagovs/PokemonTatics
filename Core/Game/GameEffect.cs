using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Logic;

namespace PokemonTFT.Core;

public enum GameEffectType
{
    MISSILE,
    TARGET,
    AREA,
    HEAL
}

public sealed class GameEffect
{
    private const double FRAME_SPEED = 0.1;

    private static readonly List<GameEffect> EFFECTS = [];

    public static IReadOnlyList<GameEffect> All => EFFECTS;

    public readonly GameEffectType TYPE;
    public readonly string PATH;
    public readonly int FRAMES;

    public readonly Color TINT;

    public readonly float SCALE;

    private readonly PokemonEntity? _target;
    private double _elapsed;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int FrameIndex { get; private set; }
    public bool DONE { get; private set; }

    private GameEffect(GameEffectType TYPE, string PATH, int FRAMES, int X, int Y, PokemonEntity? TARGET, Color TINT, float SCALE)
    {
        this.TYPE   = TYPE;
        this.PATH   = PATH;
        this.FRAMES = FRAMES;
        this.X      = X;
        this.Y      = Y;
        this.TINT   = TINT;
        this.SCALE  = SCALE;
        _target     = TARGET;
    }

    public static void Spawn(
        GameEffectType TYPE, string PATH, int FRAMES, Point POSITION,
        PokemonEntity? TARGET = null, Color? TINT = null, float SCALE = 3f)
    {
        if (string.IsNullOrEmpty(PATH) || FRAMES <= 0) return;
        EFFECTS.Add(new GameEffect(TYPE, PATH, FRAMES, POSITION.X, POSITION.Y, TARGET, TINT ?? Color.White, SCALE));
    }

    public static void Clear() => EFFECTS.Clear();

    public static void UpdateAll()
    {
        for (int i = EFFECTS.Count - 1; i >= 0; i--)
        {
            EFFECTS[i].Update();
            if (EFFECTS[i].DONE) EFFECTS.RemoveAt(i);
        }
    }

    private void Update()
    {
        if (TYPE == GameEffectType.TARGET)
        {
            if (_target == null) { DONE = true; return; }
            Rectangle bounds = _target.GetRectangle();
            X = bounds.X + bounds.Width  / 2;
            Y = bounds.Y + bounds.Height / 2;
        }

        _elapsed += GameTimeLogic.DELTA;
        if (_elapsed < FRAME_SPEED) return;

        _elapsed = 0;
        FrameIndex++;
        if (FrameIndex >= FRAMES) DONE = true;
    }
}
