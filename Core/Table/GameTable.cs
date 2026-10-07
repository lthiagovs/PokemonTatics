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
    public const int TILE_COLUMNS = 40;

    public const int UI_COLUMNS = 20;
    private const int BORDER_TILES = 2;

    private const int DECK_ROWS = 3;

    private const int ENEMY_ROWS = 4;

    private const int PLAYER_ROW_FIRST = 8;
    private const int PLAYER_ROW_LAST  = 11;

    private static readonly Rectangle TS_TILE = new(24, 24, 24, 24);
    private static readonly List<GameElement> TABLE_ELEMENTS = [];
    private static readonly List<GameTableElement> FREE_ENEMY_TILES = [];

    public static IReadOnlyList<GameElement> TableElements => TABLE_ELEMENTS;

    public static int TABLE_SIZE_X { get; private set; }
    public static int TABLE_SIZE_Y { get; private set; }
    public static int TILE_SIZE { get; private set; } = 1;

    public static Rectangle PlayArea { get; private set; }

    public static int FieldCap { get; private set; } = Balance.FIELD_START;

    public static int EnemyCap { get; private set; } = Balance.FIELD_START;

    public static void Initialize()
    {
        TABLE_ELEMENTS.Clear();

        int tileSize = GameRenderer.GetScreenWidth() / TILE_COLUMNS;
        int deckHeight = GameRenderer.GetScreenWidth() / UI_COLUMNS * DECK_ROWS;
        int yTiles = (GameRenderer.GetScreenHeight() - deckHeight) / tileSize;

        TABLE_SIZE_X = TILE_COLUMNS - BORDER_TILES * 2;
        TABLE_SIZE_Y = Math.Max(1, yTiles - BORDER_TILES * 2);
        TILE_SIZE    = tileSize;

        int originX = (GameRenderer.GetScreenWidth() - TABLE_SIZE_X * tileSize) / 2;
        int originY = BORDER_TILES * tileSize;
        PlayArea     = new Rectangle(originX, originY,
            TABLE_SIZE_X * tileSize, TABLE_SIZE_Y * tileSize);

        int playerTiles = 0;
        int enemyTiles = 0;

        for (int y = 0; y < TABLE_SIZE_Y; y++)
        {
            for (int x = 0; x < TABLE_SIZE_X; x++)
            {
                var element = new GameTableElement(
                    originX + tileSize * x,
                    originY + tileSize * y,
                    tileSize, tileSize, VISIBLE: true)
                {
                    TABLE_POSITION_X = x,
                    TABLE_POSITION_Y = y,
                    HOVERABLE        = true,
                    PLAYER_OWN       = y >= PLAYER_ROW_FIRST && y <= Math.Min(PLAYER_ROW_LAST, TABLE_SIZE_Y - 1),
                    ENEMY_ZONE       = y < Math.Min(ENEMY_ROWS, TABLE_SIZE_Y)
                };

                element.SetRendererConfig(GroundConfig(x, y));
                TABLE_ELEMENTS.Add(element);

                if (element.PLAYER_OWN) playerTiles++;
                if (element.ENEMY_ZONE) enemyTiles++;
            }
        }

        FieldCap = Balance.FieldCap(playerTiles);
        EnemyCap = Balance.FieldCap(enemyTiles);
    }

    public static GameRendererConfig GroundConfig(int COLUMN, int ROW)
    {
        DungeonTheme? theme = DungeonThemes.Current;
        if (theme == null) return GameRendererConfig.Sprite("Environment/tileset", TS_TILE);

        int slice = DungeonThemes.SliceIndex(COLUMN, ROW, TABLE_SIZE_X, TABLE_SIZE_Y);

        if (slice == DungeonThemes.SLICE_CENTER && theme.HasDecor)
        {
            int decor = DungeonThemes.DecorAt(COLUMN, ROW);
            if (decor >= 0) return GameRendererConfig.Sprite(DungeonThemes.TEXTURE, theme.Decor(decor));
        }

        return GameRendererConfig.Sprite(DungeonThemes.TEXTURE, theme.Ground(slice));
    }

    public static void Retheme()
    {
        UITheme.Rebuild();
        GameDeck.RequestRebuild();
        GameHud.Refresh();

        for (int i = 0; i < TABLE_ELEMENTS.Count; i++)
        {
            if (TABLE_ELEMENTS[i] is GameTableElement tile)
                tile.SetRendererConfig(GroundConfig(tile.TABLE_POSITION_X, tile.TABLE_POSITION_Y));
        }
        GameMap.Initialize();
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
        int slots = Math.Min(GameGlobals.GetTableSize(), EnemyCap);
        List<Pokemon> team = Encounters.Team(GameGlobals.LEVEL, slots);
        if (team.Count == 0) return;

        CollectFreeEnemyTiles();

        bool boss = Balance.IsBossRound(GameGlobals.LEVEL);
        int placed = Math.Min(team.Count, FREE_ENEMY_TILES.Count);
        for (int i = 0; i < placed; i++)
        {
            var entity = new PokemonEntity(0, 0)
            {
                POKEMON = team[i],
                ENEMY = true,
                BOSS = boss && i == 0 && team[i].LEGENDARY
            };
            FREE_ENEMY_TILES[i].InsertPokemon(entity);

            entity.ClampInside(PlayArea);
            entity.START = entity.GetPosition();
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
