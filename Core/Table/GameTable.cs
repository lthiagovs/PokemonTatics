using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using PokemonTFT.UI;

namespace PokemonTFT.Table;

public static class GameTable
{
    public const int TILE_COLUMNS = 20;
    private const int BORDER_TILES = 1;

    private const int ENEMY_ROWS = 3;

    private const int PLAYER_ROW_FIRST = 3;
    private const int PLAYER_ROW_LAST  = 6;

    private const int MAX_ENEMIES = 7;

    private static readonly Rectangle TS_TILE = new(24, 24, 24, 24);
    private static readonly List<GameElement> TABLE_ELEMENTS = [];
    private static readonly List<GameTableElement> FREE_ENEMY_TILES = [];

    public static IReadOnlyList<GameElement> TableElements => TABLE_ELEMENTS;
    public static List<GameElement> GetTable() => TABLE_ELEMENTS;

    public static int TABLE_SIZE_X { get; private set; }
    public static int TABLE_SIZE_Y { get; private set; }
    public static int TILE_SIZE { get; private set; } = 1;

    public static Rectangle PlayArea { get; private set; }

    public static void Initialize()
    {
        TABLE_ELEMENTS.Clear();

        int tileSize = GameRenderer.GetScreenWidth() / TILE_COLUMNS;
        int yTiles   = GameRenderer.GetScreenHeight() / tileSize;

        TABLE_SIZE_X = TILE_COLUMNS - BORDER_TILES * 2;
        TABLE_SIZE_Y = Math.Max(1, yTiles - BORDER_TILES * 2);
        TILE_SIZE    = tileSize;
        PlayArea     = new Rectangle(tileSize, tileSize, TABLE_SIZE_X * tileSize, TABLE_SIZE_Y * tileSize);

        for (int y = 0; y < TABLE_SIZE_Y; y++)
        {
            for (int x = 0; x < TABLE_SIZE_X; x++)
            {
                var element = new GameTableElement(
                    tileSize + tileSize * x,
                    tileSize + tileSize * y,
                    tileSize, tileSize, VISIBLE: true)
                {
                    TABLE_POSITION_X = x,
                    TABLE_POSITION_Y = y,
                    HOVERABLE        = true,
                    PLAYER_OWN       = y >= PLAYER_ROW_FIRST && y <= Math.Min(PLAYER_ROW_LAST, TABLE_SIZE_Y - 1),
                    ENEMY_ZONE       = y < Math.Min(ENEMY_ROWS, TABLE_SIZE_Y)
                };

                element.SetRendererConfig(GameRendererConfig.Sprite("Environment/tileset", TS_TILE));
                TABLE_ELEMENTS.Add(element);
            }
        }
    }

    public static void Rebuild()
    {
        var occupied = new List<(int X, int Y, PokemonEntity ENTITY)>();

        for (int i = 0; i < TABLE_ELEMENTS.Count; i++)
        {
            if (TABLE_ELEMENTS[i] is GameTableElement { } tile && tile.GetPokemon() is { } pokemon)
                occupied.Add((tile.TABLE_POSITION_X, tile.TABLE_POSITION_Y, pokemon));
        }

        GameTableElement.GetTableElements().Clear();
        Initialize();

        foreach ((int x, int y, PokemonEntity entity) in occupied)
        {
            GameTableElement? tile = FindTile(x, y);
            tile?.InsertPokemon(entity);
        }
    }

    private static GameTableElement? FindTile(int X, int Y)
    {
        for (int i = 0; i < TABLE_ELEMENTS.Count; i++)
        {
            if (TABLE_ELEMENTS[i] is GameTableElement tile
                && tile.TABLE_POSITION_X == X && tile.TABLE_POSITION_Y == Y) return tile;
        }
        return null;
    }

    #region ZONE FLASH
    private const double FLASH_SPEED = 14.0;

    private static double _flashLeft;
    private static double _flashElapsed;
    private static Color _flashColor = Color.White;
    private static bool _flashPlayerZone;

    public static void FlashZone(bool PLAYER_ZONE, Color COLOR, double SECONDS)
    {
        _flashPlayerZone = PLAYER_ZONE;
        _flashColor      = COLOR;
        _flashLeft       = SECONDS;
        _flashElapsed    = 0;
    }

    public static void UpdateFlash(double DELTA)
    {
        if (_flashLeft <= 0) return;

        _flashLeft    -= DELTA;
        _flashElapsed += DELTA;
        if (_flashLeft < 0) _flashLeft = 0;
    }

    public static void ClearFlash() => _flashLeft = 0;

    public static bool TryGetZoneFlash(GameTableElement TILE, out Color COLOR)
    {
        COLOR = Color.White;
        if (_flashLeft <= 0) return false;

        bool inZone = _flashPlayerZone ? TILE.PLAYER_OWN : TILE.ENEMY_ZONE;
        if (!inZone) return false;

        float pulse = 0.5f + 0.5f * (float)Math.Sin(_flashElapsed * FLASH_SPEED);
        COLOR = Color.Lerp(Color.White, _flashColor, pulse);
        return true;
    }
    #endregion

    #region ENEMY TEAM
    private static List<Pokemon> BuildEnemyPool(int SIZE)
    {
        int band = Balance.EnemyStatBand(GameGlobals.LEVEL);
        var pool = new List<Pokemon>(SIZE);
        for (int i = 0; i < SIZE; i++) pool.Add(PokemonDatabase.RandomInBand(band));
        return pool;
    }

    private static void CollectFreeEnemyTiles()
    {
        FREE_ENEMY_TILES.Clear();
        int enemyRows = Math.Min(ENEMY_ROWS, TABLE_SIZE_Y);

        for (int i = 0; i < TABLE_ELEMENTS.Count; i++)
        {
            if (TABLE_ELEMENTS[i] is not GameTableElement tile) continue;
            if (tile.TABLE_POSITION_Y >= enemyRows) continue;
            if (tile.HasPokemon()) continue;
            FREE_ENEMY_TILES.Add(tile);
        }

        for (int i = FREE_ENEMY_TILES.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (FREE_ENEMY_TILES[i], FREE_ENEMY_TILES[j]) = (FREE_ENEMY_TILES[j], FREE_ENEMY_TILES[i]);
        }
    }

    public static void InitializeEnemyTeam()
    {
        int count      = Math.Min(Balance.EnemyCount(GameGlobals.LEVEL), MAX_ENEMIES);
        int enemyLevel = Balance.EnemyLevel(GameGlobals.LEVEL);

        List<Pokemon> pool = BuildEnemyPool(count);
        CollectFreeEnemyTiles();

        int placed = Math.Min(count, FREE_ENEMY_TILES.Count);
        for (int i = 0; i < placed; i++)
        {
            Pokemon scaled = pool[Random.Shared.Next(pool.Count)].Clone();
            for (int level = 1; level < enemyLevel; level++) scaled.LevelUp();

            var entity = new PokemonEntity(0, 0) { POKEMON = scaled, ENEMY = true };
            FREE_ENEMY_TILES[i].InsertPokemon(entity);
        }
    }

    public static void ClearEnemies()
    {
        GameTableElement.RemoveEnemies();

        for (int i = 0; i < TABLE_ELEMENTS.Count; i++)
        {
            if (TABLE_ELEMENTS[i] is GameTableElement tile && tile.GetPokemon() is { ENEMY: true })
                tile.ClearPokemon();
        }
    }
    #endregion
}
