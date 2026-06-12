using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public class GameEffect
{


    public static List<GameEffect> EFFECTS = new List<GameEffect>();

    public GameEffectType TYPE;
    public int TOTAL_DAMAGE;
    public int SPEED;
    public bool ENEMY;
    public int X;
    public int Y;
    public int FRAMES;
    public PokemonEntity TARGET;
    public String PATH;

    //INTERNALS
    public bool DONE = false;
    public double _elapsed;
    public double FRAME_SPEED = 0.1;
    public int _currentIndex = 0;

    public GameEffect(GameEffectType TYPE, int TOTAL_DAMAGE, int SPEED, bool ENEMY, int X, int Y, String PATH, int FRAMES, PokemonEntity TARGET = null)
    {
        this.TYPE = TYPE;
        this.TOTAL_DAMAGE = TOTAL_DAMAGE;
        this.SPEED = SPEED;
        this.ENEMY = ENEMY;
        this.X = X;
        this.Y = Y;
        this.PATH = PATH;
        this.FRAMES = FRAMES;
        this.TARGET = TARGET;

        GameEffect.EFFECTS.Add(this);
    }

    public void UpdateTarget()
    {
        if (this.TARGET==null) { GameEffect.EFFECTS.Remove(this); return; }

        Point TARGET_POSITION = TARGET.GetPosition();
        this.X = TARGET_POSITION.X + (TARGET.SIZE_X/2);
        this.Y = TARGET_POSITION.Y + (TARGET.SIZE_Y/2);

        if(this.DONE) { GameEffect.EFFECTS.Remove(this); return; }
        _elapsed += GameTimeLogic.DELTA;
        if(_elapsed >= FRAME_SPEED)
        {
            _elapsed = 0;
            _currentIndex++;
            if(_currentIndex >= FRAMES){ DONE = true; TARGET.POKEMON.HP -= this.TOTAL_DAMAGE; return; }
            
        }

    }

    public void Update()
    {
        if(this.TYPE == GameEffectType.TARGET) UpdateTarget();
    }

    public static void UpdateAll()
    {
        for (int i = GameEffect.EFFECTS.Count - 1; i >= 0; i--)
            GameEffect.EFFECTS[i].Update();
    }

}

public enum GameEffectType
{
    MISSILE,
    TARGET,
    AREA,
    HEAL
}