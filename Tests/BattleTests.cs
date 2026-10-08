using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using Xunit;

namespace PokemonTFT.Tests;

public class BattleTests
{
    private const int TILE = 48;
    private const float DELTA = 1f / 60f;
    private const float LIMIT = 90f;

    private static readonly Rectangle AREA = new(96, 96, 36 * TILE, 12 * TILE);

    private sealed class Arena : IBattleEvents
    {
        private readonly List<PokemonEntity> _all = [];
        private readonly List<PokemonEntity> _players = [];
        private readonly List<PokemonEntity> _enemies = [];
        private readonly TypeBonusSnapshot _bonus = new();
        private float _time;

        public bool LeftArena { get; private set; }

        public void Add(PokemonEntity UNIT) => _all.Add(UNIT);

        public float Fight()
        {
            while (_time < LIMIT)
            {
                _players.Clear();
                _enemies.Clear();
                foreach (PokemonEntity unit in _all)
                    if (unit.IsAlive) (unit.ENEMY ? _enemies : _players).Add(unit);

                if (_players.Count == 0 || _enemies.Count == 0) return _time;

                foreach (PokemonEntity unit in _all)
                {
                    unit.TickAttackCooldown(DELTA);
                    Rectangle bounds = unit.GetRectangle();
                    if (bounds.Left < AREA.Left - 1 || bounds.Right > AREA.Right + 1 ||
                        bounds.Top < AREA.Top - 1 || bounds.Bottom > AREA.Bottom + 1) LeftArena = true;
                }

                BattleTactics.Step(_players, _enemies, AREA, TILE, DELTA, this);
                _time += DELTA;
            }

            return _time;
        }

        public void Attack(PokemonEntity ATTACKER, PokemonEntity TARGET)
        {
            ATTACKER.ResetAttackCooldown(Balance.AttackInterval(ATTACKER.POKEMON!.SPEED));

            CombatResult result = CombatLogic.Resolve(ATTACKER.POKEMON, TARGET.POKEMON!, _bonus, !ATTACKER.ENEMY);
            if (!result.HIT) return;

            int damage = Math.Max(1, (int)(result.DAMAGE * Balance.OvertimeScale(_time)));
            TARGET.POKEMON!.HP -= BattleTactics.Mitigate(TARGET, damage);
            if (TARGET.POKEMON.HP <= 0) TARGET.BeginDeath();
        }

        public void Taunt(PokemonEntity GUARDIAN) { }
    }

    private static PokemonEntity Place(Pokemon POKEMON, int COLUMN, int ROW, bool ENEMY)
    {
        var entity = new PokemonEntity(0, 0) { POKEMON = POKEMON, ENEMY = ENEMY };
        int size = entity.GetEntityConfig().DrawSize;
        entity.SetPosition(new Point(AREA.X + COLUMN * TILE + TILE / 2 - size / 2, AREA.Y + ROW * TILE + TILE / 2 - size / 2));
        entity.ClampInside(AREA);
        return entity;
    }

    [Fact]
    public void RandomFightsAlwaysEndInsideTheArena()
    {
        Repository.LoadRoster();
        var random = new Random(42);

        for (int fight = 0; fight < 40; fight++)
        {
            var arena = new Arena();
            for (int i = 0; i < 4; i++)
            {
                Pokemon ally = PokemonDatabase.RandomNearPower(420f, 10, 120f)!;
                Pokemon foe = PokemonDatabase.RandomNearPower(420f, 10, 120f)!;
                while (ally.LEVEL < 10) ally.LevelUp();
                while (foe.LEVEL < 10) foe.LevelUp();

                arena.Add(Place(ally, 12 + i * 4, 8 + random.Next(4), false));
                arena.Add(Place(foe, random.Next(36), random.Next(4), true));
            }

            float time = arena.Fight();

            Assert.True(time < LIMIT, $"fight {fight} did not end");
            Assert.False(arena.LeftArena, $"a unit left the arena in fight {fight}");
        }
    }
}
