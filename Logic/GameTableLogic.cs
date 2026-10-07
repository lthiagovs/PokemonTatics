using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Models;
using PokemonTFT.Screens;
using PokemonTFT.Table;
using PokemonTFT.UI;

namespace PokemonTFT.Logic;

public static class GameTableLogic
{
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

    public static int GetPlayerPokemonsCount() => PLAYERS.Count;

    public static IReadOnlyList<PokemonEntity> PlayerEntities => PLAYERS;

    public static void RefreshPreview()
    {
        PokemonEntity? subject = GameMouse.GetCarry() as PokemonEntity ?? Hovered();
        if (subject?.POKEMON == null)
        {
            AreaPreview.Clear();
            return;
        }

        PokemonEntity? target = subject.GetTarget() is { IsAlive: true } current
            ? current
            : BattleTactics.Nearest(subject, subject.ENEMY ? PLAYERS : ENEMIES);

        AreaPreview.Show(subject, target, GameTable.TILE_SIZE);
    }

    private static PokemonEntity? Hovered()
    {
        for (int i = 0; i < LIVE.Count; i++)
            if (LIVE[i].HOVERED) return LIVE[i];
        return null;
    }

    public static PokemonEntity? GetPlayerLine(string LINE)
    {
        if (LINE.Length == 0) return null;

        for (int i = 0; i < PLAYERS.Count; i++)
            if (string.Equals(PLAYERS[i].POKEMON?.LINE, LINE, StringComparison.Ordinal)) return PLAYERS[i];
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

        if (GameGlobals.GAME_STARTED)
        {
            TickStatuses();
            TickRegeneration();
            UpdateRun();
            return;
        }

        ClearStatuses();
        UpdateIdle();
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

        _combatTime += (float)GameTimeLogic.DELTA;
        if (!_overtime && _combatTime > Balance.OVERTIME_SECONDS)
        {
            _overtime = true;
            FloatingText.SpawnBanner("OVERTIME", COLOR_DEFEAT);
        }

        if (GameTimeLogic.COMBAT_FROZEN) return;

        BattleTactics.Step(PLAYERS, ENEMIES, GameTable.PlayArea, GameTable.TILE_SIZE,
            (float)GameTimeLogic.DELTA, EVENTS);
        DetectRoundEnd();
    }
    #endregion

    #region DRAG
    private static void HandleSell()
    {
        if (!GameMouse.RightPressed()) return;

        if (GameMouse.GetCarry() is PokemonEntity { PURCHASED: true, POKEMON: not null } sold)
        {
            ItemLogic.Reclaim(sold.POKEMON);
            GameGlobals.ChangeMana(sold.POKEMON.COST);
            FloatingText.SpawnStatus($"+{sold.POKEMON.COST}", GameMouse.GetPos(), new Color(120, 200, 255));
        }

        GameMouse.ClearCarry();
    }
    #endregion

    #region COMBAT
    private static readonly TableEvents EVENTS = new();

    private sealed class TableEvents : IBattleEvents
    {
        public void Attack(PokemonEntity ATTACKER, PokemonEntity TARGET) => GameTableLogic.Attack(ATTACKER, TARGET);

        public void Taunt(PokemonEntity GUARDIAN)
            => FloatingText.SpawnStatus("TAUNT", HeadPosition(GUARDIAN), COLOR_TAUNT);
    }

    private static readonly Color COLOR_TAUNT = new(255, 150, 70);

    private static void Attack(PokemonEntity ATTACKER, PokemonEntity DEFENDER)
    {
        if (ATTACKER.POKEMON == null || DEFENDER.POKEMON == null) return;

        float interval = Balance.AttackInterval(ATTACKER.POKEMON.SPEED) * ATTACKER.STATUS.IntervalScale;
        if (!ATTACKER.ENEMY) interval *= GameBonusLogic.SwarmScale();
        if (ItemLogic.HasCombo(ATTACKER.POKEMON, ItemCombo.FRENZY))
            interval *= 1f - Balance.COMBO_FRENZY_BONUS / 100f;

        ATTACKER.ResetAttackCooldown(interval);
        ATTACKER.PlayOnce(GameAnimation.Attack());
        ATTACKER.Lunge(DEFENDER.Center);
        GameMusic.PlayHit();

        if (BattleTactics.Evades(DEFENDER) && Random.Shared.Next(100) < Balance.DANCER_DODGE)
        {
            FloatingText.SpawnStatus("DODGE", HeadPosition(DEFENDER), COLOR_RESIST);
            return;
        }

        CombatResult result = CombatLogic.Resolve(ATTACKER.POKEMON, DEFENDER.POKEMON, BONUS, !ATTACKER.ENEMY);

        if (!result.HIT)
        {
            FloatingText.SpawnStatus(result.IMMUNE ? "IMMUNE" : "MISS", HeadPosition(DEFENDER), COLOR_RESIST);
            return;
        }

        int dealt = result.DAMAGE;
        if (result.SPECIAL)
            dealt = Math.Max(1, (int)(dealt * StyleBehaviour.Of(ATTACKER.POKEMON.GetStyle()).TARGET_SCALE));
        if (BattleTactics.Ambushes(ATTACKER, DEFENDER))
            dealt += dealt * Balance.FLANKER_AMBUSH / 100;
        dealt += dealt * BattleTactics.ConsumeCharge(ATTACKER) / 100;

        int damage = Mitigate(DEFENDER, dealt);

        BattleStats.Hit(ATTACKER, DEFENDER, Math.Min(damage, Math.Max(0, DEFENDER.POKEMON.HP)));
        DEFENDER.POKEMON.HP -= damage;

        ShowDamage(ATTACKER, DEFENDER, result, damage);
        ApplyHitFeedback(ATTACKER, DEFENDER, result);
        ApplyItemHit(ATTACKER, DEFENDER, damage);
        if (result.SPECIAL) BattleTactics.NotifySpecial(ATTACKER, DEFENDER);

        if (DEFENDER.POKEMON.HP > 0) return;

        Kill(DEFENDER);
        BattleStats.Kill(ATTACKER);
        BattleTactics.NotifyKill(ATTACKER);
        int bounty = ItemLogic.Value(ATTACKER.POKEMON, ItemEffect.MANA_ON_KILL);
        if (bounty > 0 && !ATTACKER.ENEMY) GameGlobals.ChangeMana(bounty);
    }

    private static void ShowDamage(PokemonEntity ATTACKER, PokemonEntity DEFENDER, CombatResult RESULT, int DAMAGE)
    {
        Color color = COLOR_NORMAL;
        if (RESULT.SPECIAL)             color = PokemonTypeColors.Of(ATTACKER.POKEMON!.TYPE);
        else if (RESULT.MULTIPLIER > 1f) color = COLOR_SUPER;
        else if (RESULT.MULTIPLIER < 1f) color = COLOR_RESIST;

        FloatingText.SpawnDamage(DAMAGE, HeadPosition(DEFENDER), color, RESULT.SPECIAL);
    }

    private static readonly Color COLOR_STATUS = new(190, 120, 240);
    private static readonly Color COLOR_SPLASH = new(255, 196, 120);
    private static readonly Color COLOR_HEAL = new(120, 236, 150);
    private static readonly Color COLOR_DROP = new(255, 214, 96);

    private static Models.Item? _pendingDrop;
    private static bool _bossRewarded;

    private static void ApplyHitFeedback(PokemonEntity ATTACKER, PokemonEntity DEFENDER, CombatResult RESULT)
    {
        if (!RESULT.SPECIAL)
        {
            DEFENDER.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.3f));
            return;
        }

        Color accent = PokemonTypeColors.Of(ATTACKER.POKEMON!.TYPE);

        RollStatus(ATTACKER, DEFENDER);
        ApplySpecialShape(ATTACKER, DEFENDER, RESULT);

        float share = DEFENDER.POKEMON!.MAX_HP > 0 ? RESULT.DAMAGE / (float)DEFENDER.POKEMON.MAX_HP : 0.2f;
        float intensity = Math.Clamp(share * 40f, 5f, 16f);

        DEFENDER.SetEffect(new RenderEffect(RenderEffectType.IMPACT, 0.45f, intensity, accent));
        GameTimeLogic.RequestFreeze(Balance.HIT_STOP_SECONDS);
    }

    private static void SpawnClip(string SPRITE, GameEffectType KIND, Point AT,
                                  PokemonEntity? TARGET, Color TINT, float SCALE = Balance.EFFECT_SCALE)
    {
        if (EffectLibrary.Find(SPRITE) is not { } clip) return;
        GameEffect.Spawn(KIND, clip.PATH, clip.FRAMES, AT, TARGET, TINT, SCALE);
    }

    private static Point Midpoint(Point A, Point B) => new((A.X + B.X) / 2, (A.Y + B.Y) / 2);

    public static void ApplyStatus(PokemonEntity TARGET, StatusKind KIND, Pokemon? SOURCE = null)
    {
        double seconds = KIND switch
        {
            StatusKind.BURN => Balance.BURN_SECONDS,
            StatusKind.POISON => Balance.POISON_SECONDS,
            _ => Balance.PARALYSIS_SECONDS
        };

        if (SOURCE != null && ItemLogic.HasCombo(SOURCE, ItemCombo.PLAGUE))
            seconds *= 1.0 + Balance.COMBO_PLAGUE_BONUS / 100.0;

        TARGET.STATUS.Apply(KIND, seconds);
        SpawnClip(StatusState.Sprite(KIND), GameEffectType.TARGET, TARGET.Center, TARGET, Color.White);
    }

    private static void RollStatus(PokemonEntity ATTACKER, PokemonEntity DEFENDER)
    {
        TypeEffect effect = TypeEffects.Of(ATTACKER.POKEMON!.TYPE);
        if (effect.CHANCE <= 0 || Random.Shared.Next(100) >= effect.CHANCE) return;

        ApplyStatus(DEFENDER, effect.STATUS, ATTACKER.POKEMON);
    }

    private static void ApplyItemHit(PokemonEntity ATTACKER, PokemonEntity DEFENDER, int DAMAGE)
    {
        Pokemon attacker = ATTACKER.POKEMON!;
        if (attacker.ITEMS.Count == 0) return;

        RollItemStatus(ATTACKER, DEFENDER, ItemEffect.BURN_HIT, StatusKind.BURN);
        RollItemStatus(ATTACKER, DEFENDER, ItemEffect.POISON_HIT, StatusKind.POISON);
        RollItemStatus(ATTACKER, DEFENDER, ItemEffect.PARALYSIS_HIT, StatusKind.PARALYSIS);

        int splashDealt = 0;
        int splash = ItemLogic.Value(attacker, ItemEffect.SPLASH);
        if (splash > 0)
        {
            float reach = Balance.ITEM_SPLASH_TILES;
            if (ItemLogic.HasCombo(attacker, ItemCombo.CATACLYSM))
                reach *= 1f + Balance.COMBO_CATACLYSM_BONUS / 100f;

            splashDealt = HitRadius(ATTACKER, DEFENDER, DEFENDER.Center, GameTable.TILE_SIZE * reach,
                Math.Max(1, DAMAGE * splash / 100), TypeEffects.Of(attacker.TYPE), false);
        }

        int lifesteal = ItemLogic.Value(attacker, ItemEffect.LIFESTEAL);
        if (lifesteal <= 0) return;

        int drained = DAMAGE + (ItemLogic.HasCombo(attacker, ItemCombo.SIPHON) ? splashDealt : 0);
        int healed = Math.Max(1, drained * lifesteal / 100);
        int before = attacker.HP;
        attacker.HP = Math.Min(attacker.MAX_HP, attacker.HP + healed);
        BattleStats.Heal(ATTACKER, attacker.HP - before);
        FloatingText.SpawnStatus($"+{healed}", HeadPosition(ATTACKER), COLOR_HEAL);
        SpawnClip("sparkle", GameEffectType.TARGET, ATTACKER.Center, ATTACKER, COLOR_HEAL);
    }

    private static void RollItemStatus(PokemonEntity ATTACKER, PokemonEntity DEFENDER,
                                       ItemEffect EFFECT, StatusKind KIND)
    {
        int chance = ItemLogic.Value(ATTACKER.POKEMON!, EFFECT);
        if (chance <= 0 || Random.Shared.Next(100) >= chance) return;

        ApplyStatus(DEFENDER, KIND, ATTACKER.POKEMON);
    }

    private static int Mitigate(PokemonEntity TARGET, int DAMAGE)
    {
        DAMAGE = Math.Max(1, (int)MathF.Min(1_000_000f, DAMAGE * Balance.OvertimeScale(_combatTime)));

        if (TARGET.POKEMON != null && ItemLogic.HasCombo(TARGET.POKEMON, ItemCombo.BULWARK))
            DAMAGE = Math.Max(1, DAMAGE * (100 - Balance.COMBO_BULWARK_BONUS) / 100);

        return BattleTactics.Mitigate(TARGET, DAMAGE);
    }

    private static int HitSecondary(PokemonEntity ATTACKER, PokemonEntity FOE, int DAMAGE, TypeEffect EFFECT,
                                    bool STATUS)
    {
        if (FOE.POKEMON == null || DAMAGE <= 0) return 0;

        DAMAGE = Mitigate(FOE, DAMAGE);
        BattleStats.Hit(ATTACKER, FOE, Math.Min(DAMAGE, Math.Max(0, FOE.POKEMON.HP)));
        FOE.POKEMON.HP -= DAMAGE;
        FloatingText.SpawnDamage(DAMAGE, HeadPosition(FOE), COLOR_SPLASH, false);
        FOE.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.25f));

        if (STATUS && EFFECT.CHANCE > 0 && Random.Shared.Next(100) < EFFECT.CHANCE)
            ApplyStatus(FOE, EFFECT.STATUS);

        if (FOE.POKEMON.HP > 0) return DAMAGE;

        Kill(FOE);
        BattleStats.Kill(ATTACKER);
        return DAMAGE;
    }

    private static List<PokemonEntity> FoesOf(PokemonEntity ATTACKER)
        => ATTACKER.ENEMY ? PLAYERS : ENEMIES;

    private static void ApplySpecialShape(PokemonEntity ATTACKER, PokemonEntity DEFENDER, CombatResult RESULT)
    {
        SpecialBehaviour behaviour = StyleBehaviour.Of(ATTACKER.POKEMON!.GetStyle());
        TypeEffect effect = TypeEffects.Of(ATTACKER.POKEMON.TYPE);
        Color accent = PokemonTypeColors.Of(ATTACKER.POKEMON.TYPE);

        int share = Math.Max(1, (int)(RESULT.DAMAGE * behaviour.SPLASH_SHARE));
        float radius = GameTable.TILE_SIZE * behaviour.RADIUS_TILES;

        switch (behaviour.SHAPE)
        {
            case SpecialShape.FOCUS:
                SpawnClip(effect.SPRITE, GameEffectType.MISSILE, ATTACKER.Center, DEFENDER, accent, 2.6f);
                break;

            case SpecialShape.BLAST:
                SpawnClip(effect.SPRITE, GameEffectType.TARGET, DEFENDER.Center, DEFENDER, accent, 2.4f);
                HitRadius(ATTACKER, DEFENDER, DEFENDER.Center, radius, share, effect, behaviour.STATUS_ON_SPLASH);
                break;

            case SpecialShape.SHOCKWAVE:
                SpawnClip(effect.SPRITE, GameEffectType.TARGET, ATTACKER.Center, ATTACKER, accent, 3.2f);
                HitRadius(ATTACKER, DEFENDER, ATTACKER.Center, radius, share, effect, behaviour.STATUS_ON_SPLASH);
                break;

            default:
                SpawnClip(effect.SPRITE, GameEffectType.AREA,
                    Midpoint(ATTACKER.Center, DEFENDER.Center), null, accent, 2.8f);
                HitCone(ATTACKER, DEFENDER, radius, behaviour.CONE_DEGREES, share, effect,
                    behaviour.STATUS_ON_SPLASH);
                break;
        }
    }

    private static int HitRadius(PokemonEntity ATTACKER, PokemonEntity DEFENDER, Point CENTER,
                                 float RADIUS, int DAMAGE, TypeEffect EFFECT, bool STATUS)
    {
        int dealt = 0;
        List<PokemonEntity> foes = FoesOf(ATTACKER);
        for (int i = foes.Count - 1; i >= 0; i--)
        {
            PokemonEntity foe = foes[i];
            if (ReferenceEquals(foe, DEFENDER) || foe.DEAD || foe.POKEMON == null) continue;

            float dx = foe.Center.X - CENTER.X;
            float dy = foe.Center.Y - CENTER.Y;
            if (dx * dx + dy * dy > RADIUS * RADIUS) continue;

            dealt += HitSecondary(ATTACKER, foe, DAMAGE, EFFECT, STATUS);
        }

        return dealt;
    }

    private static void HitCone(PokemonEntity ATTACKER, PokemonEntity DEFENDER, float RANGE,
                                float DEGREES, int DAMAGE, TypeEffect EFFECT, bool STATUS)
    {
        float forwardX = DEFENDER.Center.X - ATTACKER.Center.X;
        float forwardY = DEFENDER.Center.Y - ATTACKER.Center.Y;
        float length = (float)Math.Sqrt(forwardX * forwardX + forwardY * forwardY);
        if (length <= 0.001f) return;

        forwardX /= length;
        forwardY /= length;

        float limit = (float)Math.Cos(MathHelper.ToRadians(DEGREES / 2f));

        List<PokemonEntity> foes = FoesOf(ATTACKER);
        for (int i = foes.Count - 1; i >= 0; i--)
        {
            PokemonEntity foe = foes[i];
            if (ReferenceEquals(foe, DEFENDER) || foe.DEAD || foe.POKEMON == null) continue;

            float dx = foe.Center.X - ATTACKER.Center.X;
            float dy = foe.Center.Y - ATTACKER.Center.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (distance > RANGE || distance <= 0.001f) continue;

            if (dx / distance * forwardX + dy / distance * forwardY < limit) continue;

            HitSecondary(ATTACKER, foe, DAMAGE, EFFECT, STATUS);
        }
    }

    private static void ClearStatuses()
    {
        for (int i = 0; i < LIVE.Count; i++)
            if (LIVE[i].STATUS.Any) LIVE[i].STATUS.Clear();
    }

    private static void TickRegeneration()
    {
        if (!GameTimeLogic.SEC_TICK || _celebrationLeft > 0) return;

        for (int i = 0; i < PLAYERS.Count; i++)
        {
            PokemonEntity entity = PLAYERS[i];
            if (entity.DEAD || entity.POKEMON == null) continue;

            int healed = CombatLogic.Regeneration(entity.POKEMON, BONUS);
            if (healed <= 0) continue;

            entity.POKEMON.HP += healed;
            BattleStats.Heal(entity, healed);
            FloatingText.SpawnStatus($"+{healed}", HeadPosition(entity), COLOR_HEAL);
        }
    }

    private static void TickStatuses()
    {
        for (int i = LIVE.Count - 1; i >= 0; i--)
        {
            PokemonEntity entity = LIVE[i];
            if (entity.POKEMON == null || entity.DEAD) continue;

            int damage = entity.STATUS.Update(GameTimeLogic.DELTA, entity.POKEMON.MAX_HP);
            if (damage <= 0) continue;

            BattleStats.Hit(null, entity, Math.Min(damage, Math.Max(0, entity.POKEMON.HP)));
            entity.POKEMON.HP -= damage;
            FloatingText.SpawnDamage(damage, HeadPosition(entity), COLOR_STATUS, false);
            if (entity.POKEMON.HP <= 0) Kill(entity);
        }
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

        if (!POKEMON.ENEMY) return;

        GameGlobals.ChangeMana(Balance.MANA_PER_KILL);

        if (!POKEMON.BOSS || !GrantBossItem()) return;

        FloatingText.SpawnStatus(_pendingDrop!.NAME, HeadPosition(POKEMON), COLOR_DROP);
        SpawnClip("sparkle", GameEffectType.AREA, POKEMON.Center, null, COLOR_DROP, 3.2f);
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
        PokemonEntity? opponent = BattleTactics.Nearest(POKEMON, POKEMON.ENEMY ? PLAYERS : ENEMIES);

        if (opponent == null)
        {
            POKEMON.DIRECTION = POKEMON.ENEMY ? GameDirection.BOTTOM : GameDirection.TOP;
            return;
        }

        POKEMON.DIRECTION = GameDirectionExtensions.Resolve(
            opponent.Center.X - POKEMON.Center.X,
            opponent.Center.Y - POKEMON.Center.Y,
            POKEMON.DIRECTION);
    }
    #endregion

    #region ROUND END
    private static double _celebrationLeft;
    private static float _combatTime;
    private static bool _overtime;
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

        if (defeat) GameGlobals.ChangeHp(-Balance.DefeatDamage(GameGlobals.LEVEL, ENEMIES.Count));

        GameGlobals.ChangeMana(Balance.ManaReward(GameGlobals.LEVEL, !defeat));

        BattleStats.Finish(GameGlobals.LEVEL, !defeat, ALL);
        ResetBoardAfterRound(defeat ? Balance.XP_ON_LOSS : Balance.XP_ON_WIN);

        GameEffect.Clear();
        GameTimeLogic.ClearFreeze();
        GameTable.ClearFlash();
        GameGlobals.GAME_STARTED = false;

        int finished = GameGlobals.LEVEL;
        GameGlobals.LEVEL++;
        GameGlobals.REROLLS_LEFT = Balance.REROLLS_PER_ROUND;

        DungeonThemes.Roll();
        GameTable.Retheme();

        GameTable.ClearEnemies();
        GameTable.InitializeEnemyTeam();

        if (Balance.IsBossRound(finished)) GrantBossItem();
        _bossRewarded = false;

        GameDeck.RollRound();

        RefreshCaches();
        GameBonus.Refresh();

        if (GameGlobals.IsDefeated()) { Screens.GameOverScene.Begin(); return; }

        EvolutionScene.EnqueueReady(PLAYERS);

        if (_pendingDrop == null) return;

        UI.ItemModal.Announce(_pendingDrop);
        _pendingDrop = null;
    }

    private static void ResetBoardAfterRound(int XP_GAIN)
    {
        bool leveled = false;
        _combatTime = 0f;
        _overtime = false;

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
            entity.TACTIC.Reset();
        }

        if (leveled) GameMusic.PlayLevelUp();
    }

    private static bool GrantBossItem()
    {
        if (_bossRewarded || ItemDatabase.Random() is not { } drop) return false;

        _bossRewarded = true;
        _pendingDrop = drop;

        ItemLogic.Store(drop);
        UI.GameInventory.Refresh();
        return true;
    }

    public static void RestartRun()
    {
        _pendingDrop = null;
        _bossRewarded = false;
        ItemLogic.Clear();
        BattleStats.Clear();
        UI.GameInventory.Refresh();
        EndRun();
    }

    private static void EndRun()
    {
        GameTableElement.GetTableElements().Clear();
        GameEffect.Clear();
        FloatingText.Clear();
        GameModal.Close();
        GameMouse.ClearCarry();
        GameTimeLogic.ClearFreeze();
        GameTable.ClearFlash();
        EvolutionScene.Clear();
        _celebrationLeft = 0;
        _combatTime = 0f;
        _overtime = false;

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
