using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public enum TacticKind
{
    BRAWLER,
    GUARDIAN,
    WARDEN,
    DANCER,
    SNIPER,
    BATTLEMAGE,
    FLANKER
}

public readonly record struct Tactic(TacticKind KIND, float RANGE_TILES, float PACE);

public interface IBattleEvents
{
    void Attack(PokemonEntity ATTACKER, PokemonEntity TARGET);

    void Taunt(PokemonEntity GUARDIAN);
}

public sealed class TacticState
{
    public PokemonEntity? TAUNTER;
    public PokemonEntity? ENGAGED;
    public float TAUNTED;
    public float TAUNT_READY;
    public float DASH;
    public float DASH_READY;
    public float CHARGED;
    public float BURST;
    public float BLINK_READY;
    public Vector2 BURST_DIRECTION;
    public float RETARGET;
    public float IDLE;
    public bool PURSUE;
    public bool OPENED;
    public bool ORBITING;
    public float ORBIT_SIGN = 1f;
    public float ORBIT_FLIP;
    public float FLANK_SIDE;
    public float CHARGE_CARRY;

    public void Reset()
    {
        TAUNTER = null;
        ENGAGED = null;
        TAUNTED = 0f;
        TAUNT_READY = 0f;
        DASH = 0f;
        DASH_READY = 0f;
        CHARGED = 0f;
        BURST = 0f;
        BLINK_READY = 0f;
        BURST_DIRECTION = Vector2.Zero;
        RETARGET = 0f;
        IDLE = 0f;
        PURSUE = false;
        OPENED = false;
        ORBITING = false;
        ORBIT_SIGN = 1f;
        ORBIT_FLIP = 0f;
        FLANK_SIDE = 0f;
        CHARGE_CARRY = 0f;
    }

    public void Tick(float DELTA)
    {
        TAUNTED = Math.Max(0f, TAUNTED - DELTA);
        TAUNT_READY = Math.Max(0f, TAUNT_READY - DELTA);
        DASH = Math.Max(0f, DASH - DELTA);
        DASH_READY = Math.Max(0f, DASH_READY - DELTA);
        CHARGED = Math.Max(0f, CHARGED - DELTA);
        BURST = Math.Max(0f, BURST - DELTA);
        BLINK_READY = Math.Max(0f, BLINK_READY - DELTA);
        RETARGET = Math.Max(0f, RETARGET - DELTA);
        ORBIT_FLIP = Math.Max(0f, ORBIT_FLIP - DELTA);
    }
}

public static class BattleTactics
{
    private const float CHARGE_PER_SECOND = 60f;
    private const float ORBIT_PULL = 1.5f;
    private const float ESCAPE_MIN_SHARE = 0.45f;
    private const float HOLD_TILES = 0.2f;
    private const float EDGE_ROOM_TILES = 2f;

    private static readonly float[] ESCAPE_ANGLES = [0f, 35f, -35f, 70f, -70f, 105f, -105f];

    private static IReadOnlyList<PokemonEntity> _players = Array.Empty<PokemonEntity>();
    private static IReadOnlyList<PokemonEntity> _enemies = Array.Empty<PokemonEntity>();
    private static Rectangle _area;
    private static float _tile = 1f;
    private static float _delta;

    #region STYLE
    public static Tactic Of(PokemonStyle STYLE) => STYLE switch
    {
        PokemonStyle.FIGHTER       => new(TacticKind.BRAWLER, 0f, 1f),
        PokemonStyle.PHYSICAL_TANK => new(TacticKind.GUARDIAN, 0f, 0.85f),
        PokemonStyle.MAGIC_TANK    => new(TacticKind.WARDEN, Balance.WARDEN_RANGE_TILES, 0.9f),
        PokemonStyle.EVASION_TANK  => new(TacticKind.DANCER, 0f, 1.1f),
        PokemonStyle.MAGE          => new(TacticKind.SNIPER, Balance.SNIPER_RANGE_TILES, 0.95f),
        PokemonStyle.MAGIC_FIGHTER => new(TacticKind.BATTLEMAGE, Balance.BATTLEMAGE_RANGE_TILES, 1f),
        _                          => new(TacticKind.FLANKER, 0f, 1.15f)
    };

    public static Tactic Of(PokemonEntity UNIT) => Of(UNIT.POKEMON?.GetStyle() ?? PokemonStyle.BALANCED);

    public static string Name(PokemonStyle STYLE) => Of(STYLE).KIND.ToString();

    public static string Describe(PokemonStyle STYLE) => Of(STYLE).KIND switch
    {
        TacticKind.BRAWLER =>
            $"Charges the nearest foe with a burst of speed and never lets go. The first hit after a charge deals "
            + $"{Balance.BRAWLER_CHARGE_BONUS}% more damage and every kill refreshes the charge.",

        TacticKind.GUARDIAN =>
            $"Marches on the foe that threatens its team and takes {Balance.GUARDIAN_GUARD}% less damage. "
            + $"Cannot be pushed and taunts foes within {Balance.GUARDIAN_TAUNT_TILES:0.#} tiles every "
            + $"{Balance.GUARDIAN_TAUNT_RECHARGE:0.#}s.",

        TacticKind.WARDEN =>
            $"Holds a zone around its team: allies within {Balance.WARDEN_AURA_TILES:0.#} tiles take "
            + $"{Balance.WARDEN_AURA}% less damage. Strikes from {Balance.WARDEN_RANGE_TILES:0.#} tiles at "
            + "whoever is closest to its most wounded ally.",

        TacticKind.DANCER =>
            $"Dives the enemy back line, then hits and runs around its target. Dodges {Balance.DANCER_DODGE}% "
            + "of attacks while moving.",

        TacticKind.SNIPER =>
            $"Fires from {Balance.SNIPER_RANGE_TILES:0.#} tiles at the weakest foe in reach and leaps away "
            + "when a foe gets close.",

        TacticKind.BATTLEMAGE =>
            $"Fights from {Balance.BATTLEMAGE_RANGE_TILES:0.#} tiles, aims at the tightest group of foes "
            + "and sidesteps after every special.",

        _ =>
            $"Swings around to strike from the side and deals {Balance.FLANKER_AMBUSH}% more damage to foes "
            + "busy fighting someone else."
    };
    #endregion

    #region FRAME
    public static void Step(IReadOnlyList<PokemonEntity> PLAYERS, IReadOnlyList<PokemonEntity> ENEMIES,
                            Rectangle AREA, int TILE, float DELTA, IBattleEvents EVENTS)
    {
        _players = PLAYERS;
        _enemies = ENEMIES;
        _area = AREA;
        _tile = Math.Max(1f, TILE);
        _delta = Math.Max(0f, DELTA);

        Tick(PLAYERS);
        Tick(ENEMIES);
        Provoke(PLAYERS, EVENTS);
        Provoke(ENEMIES, EVENTS);
        Choose(PLAYERS);
        Choose(ENEMIES);
        Act(PLAYERS, EVENTS);
        Act(ENEMIES, EVENTS);
        Separate(PLAYERS);
        Separate(ENEMIES);
    }

    private static void Tick(IReadOnlyList<PokemonEntity> TEAM)
    {
        for (int i = 0; i < TEAM.Count; i++) TEAM[i].TACTIC.Tick(_delta);
    }
    #endregion

    #region NOTIFICATIONS
    public static bool Evades(PokemonEntity DEFENDER)
        => DEFENDER.IS_MOVING && Of(DEFENDER).KIND == TacticKind.DANCER;

    public static int Mitigate(PokemonEntity TARGET, int DAMAGE)
    {
        int reduction = 0;
        if (Of(TARGET).KIND == TacticKind.GUARDIAN) reduction += Balance.GUARDIAN_GUARD;
        if (Shielded(TARGET)) reduction += Balance.WARDEN_AURA;

        return reduction <= 0 ? DAMAGE : Math.Max(1, DAMAGE * (100 - Math.Min(reduction, 90)) / 100);
    }

    private static bool Shielded(PokemonEntity TARGET)
    {
        IReadOnlyList<PokemonEntity> allies = AlliesOf(TARGET);
        float aura = Balance.WARDEN_AURA_TILES * _tile;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity ally = allies[i];
            if (!ally.IsAlive || Of(ally).KIND != TacticKind.WARDEN) continue;
            if (Distance(ally, TARGET) <= aura) return true;
        }

        return false;
    }

    public static int ConsumeCharge(PokemonEntity ATTACKER)
    {
        if (ATTACKER.TACTIC.CHARGED <= 0f) return 0;

        ATTACKER.TACTIC.CHARGED = 0f;
        return Balance.BRAWLER_CHARGE_BONUS;
    }

    public static bool Ambushes(PokemonEntity ATTACKER, PokemonEntity DEFENDER)
        => Of(ATTACKER).KIND == TacticKind.FLANKER && !ReferenceEquals(DEFENDER.GetTarget(), ATTACKER);

    public static void NotifyKill(PokemonEntity ATTACKER)
    {
        if (Of(ATTACKER).KIND == TacticKind.BRAWLER) ATTACKER.TACTIC.DASH_READY = 0f;
    }

    public static void NotifySpecial(PokemonEntity ATTACKER, PokemonEntity TARGET)
    {
        if (Of(ATTACKER).KIND != TacticKind.BATTLEMAGE) return;

        Vector2 to = TARGET.Center.ToVector2() - ATTACKER.Center.ToVector2();
        if (to.LengthSquared() < 0.001f) return;
        to.Normalize();

        TacticState state = ATTACKER.TACTIC;
        state.ORBIT_SIGN = -state.ORBIT_SIGN;

        Vector2 side = new Vector2(-to.Y, to.X) * state.ORBIT_SIGN;
        float reach = Balance.BATTLEMAGE_SIDESTEP_TILES * _tile;
        if (Room(ATTACKER, side, reach) < Room(ATTACKER, -side, reach)) side = -side;

        state.BURST_DIRECTION = side;
        state.BURST = Balance.BATTLEMAGE_SIDESTEP_SECONDS;
    }
    #endregion

    #region TARGETING
    public static PokemonEntity? Nearest(PokemonEntity UNIT, IReadOnlyList<PokemonEntity> FOES)
    {
        PokemonEntity? best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < FOES.Count; i++)
        {
            PokemonEntity foe = FOES[i];
            if (!foe.IsAlive) continue;

            float distance = DistanceSquared(UNIT, foe);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = foe;
        }

        return best;
    }

    private static void Provoke(IReadOnlyList<PokemonEntity> TEAM, IBattleEvents EVENTS)
    {
        float reach = Balance.GUARDIAN_TAUNT_TILES * _tile;

        for (int i = 0; i < TEAM.Count; i++)
        {
            PokemonEntity guardian = TEAM[i];
            if (!guardian.IsAlive || guardian.TACTIC.TAUNT_READY > 0f) continue;
            if (Of(guardian).KIND != TacticKind.GUARDIAN) continue;

            IReadOnlyList<PokemonEntity> foes = FoesOf(guardian);
            bool fresh = false;

            for (int j = 0; j < foes.Count; j++)
            {
                PokemonEntity foe = foes[j];
                if (!foe.IsAlive || Distance(guardian, foe) > reach) continue;
                if (!ReferenceEquals(foe.TACTIC.TAUNTER, guardian)) fresh = true;
            }

            if (!fresh) continue;

            for (int j = 0; j < foes.Count; j++)
            {
                PokemonEntity foe = foes[j];
                if (!foe.IsAlive || Distance(guardian, foe) > reach) continue;

                Retarget(foe, guardian);
                foe.TACTIC.TAUNTER = guardian;
                foe.TACTIC.TAUNTED = Balance.GUARDIAN_TAUNT_SECONDS;
                foe.TACTIC.PURSUE = false;
            }

            guardian.TACTIC.TAUNT_READY = Balance.GUARDIAN_TAUNT_RECHARGE;
            EVENTS.Taunt(guardian);
        }
    }

    private static void Choose(IReadOnlyList<PokemonEntity> TEAM)
    {
        for (int i = 0; i < TEAM.Count; i++)
        {
            PokemonEntity unit = TEAM[i];
            if (!unit.IsAlive) continue;

            TacticState state = unit.TACTIC;
            if (state.TAUNTED > 0f && state.TAUNTER is { IsAlive: true } taunter)
            {
                Retarget(unit, taunter);
                continue;
            }

            state.TAUNTER = null;

            PokemonEntity? current = unit.GetTarget();
            bool valid = current is { IsAlive: true } && current.ENEMY != unit.ENEMY;

            if (state.PURSUE)
            {
                if (!valid) Retarget(unit, Nearest(unit, FoesOf(unit)));
                continue;
            }

            Tactic tactic = Of(unit);
            switch (tactic.KIND)
            {
                case TacticKind.GUARDIAN:
                    if (valid && state.RETARGET > 0f) break;
                    Retarget(unit, Steady(unit, valid ? current : null, MostThreatening(unit)));
                    state.RETARGET = Balance.GUARDIAN_RETARGET;
                    break;

                case TacticKind.WARDEN:
                    if (valid && state.RETARGET > 0f) break;
                    Retarget(unit, Protecting(unit) ?? Nearest(unit, FoesOf(unit)));
                    state.RETARGET = Balance.WARDEN_RETARGET;
                    break;

                case TacticKind.DANCER:
                    if (!valid) Retarget(unit, Deepest(unit));
                    break;

                case TacticKind.SNIPER:
                    if (valid && state.RETARGET > 0f) break;
                    Retarget(unit, Weakest(unit, tactic) ?? Nearest(unit, FoesOf(unit)));
                    state.RETARGET = Balance.SNIPER_RETARGET;
                    break;

                case TacticKind.BATTLEMAGE:
                    if (valid && state.RETARGET > 0f) break;
                    Retarget(unit, Densest(unit, tactic) ?? Nearest(unit, FoesOf(unit)));
                    state.RETARGET = Balance.BATTLEMAGE_RETARGET;
                    break;

                default:
                    if (!valid) Retarget(unit, Nearest(unit, FoesOf(unit)));
                    break;
            }
        }
    }

    private static void Retarget(PokemonEntity UNIT, PokemonEntity? TARGET)
    {
        if (ReferenceEquals(UNIT.GetTarget(), TARGET)) return;

        UNIT.SetTarget(TARGET);
        UNIT.TACTIC.OPENED = false;
        UNIT.TACTIC.FLANK_SIDE = 0f;
    }

    private static PokemonEntity? Steady(PokemonEntity UNIT, PokemonEntity? CURRENT, PokemonEntity? CANDIDATE)
    {
        if (CURRENT == null || CANDIDATE == null) return CANDIDATE ?? CURRENT;
        if (ReferenceEquals(CURRENT, CANDIDATE)) return CURRENT;

        return GuardScore(UNIT, CURRENT) - GuardScore(UNIT, CANDIDATE) > _tile ? CANDIDATE : CURRENT;
    }

    private static PokemonEntity? MostThreatening(PokemonEntity UNIT)
    {
        IReadOnlyList<PokemonEntity> foes = FoesOf(UNIT);
        PokemonEntity? best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            if (!foes[i].IsAlive) continue;

            float score = GuardScore(UNIT, foes[i]);
            if (score >= bestScore) continue;

            bestScore = score;
            best = foes[i];
        }

        return best;
    }

    private static float GuardScore(PokemonEntity GUARDIAN, PokemonEntity FOE)
    {
        IReadOnlyList<PokemonEntity> allies = AlliesOf(GUARDIAN);
        float closest = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity ally = allies[i];
            if (ReferenceEquals(ally, GUARDIAN) || !ally.IsAlive) continue;
            closest = Math.Min(closest, Distance(ally, FOE));
        }

        float own = Distance(GUARDIAN, FOE);
        return closest == float.MaxValue ? own : closest + own * 0.5f;
    }

    private static PokemonEntity? Protecting(PokemonEntity UNIT)
    {
        IReadOnlyList<PokemonEntity> allies = AlliesOf(UNIT);
        PokemonEntity? ward = null;
        float lowest = Balance.WARDEN_HURT_SHARE;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity ally = allies[i];
            if (!ally.IsAlive || ally.POKEMON is not { MAX_HP: > 0 } pokemon) continue;

            float share = pokemon.HP / (float)pokemon.MAX_HP;
            if (share >= lowest) continue;

            lowest = share;
            ward = ally;
        }

        return ward == null ? null : Nearest(ward, FoesOf(UNIT));
    }

    private static PokemonEntity? Deepest(PokemonEntity UNIT)
    {
        IReadOnlyList<PokemonEntity> foes = FoesOf(UNIT);
        if (!Centroid(AlliesOf(UNIT), null, out Vector2 home)) home = UNIT.Center.ToVector2();

        Vector2 forward = Centroid(foes, null, out Vector2 front) ? front - home : Vector2.Zero;
        if (forward.LengthSquared() < 0.001f) forward = new Vector2(0f, UNIT.ENEMY ? 1f : -1f);
        forward.Normalize();

        PokemonEntity? best = null;
        float bestDepth = float.MinValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            PokemonEntity foe = foes[i];
            if (!foe.IsAlive) continue;

            float depth = Vector2.Dot(foe.Center.ToVector2() - home, forward);
            int health = foe.POKEMON?.HP ?? 0;

            bool deeper = depth > bestDepth + _tile * 0.5f;
            bool tied = Math.Abs(depth - bestDepth) <= _tile * 0.5f && health < bestHealth;
            if (!deeper && !tied) continue;

            bestDepth = Math.Max(depth, bestDepth);
            bestHealth = health;
            best = foe;
        }

        return best ?? Nearest(UNIT, foes);
    }

    private static PokemonEntity? Weakest(PokemonEntity UNIT, Tactic TACTIC)
    {
        IReadOnlyList<PokemonEntity> foes = FoesOf(UNIT);
        float reach = (TACTIC.RANGE_TILES + Balance.SNIPER_SCAN_TILES) * _tile;

        PokemonEntity? best = null;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            PokemonEntity foe = foes[i];
            if (!foe.IsAlive || foe.POKEMON == null || Distance(UNIT, foe) > reach) continue;
            if (foe.POKEMON.HP >= bestHealth) continue;

            bestHealth = foe.POKEMON.HP;
            best = foe;
        }

        return best;
    }

    private static PokemonEntity? Densest(PokemonEntity UNIT, Tactic TACTIC)
    {
        IReadOnlyList<PokemonEntity> foes = FoesOf(UNIT);
        float reach = (TACTIC.RANGE_TILES + Balance.BATTLEMAGE_SCAN_TILES) * _tile;
        float blast = StyleBehaviour.Of(UNIT.POKEMON?.GetStyle() ?? PokemonStyle.MAGIC_FIGHTER).RADIUS_TILES * _tile;

        PokemonEntity? best = null;
        int bestCrowd = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < foes.Count; i++)
        {
            PokemonEntity foe = foes[i];
            if (!foe.IsAlive) continue;

            float distance = Distance(UNIT, foe);
            if (distance > reach) continue;

            int crowd = 0;
            for (int j = 0; j < foes.Count; j++)
            {
                if (j == i || !foes[j].IsAlive) continue;
                if (Distance(foe, foes[j]) <= blast) crowd++;
            }

            if (crowd < bestCrowd || (crowd == bestCrowd && distance >= bestDistance)) continue;

            bestCrowd = crowd;
            bestDistance = distance;
            best = foe;
        }

        return best;
    }
    #endregion

    #region BEHAVIOUR
    private static void Act(IReadOnlyList<PokemonEntity> TEAM, IBattleEvents EVENTS)
    {
        for (int i = 0; i < TEAM.Count; i++)
        {
            PokemonEntity unit = TEAM[i];
            if (!unit.IsAlive) continue;

            TacticState state = unit.TACTIC;
            state.ORBITING = false;

            PokemonEntity? target = unit.GetTarget();
            if (target is not { IsAlive: true })
            {
                unit.IS_MOVING = false;
                continue;
            }

            Tactic tactic = Of(unit);
            float speed = Speed(unit) * tactic.PACE;
            bool inRange = InRange(unit, target, tactic);

            state.IDLE += _delta;
            if (state.IDLE >= Balance.ENGAGE_SECONDS) state.PURSUE = true;

            if (state.PURSUE)
            {
                Engage(unit, target, inRange, speed, EVENTS);
                continue;
            }

            switch (tactic.KIND)
            {
                case TacticKind.BRAWLER:    Brawl(unit, target, inRange, speed, EVENTS); break;
                case TacticKind.WARDEN:     Ward(unit, target, inRange, speed, EVENTS); break;
                case TacticKind.DANCER:     Dance(unit, target, inRange, speed, EVENTS); break;
                case TacticKind.SNIPER:     Snipe(unit, target, inRange, speed, EVENTS); break;
                case TacticKind.BATTLEMAGE: Skirmish(unit, target, inRange, speed, EVENTS); break;
                case TacticKind.FLANKER:    Flank(unit, target, inRange, speed, EVENTS); break;
                default:                    Engage(unit, target, inRange, speed, EVENTS); break;
            }
        }
    }

    private static void Engage(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                               IBattleEvents EVENTS)
    {
        if (IN_RANGE)
        {
            Strike(UNIT, TARGET, EVENTS);
            return;
        }

        Approach(UNIT, TARGET.Center.ToVector2(), SPEED, true);
    }

    private static void Strike(PokemonEntity UNIT, PokemonEntity TARGET, IBattleEvents EVENTS)
    {
        UNIT.IS_MOVING = false;
        Face(UNIT, TARGET);
        if (!UNIT.AttackReady) return;

        UNIT.TACTIC.IDLE = 0f;
        UNIT.TACTIC.PURSUE = false;
        UNIT.TACTIC.ENGAGED = TARGET;
        EVENTS.Attack(UNIT, TARGET);
    }

    private static void Brawl(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                              IBattleEvents EVENTS)
    {
        if (IN_RANGE)
        {
            Strike(UNIT, TARGET, EVENTS);
            return;
        }

        TacticState state = UNIT.TACTIC;
        if (state.DASH <= 0f && state.DASH_READY <= 0f
            && Distance(UNIT, TARGET) > Balance.BRAWLER_DASH_MIN_TILES * _tile)
        {
            state.DASH = Balance.BRAWLER_DASH_SECONDS;
            state.DASH_READY = Balance.BRAWLER_DASH_RECHARGE;
            state.CHARGED = Balance.BRAWLER_DASH_SECONDS + Balance.BRAWLER_CHARGE_WINDOW;
        }

        float pace = state.DASH > 0f ? Balance.BRAWLER_DASH_SPEED : 1f;
        Approach(UNIT, TARGET.Center.ToVector2(), SPEED * pace, true);
    }

    private static void Ward(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                             IBattleEvents EVENTS)
    {
        if (IN_RANGE)
        {
            Strike(UNIT, TARGET, EVENTS);
            return;
        }

        Vector2 goal = TARGET.Center.ToVector2();
        if (Centroid(AlliesOf(UNIT), UNIT, out Vector2 anchor))
        {
            Vector2 offset = goal - anchor;
            float leash = Balance.WARDEN_LEASH_TILES * _tile;
            if (offset.LengthSquared() > leash * leash) goal = anchor + Vector2.Normalize(offset) * leash;
        }

        float hold = HOLD_TILES * _tile;
        if (Vector2.DistanceSquared(goal, UNIT.Center.ToVector2()) <= hold * hold)
        {
            UNIT.IS_MOVING = false;
            Face(UNIT, TARGET);
            return;
        }

        Approach(UNIT, goal, SPEED, true);
    }

    private static void Dance(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                              IBattleEvents EVENTS)
    {
        TacticState state = UNIT.TACTIC;

        if (!state.OPENED)
        {
            if (!IN_RANGE)
            {
                Approach(UNIT, TARGET.Center.ToVector2(), SPEED * Balance.DANCER_DIVE_SPEED, true);
                return;
            }

            state.OPENED = true;
        }

        if (UNIT.AttackReady)
        {
            if (IN_RANGE)
            {
                Strike(UNIT, TARGET, EVENTS);
                return;
            }

            Approach(UNIT, TARGET.Center.ToVector2(), SPEED, false);
            return;
        }

        Orbit(UNIT, TARGET, SPEED * Balance.DANCER_ORBIT_PACE);
    }

    private static void Orbit(PokemonEntity UNIT, PokemonEntity TARGET, float SPEED)
    {
        TacticState state = UNIT.TACTIC;

        if (state.ORBIT_FLIP <= 0f)
        {
            state.ORBIT_SIGN = -state.ORBIT_SIGN;
            state.ORBIT_FLIP = Balance.DANCER_ORBIT_FLIP;
        }

        Vector2 radial = UNIT.Center.ToVector2() - TARGET.Center.ToVector2();
        float distance = radial.Length();
        if (distance < 0.001f)
        {
            radial = new Vector2(state.ORBIT_SIGN, 0f);
            distance = 1f;
        }

        radial /= distance;

        Vector2 tangent = new Vector2(-radial.Y, radial.X) * state.ORBIT_SIGN;
        float desired = Balance.DANCER_ORBIT_TILES * _tile;
        float pull = Math.Clamp((desired - distance) / desired, -1f, 1f);

        Vector2 heading = tangent + radial * pull * ORBIT_PULL;
        if (heading.LengthSquared() < 0.0001f) heading = tangent;
        heading.Normalize();

        if (Shift(UNIT, heading, SPEED))
        {
            state.ORBIT_SIGN = -state.ORBIT_SIGN;
            state.ORBIT_FLIP = Balance.DANCER_ORBIT_FLIP;
        }

        Face(UNIT, TARGET);
        state.ORBITING = true;
    }

    private static void Snipe(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                              IBattleEvents EVENTS)
    {
        TacticState state = UNIT.TACTIC;
        float blinkSpeed = Balance.SNIPER_BLINK_TILES / Balance.SNIPER_BLINK_SECONDS * _tile * UNIT.STATUS.SpeedScale;

        if (state.BURST > 0f)
        {
            if (Shift(UNIT, state.BURST_DIRECTION, blinkSpeed)) state.BURST = 0f;
            Face(UNIT, TARGET);
            return;
        }

        if (state.BLINK_READY <= 0f && Nearest(UNIT, FoesOf(UNIT)) is { } threat
            && Distance(UNIT, threat) < Balance.SNIPER_THREAT_TILES * _tile)
        {
            Vector2 away = Escape(UNIT, threat.Center.ToVector2(), Balance.SNIPER_BLINK_TILES * _tile);
            if (away != Vector2.Zero)
            {
                state.BURST = Balance.SNIPER_BLINK_SECONDS;
                state.BURST_DIRECTION = away;
                state.BLINK_READY = Balance.SNIPER_BLINK_RECHARGE;

                Shift(UNIT, away, blinkSpeed);
                Face(UNIT, TARGET);
                return;
            }
        }

        Engage(UNIT, TARGET, IN_RANGE, SPEED, EVENTS);
    }

    private static void Skirmish(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                                 IBattleEvents EVENTS)
    {
        TacticState state = UNIT.TACTIC;

        if (state.BURST > 0f)
        {
            float stepSpeed = Balance.BATTLEMAGE_SIDESTEP_TILES / Balance.BATTLEMAGE_SIDESTEP_SECONDS * _tile
                            * UNIT.STATUS.SpeedScale;
            if (Shift(UNIT, state.BURST_DIRECTION, stepSpeed)) state.BURST = 0f;
            Face(UNIT, TARGET);
            return;
        }

        Engage(UNIT, TARGET, IN_RANGE, SPEED, EVENTS);
    }

    private static void Flank(PokemonEntity UNIT, PokemonEntity TARGET, bool IN_RANGE, float SPEED,
                              IBattleEvents EVENTS)
    {
        if (IN_RANGE)
        {
            Strike(UNIT, TARGET, EVENTS);
            return;
        }

        Vector2 to = TARGET.Center.ToVector2() - UNIT.Center.ToVector2();
        float distance = to.Length();
        if (distance < 0.001f) return;

        Vector2 direction = to / distance;
        TacticState state = UNIT.TACTIC;
        if (state.FLANK_SIDE == 0f) state.FLANK_SIDE = FlankSide(UNIT, direction);

        float near = Balance.FLANKER_NEAR_TILES * _tile;
        float far = Balance.FLANKER_FAR_TILES * _tile;
        float swing = Balance.FLANKER_SWING * Math.Clamp((distance - near) / Math.Max(1f, far - near), 0f, 1f);

        Vector2 side = new Vector2(-direction.Y, direction.X) * state.FLANK_SIDE;
        Steer(UNIT, direction + side * swing, direction, SPEED, true);
    }

    private static float FlankSide(PokemonEntity UNIT, Vector2 DIRECTION)
    {
        Vector2 side = new(-DIRECTION.Y, DIRECTION.X);
        float x = UNIT.Center.X;
        float outward = x >= _area.Center.X ? 1f : -1f;
        float sign = side.X * outward >= 0f ? 1f : -1f;

        float room = sign * side.X > 0f ? _area.Right - x : x - _area.Left;
        if (Math.Abs(side.X) > 0.2f && room < EDGE_ROOM_TILES * _tile) sign = -sign;

        return sign;
    }
    #endregion

    #region MOTION
    private static float Speed(PokemonEntity UNIT)
    {
        int speed = UNIT.POKEMON?.SPEED ?? (int)Balance.SPEED_REFERENCE;
        return Balance.MoveTiles(speed) * _tile * UNIT.STATUS.SpeedScale;
    }

    private static bool InRange(PokemonEntity UNIT, PokemonEntity TARGET, Tactic TACTIC)
    {
        bool engaged = UNIT.TACTIC.IDLE < Balance.ENGAGED_SECONDS && UNIT.TACTIC.ENGAGED == TARGET;
        float range = TACTIC.RANGE_TILES * _tile;

        if (range <= 0f)
        {
            float inset = engaged ? Balance.MELEE_HOLD_INSET : Balance.MELEE_CONTACT_INSET;
            return UNIT.CombatBounds(inset).Intersects(TARGET.CombatBounds(inset));
        }

        if (engaged) range *= Balance.RANGE_HOLD_SCALE;
        return DistanceSquared(UNIT, TARGET) <= range * range;
    }

    private static void Approach(PokemonEntity UNIT, Vector2 GOAL, float SPEED, bool CHARGE)
    {
        Vector2 desired = GOAL - UNIT.Center.ToVector2();
        if (desired.LengthSquared() < 1f)
        {
            UNIT.IS_MOVING = false;
            return;
        }

        desired.Normalize();
        Steer(UNIT, desired, desired, SPEED, CHARGE);
    }

    private static void Steer(PokemonEntity UNIT, Vector2 HEADING, Vector2 FACING, float SPEED, bool CHARGE)
    {
        if (HEADING.LengthSquared() < 0.0001f) HEADING = FACING;
        HEADING.Normalize();

        Vector2 steer = HEADING + Avoidance(UNIT) * Balance.AVOIDANCE_WEIGHT;
        if (steer.LengthSquared() < 0.0001f) steer = HEADING;
        steer.Normalize();

        Shift(UNIT, steer, SPEED);
        UNIT.ClearOneShot();
        if (CHARGE) Charge(UNIT);

        UNIT.DIRECTION = GameDirectionExtensions.Resolve(FACING.X, FACING.Y, UNIT.DIRECTION);
    }

    private static bool Shift(PokemonEntity UNIT, Vector2 HEADING, float SPEED)
    {
        UNIT.MoveBy(HEADING * SPEED * _delta);
        Point free = UNIT.GetPosition();
        UNIT.ClampInside(_area);

        UNIT.IS_MOVING = true;
        UNIT.SetWalkPace(SPEED);
        return UNIT.GetPosition() != free;
    }

    private static void Charge(PokemonEntity UNIT)
    {
        if (UNIT.POKEMON == null) return;

        TacticState state = UNIT.TACTIC;
        state.CHARGE_CARRY += CHARGE_PER_SECOND * _delta;

        int whole = (int)state.CHARGE_CARRY;
        if (whole <= 0) return;

        state.CHARGE_CARRY -= whole;
        UNIT.POKEMON.ChargeSpecial(whole);
    }

    private static void Face(PokemonEntity UNIT, PokemonEntity TARGET)
    {
        UNIT.DIRECTION = GameDirectionExtensions.Resolve(
            TARGET.Center.X - UNIT.Center.X,
            TARGET.Center.Y - UNIT.Center.Y,
            UNIT.DIRECTION);
    }

    private static Vector2 Escape(PokemonEntity UNIT, Vector2 FROM, float DISTANCE)
    {
        Vector2 origin = UNIT.Center.ToVector2();
        Vector2 away = origin - FROM;
        if (away.LengthSquared() < 0.001f) away = new Vector2(0f, UNIT.ENEMY ? -1f : 1f);
        away.Normalize();

        Vector2 best = Vector2.Zero;
        float bestGain = DISTANCE * ESCAPE_MIN_SHARE;
        float before = Vector2.Distance(origin, FROM);

        for (int i = 0; i < ESCAPE_ANGLES.Length; i++)
        {
            Vector2 direction = Rotate(away, MathHelper.ToRadians(ESCAPE_ANGLES[i]));
            Vector2 end = Clamp(UNIT, origin + direction * DISTANCE);

            float gain = Vector2.Distance(end, FROM) - before;
            if (gain <= bestGain) continue;

            bestGain = gain;
            best = direction;
        }

        return best;
    }

    private static float Room(PokemonEntity UNIT, Vector2 DIRECTION, float DISTANCE)
    {
        Vector2 origin = UNIT.Center.ToVector2();
        return Vector2.Distance(origin, Clamp(UNIT, origin + DIRECTION * DISTANCE));
    }

    private static Vector2 Clamp(PokemonEntity UNIT, Vector2 POINT)
    {
        Rectangle bounds = UNIT.GetRectangle();
        float halfW = bounds.Width / 2f;
        float halfH = bounds.Height / 2f;

        return new Vector2(
            Math.Clamp(POINT.X, _area.Left + halfW, Math.Max(_area.Left + halfW, _area.Right - halfW)),
            Math.Clamp(POINT.Y, _area.Top + halfH, Math.Max(_area.Top + halfH, _area.Bottom - halfH)));
    }

    private static Vector2 Rotate(Vector2 VALUE, float RADIANS)
    {
        float cos = MathF.Cos(RADIANS);
        float sin = MathF.Sin(RADIANS);
        return new Vector2(VALUE.X * cos - VALUE.Y * sin, VALUE.X * sin + VALUE.Y * cos);
    }
    #endregion

    #region SPACING
    private static float Spacing() => Math.Max(8f, _tile * Balance.ALLY_SPACING_TILES);

    private static bool Heavy(PokemonEntity UNIT) => Of(UNIT).KIND == TacticKind.GUARDIAN;

    private static Vector2 Avoidance(PokemonEntity UNIT)
    {
        IReadOnlyList<PokemonEntity> allies = AlliesOf(UNIT);
        float spacing = Spacing();
        Vector2 push = Vector2.Zero;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity other = allies[i];
            if (ReferenceEquals(other, UNIT) || !other.IsAlive) continue;

            Vector2 away = UNIT.Center.ToVector2() - other.Center.ToVector2();
            float distance = away.Length();
            if (distance >= spacing) continue;

            if (distance < 0.001f)
            {
                push += Vector2.UnitX;
                continue;
            }

            push += away / distance * (1f - distance / spacing);
        }

        return push;
    }

    private static void Separate(IReadOnlyList<PokemonEntity> TEAM)
    {
        float spacing = Spacing();

        for (int i = 0; i < TEAM.Count; i++)
        {
            PokemonEntity a = TEAM[i];
            if (!a.IsAlive) continue;

            for (int j = i + 1; j < TEAM.Count; j++)
            {
                PokemonEntity b = TEAM[j];
                if (!b.IsAlive) continue;

                Vector2 delta = b.Center.ToVector2() - a.Center.ToVector2();
                float distance = delta.Length();
                if (distance >= spacing) continue;

                Vector2 axis = distance < 0.001f ? Vector2.UnitX : delta / distance;
                float correction = (spacing - distance) * 0.5f * Balance.SEPARATION_DAMPING;

                bool heavyA = Heavy(a);
                bool heavyB = Heavy(b);
                float shareA = heavyA == heavyB ? 1f : heavyA ? 0f : 2f;
                float shareB = heavyA == heavyB ? 1f : heavyB ? 0f : 2f;

                a.MoveBy(-axis * correction * shareA);
                b.MoveBy(axis * correction * shareB);
                a.ClampInside(_area);
                b.ClampInside(_area);
            }
        }
    }
    #endregion

    #region GEOMETRY
    private static IReadOnlyList<PokemonEntity> FoesOf(PokemonEntity UNIT) => UNIT.ENEMY ? _players : _enemies;

    private static IReadOnlyList<PokemonEntity> AlliesOf(PokemonEntity UNIT) => UNIT.ENEMY ? _enemies : _players;

    private static bool Centroid(IReadOnlyList<PokemonEntity> TEAM, PokemonEntity? EXCLUDE, out Vector2 CENTER)
    {
        Vector2 sum = Vector2.Zero;
        int count = 0;

        for (int i = 0; i < TEAM.Count; i++)
        {
            PokemonEntity unit = TEAM[i];
            if (!unit.IsAlive || ReferenceEquals(unit, EXCLUDE)) continue;

            sum += unit.Center.ToVector2();
            count++;
        }

        CENTER = count > 0 ? sum / count : Vector2.Zero;
        return count > 0;
    }

    private static float Distance(PokemonEntity A, PokemonEntity B) => MathF.Sqrt(DistanceSquared(A, B));

    private static float DistanceSquared(PokemonEntity A, PokemonEntity B)
    {
        float dx = B.Center.X - A.Center.X;
        float dy = B.Center.Y - A.Center.Y;
        return dx * dx + dy * dy;
    }
    #endregion
}
