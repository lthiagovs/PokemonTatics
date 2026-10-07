using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.UI;

namespace PokemonTFT.Screens;

public static class EvolutionScene
{
    #region TEMPOS (segundos)
    private const double DARKEN  = 1.3;
    private const double WALK    = 1.5;
    private const double CHARGE  = 2.0;
    private const double BURST   = 3.0;
    private const double REVEAL  = 1.6;
    private const double RESTORE = 0.6;

    private const double T_WALK    = DARKEN;
    private const double T_CHARGE  = T_WALK + WALK;
    private const double T_BURST   = T_CHARGE + CHARGE;
    private const double T_REVEAL  = T_BURST + BURST;
    private const double T_RESTORE = T_REVEAL + REVEAL;
    private const double T_END     = T_RESTORE + RESTORE;
    #endregion

    #region APARENCIA
    private const float BOARD_SCALE = GameEntityRenderConfig.DEFAULT_SCALE;
    private const float HERO_SCALE  = 7f;

    private const int CELLS = 8;

    private const float SCATTER_MIN = 70f;
    private const float SCATTER_MAX = 230f;

    private const int RAY_COUNT = 14;
    private const double WALK_FRAME_TIME = 0.18;
    private static readonly int[] WALK_FRAMES = [1, 0, 1, 2];
    #endregion

    private sealed class Entry
    {
        public required PokemonEntity ENTITY;
        public required string FROM_PATH;
        public required int FROM_SLICE;
        public required string TO_PATH;
        public required int TO_SLICE;
        public required string TO_NAME;
        public required Color ACCENT;
    }

    private static readonly Queue<Entry> QUEUE = [];

    private static Entry? _current;
    private static double _elapsed;
    private static bool _applied;
    private static Vector2 _start;
    private static GameDirection _walkDirection = GameDirection.BOTTOM;

    public static bool IsActive => _current != null;

    #region FILA
    public static void EnqueueReady(IReadOnlyList<PokemonEntity> PLAYER_TEAM)
    {
        for (int i = 0; i < PLAYER_TEAM.Count; i++) Enqueue(PLAYER_TEAM[i]);
    }

    public static void Enqueue(PokemonEntity ENTITY)
    {
        Pokemon? pokemon = ENTITY.POKEMON;
        if (ENTITY.ENEMY || pokemon is not { CanEvolve: true }) return;
        if (pokemon.NextMovesetPath is not { } nextPath) return;

        foreach (Entry queued in QUEUE)
            if (ReferenceEquals(queued.ENTITY, ENTITY)) return;

        if (_current != null && ReferenceEquals(_current.ENTITY, ENTITY)) return;

        QUEUE.Enqueue(new Entry
        {
            ENTITY     = ENTITY,
            FROM_PATH  = pokemon.MovesetPath,
            FROM_SLICE = pokemon.SPRITE_SLICE,
            TO_PATH    = nextPath,
            TO_SLICE   = pokemon.NextSpriteSlice,
            TO_NAME    = pokemon.NextName,
            ACCENT     = PokemonTypeColors.Of(pokemon.TYPE)
        });

        if (_current == null) StartNext();
    }

    public static void Clear()
    {
        Finish();
        QUEUE.Clear();
    }

    private static void StartNext()
    {
        if (!QUEUE.TryDequeue(out Entry? next)) { _current = null; return; }

        _current = next;
        _elapsed = 0;
        _applied = false;

        GameMouse.ClearCarry();
        GameModal.Close();

        Rectangle bounds = next.ENTITY.GetRectangle();
        _start = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

        Vector2 target = ScreenCenter();
        _walkDirection = GameDirectionExtensions.Resolve(target.X - _start.X, target.Y - _start.Y, GameDirection.BOTTOM);

        next.ENTITY.VISIBLE = false;
    }

    private static void Finish()
    {
        if (_current != null) _current.ENTITY.VISIBLE = true;
        _current = null;
        _elapsed = 0;
        _applied = false;
    }
    #endregion

    #region UPDATE
    public static void Update()
    {
        if (_current is not { } entry) return;

        _elapsed += GameTimeLogic.DELTA;

        if (!_applied && _elapsed >= T_BURST + BURST * 0.5)
        {
            _applied = true;
            entry.ENTITY.POKEMON?.Evolve();
            GameMusic.PlayLevelUp();
        }

        if (_elapsed < T_END) return;

        PokemonEntity finished = entry.ENTITY;
        Finish();

        if (finished.POKEMON is { CanEvolve: true }) Enqueue(finished);
        else StartNext();
    }
    #endregion

    #region RENDER
    public static void Render()
    {
        if (_current is not { } entry) return;

        GameRenderer.FillScreen(Color.Black * Darkness());

        Vector2 center = StarCenter();
        float scale    = StarScale();

        if (_elapsed >= T_CHARGE) DrawRays(entry, center);

        if (_elapsed < T_BURST) DrawWholeSprite(entry, center, scale, FROM_FORM: true);
        else if (_elapsed < T_REVEAL) DrawDissolve(entry, center, scale);
        else DrawWholeSprite(entry, center, scale, FROM_FORM: false);

        DrawCaption(entry, center, scale);
    }

    private static float Darkness()
    {
        if (_elapsed < DARKEN)    return (float)(_elapsed / DARKEN);
        if (_elapsed < T_RESTORE) return 1f;
        return 1f - (float)Math.Clamp((_elapsed - T_RESTORE) / RESTORE, 0, 1);
    }

    private static Vector2 ScreenCenter()
        => new(GameRenderer.GetScreenWidth() / 2f, GameRenderer.GetScreenHeight() / 2f);

    private static Vector2 StarCenter()
    {
        if (_elapsed < T_WALK) return _start;
        if (_elapsed >= T_CHARGE) return ScreenCenter();

        float t = (float)((_elapsed - T_WALK) / WALK);
        return Vector2.Lerp(_start, ScreenCenter(), SmoothStep(t));
    }

    private static float StarScale()
    {
        if (_elapsed < T_WALK) return BOARD_SCALE;
        if (_elapsed >= T_CHARGE) return HERO_SCALE;

        float t = (float)((_elapsed - T_WALK) / WALK);
        return MathHelper.Lerp(BOARD_SCALE, HERO_SCALE, SmoothStep(t));
    }

    private static Vector2 ChargeShake()
    {
        if (_elapsed < T_CHARGE || _elapsed >= T_BURST) return Vector2.Zero;

        float t = (float)((_elapsed - T_CHARGE) / CHARGE);
        float strength = t * t * 9f;
        return new Vector2(
            (float)Math.Sin(_elapsed * 47.0) * strength,
            (float)Math.Cos(_elapsed * 39.0) * strength);
    }

    private static void DrawWholeSprite(Entry ENTRY, Vector2 CENTER, float SCALE, bool FROM_FORM)
    {
        string path = FROM_FORM ? ENTRY.FROM_PATH : ENTRY.TO_PATH;
        int slice   = FROM_FORM ? ENTRY.FROM_SLICE : ENTRY.TO_SLICE;

        bool walking = _elapsed >= T_WALK && _elapsed < T_CHARGE;
        GameDirection direction = walking ? _walkDirection : GameDirection.BOTTOM;
        (int row, bool flip) = GameDirectionExtensions.SpriteCell(direction);

        int column = walking
            ? WALK_FRAMES[(int)(_elapsed / WALK_FRAME_TIME) % WALK_FRAMES.Length]
            : 1;

        Vector2 offset = ChargeShake();
        int size = (int)(slice * SCALE);

        var dest = new Rectangle(
            (int)(CENTER.X + offset.X - size / 2f),
            (int)(CENTER.Y + offset.Y - size / 2f),
            size, size);

        Color tint = Color.White;

        if (_elapsed >= T_CHARGE && _elapsed < T_BURST)
        {
            float t = (float)((_elapsed - T_CHARGE) / CHARGE);
            tint = Color.Lerp(Color.White, ENTRY.ACCENT, t * 0.85f);
        }

        GameRenderer.DrawSprite(path, dest, new Rectangle(column * slice, row * slice, slice, slice), tint, flip);
    }

    private static void DrawDissolve(Entry ENTRY, Vector2 CENTER, float SCALE)
    {
        float t = (float)((_elapsed - T_BURST) / BURST);
        bool dispersing = t < 0.5f;
        float phase = dispersing ? t / 0.5f : (t - 0.5f) / 0.5f;

        string path = dispersing ? ENTRY.FROM_PATH : ENTRY.TO_PATH;
        int slice   = dispersing ? ENTRY.FROM_SLICE : ENTRY.TO_SLICE;

        (int row, bool flip) = GameDirectionExtensions.SpriteCell(GameDirection.BOTTOM);

        int sourceCell = Math.Max(1, slice / CELLS);
        int size       = (int)(slice * SCALE);
        int drawCell   = Math.Max(1, (int)(size / (float)CELLS)) + 1;

        float travel = dispersing ? EaseIn(phase) : 1f - EaseOut(phase);
        float alpha  = dispersing ? 1f - phase : phase;

        float left = CENTER.X - size / 2f;
        float top  = CENTER.Y - size / 2f;

        for (int cy = 0; cy < CELLS; cy++)
        {
            for (int cx = 0; cx < CELLS; cx++)
            {
                int index = cy * CELLS + cx;

                float angle    = Hash(index) * MathHelper.TwoPi;
                float distance = MathHelper.Lerp(SCATTER_MIN, SCATTER_MAX, Hash(index + 977));
                var scatter    = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance * travel;

                var dest = new Rectangle(
                    (int)(left + cx * (size / (float)CELLS) + scatter.X),
                    (int)(top  + cy * (size / (float)CELLS) + scatter.Y),
                    drawCell, drawCell);

                var source = new Rectangle(
                    slice + cx * sourceCell,
                    row * slice + cy * sourceCell,
                    sourceCell, sourceCell);

                Color tint = Color.Lerp(Color.White, ENTRY.ACCENT, travel) * alpha;

                GameRenderer.DrawSprite(path, dest, source, tint, flip);
            }
        }

        float flash = 1f - Math.Abs(t - 0.5f) / 0.5f;
        if (flash > 0f) GameRenderer.FillScreen(Color.White * (flash * flash * 0.85f));
    }

    private static void DrawRays(Entry ENTRY, Vector2 CENTER)
    {
        float intensity;
        if (_elapsed < T_BURST)        intensity = (float)((_elapsed - T_CHARGE) / CHARGE);
        else if (_elapsed < T_RESTORE) intensity = 1f;
        else                           intensity = 1f - (float)Math.Clamp((_elapsed - T_RESTORE) / RESTORE, 0, 1);

        if (intensity <= 0f) return;

        float spin   = (float)_elapsed * 1.6f;
        float length = 260f + 340f * intensity;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            float angle = spin + MathHelper.TwoPi * i / RAY_COUNT;
            float wobble = 0.6f + 0.4f * (float)Math.Sin(_elapsed * 4.0 + i);

            GameRenderer.DrawRay(CENTER, angle, length * wobble, 6f + 10f * intensity,
                ENTRY.ACCENT * (0.22f * intensity));
        }
    }

    private const float CAPTION_Y = 0.74f;
    private const float CAPTION_GAP = 14f;

    private static void DrawCaption(Entry ENTRY, Vector2 CENTER, float SCALE)
    {
        float width = GameRenderer.GetScreenWidth();
        float height = GameRenderer.GetScreenHeight();

        float nameH = GameFonts.Measure(ENTRY.TO_NAME, GameFonts.LARGE).Y;
        float tagH = GameFonts.Measure("EVOLVED!", GameFonts.MEDIUM).Y;

        float tagY = height * CAPTION_Y;
        float nameY = tagY - CAPTION_GAP - nameH;

        if (_elapsed >= T_CHARGE && _elapsed < T_BURST)
        {
            float t = (float)((_elapsed - T_CHARGE) / CHARGE);
            GameRenderer.DrawCenteredText("EVOLVING...", width / 2f, tagY,
                Color.White * Math.Min(1f, t * 2f), GameFonts.MEDIUM);
            return;
        }

        if (_elapsed < T_REVEAL) return;

        float reveal = (float)Math.Clamp((_elapsed - T_REVEAL) / 0.35, 0, 1);
        float fade   = _elapsed < T_RESTORE ? 1f : 1f - (float)Math.Clamp((_elapsed - T_RESTORE) / RESTORE, 0, 1);
        float alpha  = reveal * fade;

        GameRenderer.DrawCenteredText(ENTRY.TO_NAME.ToUpperInvariant(), width / 2f, nameY,
            ENTRY.ACCENT * alpha, GameFonts.LARGE);

        GameRenderer.DrawCenteredText("EVOLVED!", width / 2f, tagY,
            Color.White * alpha, GameFonts.MEDIUM);
    }
    #endregion

    #region MATH
    private static float Hash(int SEED)
    {
        double value = Math.Sin(SEED * 127.1) * 43758.5453;
        return (float)(value - Math.Floor(value));
    }

    private static float SmoothStep(float T)
    {
        T = Math.Clamp(T, 0f, 1f);
        return T * T * (3f - 2f * T);
    }

    private static float EaseIn(float T)  => Math.Clamp(T, 0f, 1f) * Math.Clamp(T, 0f, 1f);

    private static float EaseOut(float T)
    {
        float t = 1f - Math.Clamp(T, 0f, 1f);
        return 1f - t * t;
    }
    #endregion
}
