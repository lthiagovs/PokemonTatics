using System;
using System.Collections.Generic;
using ENGINE.MODELS;
using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.TABLE;

public static class GameTable
{
    private static List<GameElement> TABLE_ELEMENTS = new List<GameElement>();
    public static int TABLE_SIZE_X { get; private set; }
    public static int TABLE_SIZE_Y { get; private set; }
    private static readonly Rectangle TS_TILE = new Rectangle(24, 24, 24, 24);
    public static List<GameElement> GetTable() { return GameTable.TABLE_ELEMENTS; }

    //ENEMY TEAM
    private static int ENEMY_SIZE = 7;

    public static void Initialize()
    {
        int totalTiles = 20;
        int tileSize   = GameRenderer.GetScreenWidth() / totalTiles;
        int yTiles     = GameRenderer.GetScreenHeight() / tileSize;
        TABLE_SIZE_X = totalTiles - 2;
        TABLE_SIZE_Y = yTiles - 2;
        for (int y = 0; y < TABLE_SIZE_Y; y++)
        {
            for (int x = 0; x < TABLE_SIZE_X; x++)
            {
                int posX = tileSize + (tileSize * x);
                int posY = tileSize + (tileSize * y);
                var element = new GameTableElement(
                    (short)posX, (short)posY,
                    (short)tileSize, (short)tileSize, true);
                
                element.CONFIG = new GameInterfaceConfig(true, false);
                element.PLAYER_OWN = (y >= 3 && y <= 6);
                GameRendererConfig eConfig = new GameRendererConfig();
                eConfig.RECTANGLE = TS_TILE;
                eConfig.TEXTURE_PATH = "Environment/tileset";
                element.SetRendererConfig(eConfig);
                TABLE_ELEMENTS.Add(element);
            }
        }
    }

    //ENEMY TEAM GENERATION

    private static List<Pokemon> GetRandomDecks()
    {
        Random _random = new Random();
        GameDeck.PokemonDecks.Clear();
        List<Pokemon> _pkmList = new List<Pokemon>();

        for (int i = 0; i < GameTable.ENEMY_SIZE; i++)
        {
            int index = _random.Next(0, PokemonDatabase.PokemonList.Count);
            _pkmList.Add(PokemonDatabase.PokemonList[index]);
        }

        return _pkmList;
    }
    
    public static void InitializeEnemyTeam()
    {
        List<Pokemon> _pkmList = GetRandomDecks();
        Random _random = new Random();

        foreach(Pokemon pkm in _pkmList)
        {
            bool repeat = true;
            do{
                int index = _random.Next(0, 53);
                GameTableElement tElement = GameTable.TABLE_ELEMENTS[index] as GameTableElement;
                if(!tElement.HasPokemon())
                {
                    PokemonEntity _pkmEntity = new PokemonEntity(0, 0, 32, 32, true);
                    _pkmEntity.POKEMON = pkm;

                    GameEntityRenderConfig cfg = new GameEntityRenderConfig();
                    cfg.TEXTURE_PATH = "Pokemons/"+pkm.NAME+"/moveset";
                    cfg.SLICE_SIZE = 32;
                    cfg.SIZE = 3;
                    cfg.ANIMATION_SPEED = 1;

                    _pkmEntity.SetEntityConfig(cfg);
                    tElement.InsertPokemon(_pkmEntity);

                    repeat = false;
                }

            }while(repeat);
        }


    }

}