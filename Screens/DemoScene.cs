using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.Table;
using PokemonTFT.UI;

namespace PokemonTFT.Screens;

public static class DemoScene
{
    private const int ROUND = 30;
    private const int LEVEL = 30;
    private const int BOSS_LEVEL = 70;
    private const float BOSS_HEALTH = 1.3f;
    private const float BOSS_POWER = 1.9f;
    private const int CAPTURE_EVERY = 4;
    private const int CAPTURE_WIDTH = 960;
    private const double START_DELAY = 1.2;
    private const double OUTRO = 2.4;
    private const double LIMIT = 45;

    private static readonly (string NAME, int X, int Y)[] TEAM =
    [
        ("Snorlax", 15, 8),
        ("Tyranitar", 21, 8),
        ("Lucario", 12, 9),
        ("Garchomp", 24, 9),
        ("Greninja", 18, 10),
        ("Gardevoir", 18, 11)
    ];

    private const string BOSS = "Ho-Oh";
    private const int BOSS_X = 18;
    private const int BOSS_Y = 1;

    private static string _folder = string.Empty;
    private static Action? _exit;
    private static RenderTarget2D? _small;
    private static double _clock;
    private static double _over = -1;
    private static int _frame;
    private static int _saved;

    public static bool ACTIVE { get; private set; }

    public static void Begin(string FOLDER, Action EXIT)
    {
        ACTIVE = true;
        _folder = FOLDER;
        _exit = EXIT;
        Directory.CreateDirectory(_folder);

        GameGlobals.STATE = GameState.GAME;
        GameGlobals.LEVEL = ROUND;
        GameTable.ClearEnemies();

        foreach ((string name, int x, int y) in TEAM)
        {
            if (PokemonDatabase.Create(name) is not { } pokemon) continue;
            while (pokemon.LEVEL < LEVEL) pokemon.LevelUp();
            Place(new PokemonEntity(0, 0) { POKEMON = pokemon, ENEMY = false }, x, y);
        }

        if (PokemonDatabase.Create(BOSS) is { } boss)
        {
            boss.BASE_HP = (int)(boss.BASE_HP * BOSS_HEALTH);
            boss.BASE_ATK = (int)(boss.BASE_ATK * BOSS_POWER);
            boss.BASE_SPATK = (int)(boss.BASE_SPATK * BOSS_POWER);
            while (boss.LEVEL < BOSS_LEVEL) boss.LevelUp();
            boss.RecalculateStats();
            boss.HP = boss.MAX_HP;
            Place(new PokemonEntity(0, 0) { POKEMON = boss, ENEMY = true, BOSS = true }, BOSS_X, BOSS_Y);
        }

        GameTableLogic.RefreshCaches();
        GameBonus.Refresh();
        GameHud.Refresh();
    }

    private static void Place(PokemonEntity ENTITY, int X, int Y)
    {
        foreach (GameElement element in GameTable.TableElements)
        {
            if (element is not GameTableElement tile || tile.TABLE_POSITION_X != X || tile.TABLE_POSITION_Y != Y) continue;

            tile.InsertPokemon(ENTITY);
            ENTITY.ClampInside(GameTable.PlayArea);
            ENTITY.START = ENTITY.GetPosition();
            return;
        }
    }

    public static void Update()
    {
        _clock += GameTimeLogic.DELTA;
        Microsoft.Xna.Framework.Input.Mouse.SetPosition(0, 0);

        if (!GameGlobals.GAME_STARTED && _over < 0 && _clock >= START_DELAY)
        {
            GameGlobals.GAME_STARTED = true;
            GameBonus.Refresh();
        }

        if (GameGlobals.GAME_STARTED && _over < 0 && Decided()) _over = _clock;

        bool done = (_over >= 0 && _clock - _over >= OUTRO) || _clock >= LIMIT;
        if (!done) return;

        Console.WriteLine($"[DemoScene] {_saved} frames in {_folder}, battle {(_over >= 0 ? _over - START_DELAY : -1):0.0}s, " +
                          $"allies alive {Alive(false)}, boss alive {Alive(true)}");
        ACTIVE = false;
        _exit?.Invoke();
    }

    private static bool Decided() => Alive(true) == 0 || Alive(false) == 0;

    private static int Alive(bool ENEMY)
    {
        int count = 0;
        foreach (GameElement element in GameTableElement.GetTableElements())
            if (element is PokemonEntity { IsAlive: true } entity && entity.ENEMY == ENEMY) count++;
        return count;
    }

    public static void Capture(GraphicsDevice DEVICE, SpriteBatch BATCH, Texture2D? FRAME)
    {
        if (!ACTIVE || FRAME == null) return;
        if (_frame++ % CAPTURE_EVERY != 0) return;

        int height = CAPTURE_WIDTH * FRAME.Height / FRAME.Width;
        if (_small == null || _small.Height != height) _small = new RenderTarget2D(DEVICE, CAPTURE_WIDTH, height);

        DEVICE.SetRenderTarget(_small);
        BATCH.Begin(samplerState: SamplerState.LinearClamp);
        BATCH.Draw(FRAME, new Rectangle(0, 0, CAPTURE_WIDTH, height), Color.White);
        BATCH.End();
        DEVICE.SetRenderTarget(null);

        using FileStream stream = File.Create(Path.Combine(_folder, $"frame_{_saved++:0000}.png"));
        _small.SaveAsPng(stream, CAPTURE_WIDTH, height);
    }
}
