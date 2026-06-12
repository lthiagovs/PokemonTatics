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
        GameTableLogic.DetectGameEnd();
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

    private static List<PokemonEntity> GetAllLivePokemons()
    {
        List<PokemonEntity> _pokemons = new List<PokemonEntity>();
        foreach(GameElement e in GameTableElement.GetTableElements())
            if(e is PokemonEntity){ if(!(e as PokemonEntity).DEAD) _pokemons.Add(e as PokemonEntity); }
        
        return _pokemons;
    }

    private static List<PokemonEntity> GetEnemyPokemons()
    {
        return GameTableLogic.GetAllLivePokemons().FindAll(p => p.ENEMY);
    }

    private static List<PokemonEntity> GetPlayerPokemons()
    {
        return GameTableLogic.GetAllLivePokemons().FindAll(p => !p.ENEMY);
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

        if(_pokemon.Count == 0) return null;

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
        List<PokemonEntity> _pokemons = GetAllLivePokemons();
        foreach(PokemonEntity e in _pokemons)
        {
            if(e.GetTarget() == null)
            {
                PokemonEntity closest = GameTableLogic.GetClosestEnemy(e);
                if(closest != null) e.SetTarget(closest);
            }
        }
    }

    private static void ClearTableTargets()
    {
        List<PokemonEntity> _pokemons = GetAllLivePokemons();
        foreach(PokemonEntity e in _pokemons) e.SetTarget(null);

    }

    //Movement Logic
    private static void MovePokemonToTarget(PokemonEntity POKEMON)
    {
        if(POKEMON.GetTarget() == null) { POKEMON.IS_MOVING = false; return; }
        if(POKEMON.GetRectangle().Intersects(POKEMON.GetTarget().GetRectangle())) { 
            GameTableLogic.AttackTarget(POKEMON.GetTarget());
            POKEMON.IS_MOVING = false; 
            return; 
        }

        POKEMON.SetAnimation(null);

        POKEMON.IS_MOVING = true;

        int dx = POKEMON.GetTarget().GetPosition().X - POKEMON.GetPosition().X;
        int dy = POKEMON.GetTarget().GetPosition().Y - POKEMON.GetPosition().Y;

        POKEMON.POKEMON.ChargeAttack(1);

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
        List<PokemonEntity> _pokemons = GameTableLogic.GetAllLivePokemons();

        foreach(PokemonEntity p in _pokemons)
        {
            GameTableLogic.MovePokemonToTarget(p);
        }

    }

    private static void UpdatePokemons() { foreach(PokemonEntity p in GameTableLogic.GetAllLivePokemons()) p.Update(); }
    
    private static void FaceTarget(PokemonEntity POKEMON)
    {
        if(POKEMON.GetTarget() == null) return;

        int dx = POKEMON.GetTarget().GetPosition().X - POKEMON.GetPosition().X;
        int dy = POKEMON.GetTarget().GetPosition().Y - POKEMON.GetPosition().Y;

        if      (dx > 0 && dy < 0) POKEMON.DIRECTION = GameDirection.TOP_RIGHT;
        else if (dx < 0 && dy < 0) POKEMON.DIRECTION = GameDirection.TOP_LEFT;
        else if (dx > 0 && dy > 0) POKEMON.DIRECTION = GameDirection.BOTTOM_RIGHT;
        else if (dx < 0 && dy > 0) POKEMON.DIRECTION = GameDirection.BOTTOM_LEFT;
        else if (dx > 0)            POKEMON.DIRECTION = GameDirection.RIGHT;
        else if (dx < 0)            POKEMON.DIRECTION = GameDirection.LEFT;
        else if (dy > 0)            POKEMON.DIRECTION = GameDirection.BOTTOM;
        else if (dy < 0)            POKEMON.DIRECTION = GameDirection.TOP;
    }

    //Attack Logic
    private static void AttackTarget(PokemonEntity POKEMON)
    {
        if(POKEMON.GetTarget()==null) return;

        //every 1s
        if(!(GameTimeLogic.FRAC_TICK && GameTimeLogic.FRAC % 10 == 0)) return;

        GameMusic.PlayHit();

        GameTableLogic.FaceTarget(POKEMON);
        POKEMON.SetAnimation(new GameAnimation { FRAMES = new[] { 2, 1, 0, 1 }, FRAME_SPEED = 0.06f, LOOP = false });
        POKEMON.POKEMON.Attack(POKEMON.GetTarget().POKEMON);

        if (POKEMON.POKEMON.SPECIAL_EFFECT)
        {
            POKEMON.POKEMON.SPECIAL_EFFECT = false;

            Point targetPos = POKEMON.GetTarget().GetPosition();

            GameEffect effect = new GameEffect(GameEffectType.TARGET, 0, 10, POKEMON.ENEMY, 
            targetPos.X, targetPos.Y, $"Effects/{POKEMON.POKEMON.EFFECT}", 4, POKEMON.GetTarget());
        }

        POKEMON.GetTarget().SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.3f));
        if(POKEMON.GetTarget().POKEMON.HP <= 0) KillPokemon(POKEMON.GetTarget());
    }

    private static void KillPokemon(PokemonEntity POKEMON)
    {
        foreach(PokemonEntity p in GetAllLivePokemons())
            if(p.GetTarget() == POKEMON) p.SetTarget(null);

        POKEMON.DEAD = true;
        POKEMON.VISIBLE = false;

        if(POKEMON.ENEMY) GameGlobals.ChangeMana(+1);
    }

    private static void DetectGameEnd()
    {
        if(GameTableLogic.GetEnemyPokemons().Count != 0 && GameTableLogic.GetPlayerPokemons().Count != 0) return;

        if(GameTableLogic.GetPlayerPokemons().Count == 0) GameGlobals.PLAYER_HP-=10;
        else GameMusic.PlayVictory();
        
        GameTableLogic.GetAllPokemons().ForEach(e => e.POKEMON.GainXP(10));
        //RESET POSITIONS:
        GameTableLogic.GetAllPokemons().ForEach(e => e.SetPosition(e.START));
        GameTableLogic.GetAllPokemons().ForEach(e => e.POKEMON.HP=e.POKEMON.MAX_HP);
        GameTableLogic.GetAllPokemons().ForEach(e => e.IS_MOVING = false);
        GameTableLogic.GetAllPokemons().ForEach(e => e.DEAD = false);
        GameTableLogic.GetAllPokemons().ForEach(e => e.VISIBLE = true);
        GameTableLogic.GetAllPokemons().ForEach(e => e.POKEMON.SPECIAL_COUNTER = 0);
        GameTableLogic.GetAllPokemons().ForEach(e => e.SetAnimation(null));
        GameEffect.EFFECTS.Clear();
        GameGlobals.GAME_STARTED = false;
        GameTableLogic.ClearEnemyPokemons();
        GameTable.InitializeEnemyTeam();
        GameGlobals.ChangeMana(+3);
        GameGlobals.LEVEL+=1;
        GameBonus.Initialize();
    }

    private static void ClearEnemyPokemons()
    {
        GameTableElement.GetTableElements().RemoveAll(e => e is PokemonEntity && (e as PokemonEntity).ENEMY);

        foreach(GameElement e in GameTable.TABLE_ELEMENTS)
        {
            if(e is GameTableElement)
            {
                GameTableElement tEle = e as GameTableElement;
                if(tEle.HasPokemon() && tEle.GetPokemon().ENEMY) tEle.ClearPokemon();
            }
        }
    }

}