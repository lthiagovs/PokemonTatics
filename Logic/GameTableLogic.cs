using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Models;
using PokemonTFT.Screens;
using PokemonTFT.Table;
using PokemonTFT.UI;

namespace PokemonTFT.Logic;

public static class GameTableLogic
{
    private const int CHARGE_PER_SECOND = 60;

    private static readonly Color COLOR_SUPER   = new(255, 170,  60);
    private static readonly Color COLOR_RESIST  = new(170, 170, 170);
    private static readonly Color COLOR_NORMAL  = Color.White;
    private static readonly Color COLOR_VICTORY = new(140, 240, 140);
    private static readonly Color COLOR_DEFEAT  = new(240, 120, 120);

    private static readonly List<PokemonEntity> ALL = [];
    private static readonly List<PokemonEntity> LIVE = [];
    private static readonly List<PokemonEntity> PLAYERS = [];
    private static readonly List<PokemonEntity> ENEMIES = [];
    private static readonly TypeBonusSnapshot BONUS = new();

    #region CACHE
    public static void RefreshCaches()
    {
        ALL.Clear();
        LIVE.Clear();
        PLAYERS.Clear();
        ENEMIES.Clear();
        BONUS.Clear();

        List<GameElement> entities = GameTableElement.GetTableElements();
        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not PokemonEntity pokemon) continue;

            ALL.Add(pokemon);
            if (!pokemon.IsAlive) continue;

            LIVE.Add(pokemon);
            if (pokemon.ENEMY) ENEMIES.Add(pokemon);
            else
            {
                PLAYERS.Add(pokemon);
                if (pokemon.POKEMON != null) BONUS.Add(pokemon.POKEMON.TYPE);
            }
        }

        GameBonusLogic.SNAPSHOT = BONUS;
    }

    public static IReadOnlyList<PokemonEntity> GetPlayerPokemons() => PLAYERS;

    public static int GetPlayerPokemonsCount() => PLAYERS.Count;

    public static PokemonEntity? GetPlayerPokemon(string NAME)
    {
        for (int i = 0; i < PLAYERS.Count; i++)
            if (string.Equals(PLAYERS[i].POKEMON?.NAME, NAME, StringComparison.Ordinal)) return PLAYERS[i];
        return null;
    }
    #endregion

    #region FRAME
    public static void Update()
    {
        RefreshCaches();
        HandleSell();

        for (int i = 0; i < ALL.Count; i++) ALL[i].Update();

        GameTimeLogic.TickFreeze();
        GameTable.UpdateFlash(GameTimeLogic.DELTA);

        if (GameGlobals.GAME_STARTED) UpdateRun();
        else UpdateIdle();
    }

    private static void UpdateIdle()
    {
        FaceIdleDirections();
        for (int i = 0; i < LIVE.Count; i++)
        {
            LIVE[i].SetTarget(null);
            LIVE[i].IS_MOVING = false;
        }
    }

    private static void UpdateRun()
    {
        if (_celebrationLeft > 0)
        {
            _celebrationLeft -= GameTimeLogic.DELTA;
            if (_celebrationLeft <= 0)
            {
                _celebrationLeft = 0;
                FinishRound();
            }
            return;
        }

        for (int i = 0; i < LIVE.Count; i++) LIVE[i].TickAttackCooldown(GameTimeLogic.DELTA);

        if (GameTimeLogic.COMBAT_FROZEN) return;

        AssignTargets();
        MoveAndFight();
        SeparateAllies();
        DetectRoundEnd();
    }
    #endregion

    #region DRAG
    private static void HandleSell()
    {
        if (!GameMouse.RightPressed()) return;

        if (GameMouse.GetCarry() is PokemonEntity { PURCHASED: true, POKEMON: not null } sold)
        {
            GameGlobals.ChangeMana(sold.POKEMON.COST);
            FloatingText.SpawnStatus($"+{sold.POKEMON.COST}", GameMouse.GetPos(), new Color(120, 200, 255));
        }

        GameMouse.ClearCarry();
    }
    #endregion

    #region TARGETS
    private static void AssignTargets()
    {
        for (int i = 0; i < LIVE.Count; i++)
        {
            PokemonEntity pokemon = LIVE[i];
            if (pokemon.GetTarget() is { IsAlive: true }) continue;
            pokemon.SetTarget(FindClosestEnemy(pokemon));
        }
    }

    private static PokemonEntity? FindClosestEnemy(PokemonEntity POKEMON)
    {
        List<PokemonEntity> candidates = POKEMON.ENEMY ? PLAYERS : ENEMIES;
        if (candidates.Count == 0) return null;

        PokemonEntity closest = candidates[0];
        long bestDistance = SquaredDistance(POKEMON.Center, closest.Center);

        for (int i = 1; i < candidates.Count; i++)
        {
            long distance = SquaredDistance(POKEMON.Center, candidates[i].Center);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            closest = candidates[i];
        }

        return closest;
    }

    private static long SquaredDistance(Point A, Point B)
    {
        long dx = B.X - A.X;
        long dy = B.Y - A.Y;
        return dx * dx + dy * dy;
    }

    private static bool InAttackRange(PokemonEntity ATTACKER, PokemonEntity TARGET)
    {
        float range = ATTACKER.POKEMON == null ? 0f : Balance.AttackRange(ATTACKER.POKEMON.GetStyle());

        if (range <= 0f)
        {
            return ATTACKER.CombatBounds(Balance.MELEE_CONTACT_INSET)
                .Intersects(TARGET.CombatBounds(Balance.MELEE_CONTACT_INSET));
        }

        return SquaredDistance(ATTACKER.Center, TARGET.Center) <= (long)(range * range);
    }
    #endregion

    #region MOVEMENT & COMBAT
    private static void MoveAndFight()
    {
        for (int i = 0; i < LIVE.Count; i++)
        {
            PokemonEntity pokemon = LIVE[i];
            PokemonEntity? target = pokemon.GetTarget();

            if (target is not { IsAlive: true })
            {
                pokemon.IS_MOVING = false;
                continue;
            }

            if (InAttackRange(pokemon, target))
            {
                pokemon.IS_MOVING = false;
                Face(pokemon, target);

                if (pokemon.AttackReady) Attack(pokemon, target);
                continue;
            }

            StepTowards(pokemon, target);
        }
    }

    private static void StepTowards(PokemonEntity POKEMON, PokemonEntity TARGET)
    {
        Vector2 desired = new(TARGET.Center.X - POKEMON.Center.X, TARGET.Center.Y - POKEMON.Center.Y);
        if (desired.LengthSquared() < 1f) return;
        desired.Normalize();

        Vector2 steer = desired + Avoidance(POKEMON) * Balance.AVOIDANCE_WEIGHT;
        if (steer.LengthSquared() < 0.0001f) steer = desired;
        steer.Normalize();

        float speed = Balance.MoveSpeed(POKEMON.POKEMON?.SPEED ?? (int)Balance.SPEED_REFERENCE);
        POKEMON.MoveBy(steer * speed * (float)GameTimeLogic.DELTA);
        POKEMON.ClampInside(GameTable.PlayArea);

        POKEMON.IS_MOVING = true;
        POKEMON.ClearOneShot();
        POKEMON.POKEMON?.ChargeSpecial((int)(CHARGE_PER_SECOND * GameTimeLogic.DELTA));

        POKEMON.DIRECTION = GameDirectionExtensions.Resolve(desired.X, desired.Y, POKEMON.DIRECTION);
    }

    private static void Face(PokemonEntity POKEMON, PokemonEntity TARGET)
    {
        POKEMON.DIRECTION = GameDirectionExtensions.Resolve(
            TARGET.Center.X - POKEMON.Center.X,
            TARGET.Center.Y - POKEMON.Center.Y,
            POKEMON.DIRECTION);
    }

    #region SPACING
    private static float AllySpacing() => Math.Max(8f, GameTable.TILE_SIZE * Balance.ALLY_SPACING_TILES);

    private static Vector2 Avoidance(PokemonEntity POKEMON)
    {
        List<PokemonEntity> allies = POKEMON.ENEMY ? ENEMIES : PLAYERS;
        float spacing = AllySpacing();
        Vector2 push = Vector2.Zero;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity other = allies[i];
            if (ReferenceEquals(other, POKEMON)) continue;

            Vector2 away = new(POKEMON.Center.X - other.Center.X, POKEMON.Center.Y - other.Center.Y);
            float distance = away.Length();
            if (distance >= spacing) continue;

            if (distance < 0.001f) { push += Vector2.UnitX; continue; }
            push += away / distance * (1f - distance / spacing);
        }

        return push;
    }

    private static void SeparateAllies()
    {
        SeparateTeam(PLAYERS);
        SeparateTeam(ENEMIES);
    }

    private static void SeparateTeam(List<PokemonEntity> TEAM)
    {
        float spacing = AllySpacing();
        Rectangle area = GameTable.PlayArea;

        for (int i = 0; i < TEAM.Count; i++)
        {
            for (int j = i + 1; j < TEAM.Count; j++)
            {
                PokemonEntity a = TEAM[i];
                PokemonEntity b = TEAM[j];

                Vector2 delta = new(b.Center.X - a.Center.X, b.Center.Y - a.Center.Y);
                float distance = delta.Length();
                if (distance >= spacing) continue;

                Vector2 axis = distance < 0.001f ? Vector2.UnitX : delta / distance;
                float correction = (spacing - distance) * 0.5f * Balance.SEPARATION_DAMPING;

                a.MoveBy(-axis * correction);
                b.MoveBy(axis * correction);
                a.ClampInside(area);
                b.ClampInside(area);
            }
        }
    }
    #endregion

    private static void Attack(PokemonEntity ATTACKER, PokemonEntity DEFENDER)
    {
        if (ATTACKER.POKEMON == null || DEFENDER.POKEMON == null) return;

        ATTACKER.ResetAttackCooldown(Balance.AttackInterval(ATTACKER.POKEMON.SPEED));
        ATTACKER.PlayOnce(GameAnimation.Attack());
        ATTACKER.Lunge(DEFENDER.Center);
        GameMusic.PlayHit();

        CombatResult result = CombatLogic.Resolve(ATTACKER.POKEMON, DEFENDER.POKEMON, BONUS, GameTimeLogic.SEC_TICK);

        if (!result.HIT)
        {
            FloatingText.SpawnStatus(result.IMMUNE ? "IMMUNE" : "MISS", HeadPosition(DEFENDER), COLOR_RESIST);
            return;
        }

        DEFENDER.POKEMON.HP -= result.DAMAGE;

        ShowDamage(ATTACKER, DEFENDER, result);
        ApplyHitFeedback(ATTACKER, DEFENDER, result);

        if (DEFENDER.POKEMON.HP <= 0) Kill(DEFENDER);
    }

    private static void ShowDamage(PokemonEntity ATTACKER, PokemonEntity DEFENDER, CombatResult RESULT)
    {
        Color color = COLOR_NORMAL;
        if (RESULT.SPECIAL)             color = PokemonTypeColors.Of(ATTACKER.POKEMON!.TYPE);
        else if (RESULT.MULTIPLIER > 1f) color = COLOR_SUPER;
        else if (RESULT.MULTIPLIER < 1f) color = COLOR_RESIST;

        FloatingText.SpawnDamage(RESULT.DAMAGE, HeadPosition(DEFENDER), color, RESULT.SPECIAL);
    }

    private static readonly Point[] BURST_OFFSETS = [new(-38, -22), new(38, -30), new(-14, 30), new(26, 24)];
    private static readonly float[] BURST_SCALES  = [2.6f, 2.2f, 2.0f, 2.4f];

    private static void ApplyHitFeedback(PokemonEntity ATTACKER, PokemonEntity DEFENDER, CombatResult RESULT)
    {
        if (!RESULT.SPECIAL)
        {
            DEFENDER.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.3f));
            return;
        }

        Color accent = PokemonTypeColors.Of(ATTACKER.POKEMON!.TYPE);

        float share = DEFENDER.POKEMON!.MAX_HP > 0 ? RESULT.DAMAGE / (float)DEFENDER.POKEMON.MAX_HP : 0.2f;
        float intensity = Math.Clamp(share * 40f, 5f, 16f);

        DEFENDER.SetEffect(new RenderEffect(RenderEffectType.IMPACT, 0.45f, intensity, accent));

        GameEffect.Spawn(
            GameEffectType.TARGET,
            ATTACKER.POKEMON.EffectPath,
            FRAMES: 4,
            DEFENDER.Center,
            DEFENDER,
            accent,
            SCALE: 5f);

        Point center = DEFENDER.Center;
        for (int i = 0; i < BURST_OFFSETS.Length; i++)
        {
            GameEffect.Spawn(
                GameEffectType.AREA,
                ATTACKER.POKEMON.EffectPath,
                FRAMES: 4,
                new Point(center.X + BURST_OFFSETS[i].X, center.Y + BURST_OFFSETS[i].Y),
                TARGET: null,
                accent,
                BURST_SCALES[i]);
        }

        GameTimeLogic.RequestFreeze(Balance.HIT_STOP_SECONDS);
    }

    private static Point HeadPosition(PokemonEntity ENTITY)
    {
        Rectangle bounds = ENTITY.GetRectangle();
        return new Point(bounds.X + bounds.Width / 2, bounds.Y);
    }

    private static void Kill(PokemonEntity POKEMON)
    {
        for (int i = 0; i < LIVE.Count; i++)
            if (ReferenceEquals(LIVE[i].GetTarget(), POKEMON)) LIVE[i].SetTarget(null);

        POKEMON.BeginDeath();

        if (POKEMON.ENEMY) GameGlobals.ChangeMana(Balance.MANA_PER_KILL);
    }
    #endregion

    #region IDLE
    private static void FaceIdleDirections()
    {
        for (int i = 0; i < LIVE.Count; i++) FaceClosestOpponent(LIVE[i]);

        if (GameMouse.GetCarry() is PokemonEntity carried) FaceClosestOpponent(carried);
    }

    private static void FaceClosestOpponent(PokemonEntity POKEMON)
    {
        PokemonEntity? opponent = FindClosestEnemy(POKEMON);

        if (opponent == null)
        {
            POKEMON.DIRECTION = POKEMON.ENEMY ? GameDirection.BOTTOM : GameDirection.TOP;
            return;
        }

        Face(POKEMON, opponent);
    }
    #endregion

    #region ROUND END
    private static double _celebrationLeft;
    private static bool _playerWonRound;

    private static void DetectRoundEnd()
    {
        if (ENEMIES.Count != 0 && PLAYERS.Count != 0) return;

        _playerWonRound = PLAYERS.Count != 0;

        if (_playerWonRound)
        {
            GameMusic.PlayVictory();
            FloatingText.SpawnBanner("VICTORY", COLOR_VICTORY);
        }
        else
        {
            FloatingText.SpawnBanner("DEFEAT", COLOR_DEFEAT);
        }

        BeginCelebration(_playerWonRound);
    }

    private static void BeginCelebration(bool PLAYER_WON)
    {
        _celebrationLeft = Balance.CELEBRATION_SECONDS;

        GameTimeLogic.ClearFreeze();

        List<PokemonEntity> winners = PLAYER_WON ? PLAYERS : ENEMIES;
        for (int i = 0; i < winners.Count; i++)
        {
            winners[i].SetTarget(null);
            winners[i].Celebrate(Balance.CELEBRATION_SECONDS);
        }

        GameTable.FlashZone(PLAYER_WON, PLAYER_WON ? COLOR_VICTORY : COLOR_DEFEAT, Balance.CELEBRATION_SECONDS);
    }

    private static void FinishRound()
    {
        bool defeat = !_playerWonRound;

        if (defeat)
        {
            GameGlobals.ChangeHp(-Balance.HP_LOSS_ON_DEFEAT);
            GameGlobals.ChangeMana(Balance.MANA_ON_LOSS);
        }
        else
        {
            GameGlobals.ChangeMana(Balance.MANA_ON_WIN);
        }

        ResetBoardAfterRound(defeat ? Balance.XP_ON_LOSS : Balance.XP_ON_WIN);

        GameEffect.Clear();
        GameTimeLogic.ClearFreeze();
        GameTable.ClearFlash();
        GameGlobals.GAME_STARTED = false;
        GameGlobals.LEVEL++;

        GameTable.ClearEnemies();
        GameTable.InitializeEnemyTeam();

        GameDeck.RollDeck();
        GameDeck.RequestRebuild();

        RefreshCaches();
        GameBonus.Refresh();

        if (GameGlobals.IsDefeated()) { EndRun(); return; }

        EvolutionScene.EnqueueReady(PLAYERS);
    }

    private static void ResetBoardAfterRound(int XP_GAIN)
    {
        bool leveled = false;

        for (int i = 0; i < ALL.Count; i++)
        {
            PokemonEntity entity = ALL[i];

            if (entity.POKEMON != null)
            {
                if (!entity.ENEMY && entity.POKEMON.GainXP(XP_GAIN) > 0) leveled = true;
                entity.POKEMON.HP = entity.POKEMON.MAX_HP;
                entity.POKEMON.SPECIAL_COUNTER = 0;
            }

            entity.SetPosition(entity.START);
            entity.IS_MOVING = false;
            entity.DEAD      = false;
            entity.VISIBLE   = true;
            entity.SetTarget(null);
            entity.ClearOneShot();
            entity.SetEffect(null);
            entity.ResetAttackCooldown(0f);
            entity.ResetRoundVisuals();
        }

        if (leveled) GameMusic.PlayLevelUp();
    }

    private static void EndRun()
    {
        GameTableElement.GetTableElements().Clear();
        GameEffect.Clear();
        FloatingText.Clear();
        GameTooltip.Clear();
        GameMouse.ClearCarry();
        GameTimeLogic.ClearFreeze();
        GameTable.ClearFlash();
        EvolutionScene.Clear();
        _celebrationLeft = 0;

        GameGlobals.ResetRun();

        GameTable.Initialize();
        GameTable.InitializeEnemyTeam();
        GameDeck.Reset();

        RefreshCaches();
        GameBonus.Refresh();
        GameMusic.PlayTitle();
    }
    #endregion
}
