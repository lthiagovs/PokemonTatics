using System;
using System.Collections.Generic;
using GAME.CORE;
using GAME.TABLE;
using Microsoft.Xna.Framework;

public static class GameTableLogic
{

    public static void Update()
    {
        GameTableLogic.UpdatePokemons();
        if(GameGlobals.GAME_STARTED) GameTableLogic.UpdateRun();
        else GameTableLogic.UpdateIdle();
    }
    
    private static void UpdateIdle()
    {
        
        GameTableLogic.DefineIdlePokemonsDirection();
        GameTableLogic.ClearTableTargets();

    }

    private static void UpdateRun()
    {
        GameTableLogic.SetTableTargets();
        GameTableLogic.MovePokemons();
    }

    //Helpers
    private static void DefineIdlePokemonsDirection()
    {
        foreach(GameElement e in GameTable.TABLE_ELEMENTS)
        {
            if(e is GameTableElement)
            {
                GameTableElement tEle = e as GameTableElement;
                if(tEle.HasPokemon()) tEle.GetPokemon().DIRECTION = GameTableLogic.GetIdleDirection(tEle.TABLE_POSITION_X, tEle.GetPokemon().ENEMY);
            }
        }
    }

    private static GameDirection GetIdleDirection(int X, bool IS_ENEMY)
    {
        //0 - 5 | 5 - 11 | 11 - 17
        if(X >= 0 && X <= 5) return IS_ENEMY ? GameDirection.BOTTOM_RIGHT : GameDirection.TOP_RIGHT;
        else if(X > 5 && X <= 11) return IS_ENEMY ? GameDirection.BOTTOM : GameDirection.TOP;
        else return IS_ENEMY ? GameDirection.BOTTOM_LEFT : GameDirection.TOP_LEFT;
    }

    private static List<PokemonEntity> GetAllPokemons()
    {
        List<PokemonEntity> _pokemons = new List<PokemonEntity>();
        foreach(GameElement e in GameTableElement.GetTableElements())
            if(e is PokemonEntity) _pokemons.Add(e as PokemonEntity);
        
        return _pokemons;
    }

    private static List<PokemonEntity> GetEnemyPokemons()
    {
        return GameTableLogic.GetAllPokemons().FindAll(p => p.ENEMY);
    }

    private static List<PokemonEntity> GetPlayerPokemons()
    {
        return GameTableLogic.GetAllPokemons().FindAll(p => !p.ENEMY);
    }

    private static int GetDistanceOverhaul(Point P1, Point P2)
    {
        return (int)Math.Sqrt(Math.Pow(P2.X - P1.X, 2) + Math.Pow(P2.Y - P1.Y, 2));
    }

    private static PokemonEntity GetClosestEnemy(PokemonEntity POKEMON)
    {
        List<PokemonEntity> _pokemon;
        if(POKEMON.ENEMY) _pokemon = GetPlayerPokemons();
        else _pokemon = GetEnemyPokemons();

        PokemonEntity closest = _pokemon[0];
        int closestOverhaul = GameTableLogic.GetDistanceOverhaul(POKEMON.GetPosition(), _pokemon[0].GetPosition());

        foreach(PokemonEntity p in _pokemon)
        {
            int distance = GameTableLogic.GetDistanceOverhaul(POKEMON.GetPosition(), p.GetPosition());
            if(distance < closestOverhaul) {
                closest = p;
                closestOverhaul = distance;
            }
        }

        return closest;
    }

    private static void SetTableTargets()
    {
        List<PokemonEntity> _pokemons = GetAllPokemons();
        foreach(PokemonEntity e in _pokemons) if(e.GetTarget() == null) e.SetTarget(GameTableLogic.GetClosestEnemy(e));

    }

    private static void ClearTableTargets()
    {
        List<PokemonEntity> _pokemons = GetAllPokemons();
        foreach(PokemonEntity e in _pokemons) e.SetTarget(null);

    }

    //Movement Logic
    private static void MovePokemonToTarget(PokemonEntity POKEMON)
    {
        if(POKEMON.GetTarget() == null) { POKEMON.IS_MOVING = false; return; }
        if(POKEMON.GetRectangle().Intersects(POKEMON.GetTarget().GetRectangle())) { POKEMON.IS_MOVING = false; return; }

        POKEMON.IS_MOVING = true;

        int dx = POKEMON.GetTarget().GetPosition().X - POKEMON.GetPosition().X;
        int dy = POKEMON.GetTarget().GetPosition().Y - POKEMON.GetPosition().Y;

        int moveX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int moveY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        POKEMON.SetPosition(new Point(POKEMON.GetPosition().X + moveX, POKEMON.GetPosition().Y + moveY));

        if      (moveX > 0 && moveY < 0) POKEMON.DIRECTION = GameDirection.TOP_RIGHT;
        else if (moveX < 0 && moveY < 0) POKEMON.DIRECTION = GameDirection.TOP_LEFT;
        else if (moveX > 0 && moveY > 0) POKEMON.DIRECTION = GameDirection.BOTTOM_RIGHT;
        else if (moveX < 0 && moveY > 0) POKEMON.DIRECTION = GameDirection.BOTTOM_LEFT;
        else if (moveX > 0)              POKEMON.DIRECTION = GameDirection.RIGHT;
        else if (moveX < 0)              POKEMON.DIRECTION = GameDirection.LEFT;
        else if (moveY > 0)              POKEMON.DIRECTION = GameDirection.BOTTOM;
        else if (moveY < 0)              POKEMON.DIRECTION = GameDirection.TOP;

        POKEMON.IS_MOVING = true;
    }

    private static void MovePokemons()
    {
        List<PokemonEntity> _pokemons = GameTableLogic.GetAllPokemons();

        foreach(PokemonEntity p in _pokemons)
        {
            GameTableLogic.MovePokemonToTarget(p);
        }

    }

    private static void UpdatePokemons() { foreach(PokemonEntity p in GameTableLogic.GetAllPokemons()) p.Update(); }
    
    //Attack Logic
    

}