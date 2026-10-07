using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.UI;

namespace PokemonTFT.Core;

public class PokemonEntity : GameEntity
{
    private const double HOVER_DELAY = 0.5;

    private const float IDLE_BOB      = 1.5f;
    private const double IDLE_BOB_SPEED = 2.6;
    private const float CARRY_BOB     = 4f;
    private const double CARRY_BOB_SPEED = 5.0;
    private const float CARRY_ALPHA   = 0.85f;
    private const float CARRY_SCALE   = 1.08f;
    private const float LUNGE_DISTANCE = 10f;
    private const double LUNGE_TIME   = 0.18;

    private const float CELEBRATE_HOP    = 34f;
    private const double CELEBRATE_SPEED = 7.5;

    private const float DEATH_RISE_PORTION = 0.45f;
    private const float DEATH_RISE         = 58f;
    private const float DEATH_SINK         = 64f;
    private const float DEATH_SHRINK       = 0.45f;
    private const double DEATH_SPIN_RISE   = 0.11;
    private const double DEATH_SPIN_FALL   = 0.04;

    public Pokemon? POKEMON;
    public bool ENEMY = true;
    public bool DEAD;

    public bool PURCHASED;

    public bool CARRIED;

    public Point START;

    public bool HOVERED { get; private set; }

    private PokemonEntity? _target;
    private string? _spriteKey;
    private double _hoverElapsed;
    private double _bobTime;
    private double _lungeLeft;
    private Vector2 _lungeDirection;
    private float _attackCooldown;
    private double _celebrateLeft;
    private double _deathElapsed = -1;
    private double _deathSpin;

    public PokemonEntity(int POS_X, int POS_Y, bool VISIBLE = true)
        : base(POS_X, POS_Y, GameEntityRenderConfig.DEFAULT_SLICE, GameEntityRenderConfig.DEFAULT_SLICE, VISIBLE) { }

    #region CONFIG
    public override GameEntityRenderConfig GetEntityConfig()
    {
        if (POKEMON != null && _spriteKey != POKEMON.SPRITE)
        {
            _spriteKey = POKEMON.SPRITE;
            SetEntityConfig(new GameEntityRenderConfig(POKEMON.MovesetPath, POKEMON.SPRITE_SLICE));
        }
        return base.GetEntityConfig();
    }

    public override Rectangle GetRectangle()
    {
        int size = GetEntityConfig().DrawSize;
        return new Rectangle(GetPosition().X, GetPosition().Y, size, size);
    }

    public Point Center
    {
        get
        {
            Rectangle bounds = GetRectangle();
            return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }
    }

    public Rectangle CombatBounds(float INSET)
    {
        Rectangle bounds = GetRectangle();
        int inset = (int)(bounds.Width * INSET);
        return new Rectangle(
            bounds.X + inset,
            bounds.Y + inset,
            Math.Max(1, bounds.Width  - inset * 2),
            Math.Max(1, bounds.Height - inset * 2));
    }
    #endregion

    #region TARGET & COMBAT STATE
    public void SetTarget(PokemonEntity? TARGET) => _target = TARGET;

    public PokemonEntity? GetTarget() => _target;

    public bool IsAlive => !DEAD && POKEMON is { HP: > 0 };

    public bool AttackReady => _attackCooldown <= 0;

    public void ResetAttackCooldown(float SECONDS) => _attackCooldown = SECONDS;

    public void TickAttackCooldown(double DELTA)
    {
        if (_attackCooldown > 0) _attackCooldown -= (float)DELTA;
    }

    public void MoveBy(Vector2 DELTA)
    {
        Point current = GetPosition();
        if (current != _preciseAnchor) _precise = new Vector2(current.X, current.Y);

        _precise += DELTA;
        var next = new Point((int)Math.Round(_precise.X), (int)Math.Round(_precise.Y));
        _preciseAnchor = next;
        SetPosition(next);
    }

    public void ClampInside(Rectangle AREA)
    {
        if (AREA.Width <= 0 || AREA.Height <= 0) return;

        Rectangle bounds = GetRectangle();
        int x = Math.Clamp(bounds.X, AREA.Left, Math.Max(AREA.Left, AREA.Right  - bounds.Width));
        int y = Math.Clamp(bounds.Y, AREA.Top,  Math.Max(AREA.Top,  AREA.Bottom - bounds.Height));

        if (x == bounds.X && y == bounds.Y) return;
        MoveBy(new Vector2(x - bounds.X, y - bounds.Y));
    }

    private Vector2 _precise;
    private Point _preciseAnchor = new(int.MinValue, int.MinValue);
    #endregion

    #region ANIMATION
    public void Lunge(Point TOWARDS)
    {
        Vector2 direction = new(TOWARDS.X - Center.X, TOWARDS.Y - Center.Y);
        if (direction.LengthSquared() > 0.001f) direction.Normalize();

        _lungeDirection = direction;
        _lungeLeft      = LUNGE_TIME;
    }

    public void BeginDeath()
    {
        DEAD = true;
        SetTarget(null);
        ClearOneShot();
        SetEffect(null);
        _deathElapsed = 0;
        _deathSpin    = 0;
    }

    public void Celebrate(double SECONDS)
    {
        _celebrateLeft = SECONDS;
        ClearOneShot();
        SetEffect(null);
    }

    public bool IsCelebrating => _celebrateLeft > 0;

    public void ResetRoundVisuals()
    {
        _celebrateLeft = 0;
        _deathElapsed  = -1;
        _deathSpin     = 0;
        _lungeLeft     = 0;
        RENDER_OFFSET  = Vector2.Zero;
        RENDER_ALPHA   = 1f;
        RENDER_SCALE   = 1f;
    }

    private void UpdateVisuals()
    {
        _bobTime += GameTimeLogic.DELTA;

        if (DEAD) { UpdateDeathVisuals(); return; }
        if (_celebrateLeft > 0) { UpdateCelebrationVisuals(); return; }

        Vector2 offset = Vector2.Zero;

        if (_lungeLeft > 0)
        {
            _lungeLeft -= GameTimeLogic.DELTA;
            float progress = (float)Math.Clamp(1 - _lungeLeft / LUNGE_TIME, 0, 1);
            offset += _lungeDirection * (float)Math.Sin(progress * Math.PI) * LUNGE_DISTANCE;
        }

        if (CARRIED)
        {
            offset.Y += (float)Math.Sin(_bobTime * CARRY_BOB_SPEED) * CARRY_BOB;
            RENDER_ALPHA = CARRY_ALPHA;
            RENDER_SCALE = CARRY_SCALE;
        }
        else
        {
            RENDER_ALPHA = 1f;
            RENDER_SCALE = 1f;
            if (!IS_MOVING && IsAlive) offset.Y += (float)Math.Sin(_bobTime * IDLE_BOB_SPEED) * IDLE_BOB;
        }

        RENDER_OFFSET = offset;
    }

    private void UpdateCelebrationVisuals()
    {
        _celebrateLeft -= GameTimeLogic.DELTA;

        DIRECTION    = GameDirection.BOTTOM;
        IS_MOVING    = true;
        RENDER_ALPHA = 1f;
        RENDER_SCALE = 1f;

        float hop = (float)Math.Abs(Math.Sin(_bobTime * CELEBRATE_SPEED));
        RENDER_OFFSET = new Vector2(0, -hop * CELEBRATE_HOP);

        if (_celebrateLeft <= 0) { _celebrateLeft = 0; IS_MOVING = false; RENDER_OFFSET = Vector2.Zero; }
    }

    private void UpdateDeathVisuals()
    {
        if (_deathElapsed < 0) { VISIBLE = false; return; }

        _deathElapsed += GameTimeLogic.DELTA;
        float progress = (float)Math.Clamp(_deathElapsed / Balance.DEATH_SECONDS, 0, 1);

        bool rising = progress < DEATH_RISE_PORTION;

        _deathSpin += GameTimeLogic.DELTA / (rising ? DEATH_SPIN_RISE : DEATH_SPIN_FALL);
        DIRECTION = GameDirectionExtensions.Spin((int)_deathSpin);
        IS_MOVING = true;

        float height;
        if (rising)
        {
            float p = progress / DEATH_RISE_PORTION;
            height = -DEATH_RISE * (2f * p - p * p);
            RENDER_ALPHA = 1f;
            RENDER_SCALE = 1f + 0.08f * p;
        }
        else
        {
            float p = (progress - DEATH_RISE_PORTION) / (1f - DEATH_RISE_PORTION);
            height = -DEATH_RISE + (DEATH_RISE + DEATH_SINK) * p * p;
            RENDER_ALPHA = 1f - p;
            RENDER_SCALE = 1.08f - p * DEATH_SHRINK;
        }

        RENDER_OFFSET = new Vector2(0, height);

        if (progress >= 1f) VISIBLE = false;
    }
    #endregion

    public static PokemonEntity Copy(PokemonEntity SOURCE)
    {
        return new PokemonEntity(SOURCE.GetPosition().X, SOURCE.GetPosition().Y, SOURCE.VISIBLE)
        {
            POKEMON   = SOURCE.POKEMON,
            DIRECTION = SOURCE.DIRECTION,
            IS_MOVING = SOURCE.IS_MOVING,
            ENEMY     = SOURCE.ENEMY,
            START     = SOURCE.START,
            PURCHASED = SOURCE.PURCHASED
        };
    }

    public override void Update()
    {
        base.Update();
        UpdateVisuals();

        if (DEAD)
        {
            HOVERED = false;
            GameTooltip.Hide(this);
            return;
        }

        if (CARRIED)
        {
            HOVERED = false;
            return;
        }

        HOVERED = GameMouse.IsOver(this);

        if (!HOVERED)
        {
            _hoverElapsed = 0;
            GameTooltip.Hide(this);
            return;
        }

        _hoverElapsed += GameTimeLogic.DELTA;
        if (_hoverElapsed < HOVER_DELAY || POKEMON == null) return;

        GameTooltip.Show(this, PokemonHintText.Build(POKEMON));
    }
}
