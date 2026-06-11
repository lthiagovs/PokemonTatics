using GAME.CORE;
using Microsoft.Xna.Framework;

public class GameEntity : GameElement
{

    public GameEntity(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE) : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE) { 
        // SET DEFAULT
        this.CONFIG = new GameEntityRenderConfig();
    }

    public GameDirection DIRECTION = GameDirection.TOP_RIGHT;
    private GameEntityRenderConfig CONFIG;
    private int FRAME = 0;
    public bool IS_MOVING = false;

    public int GetFrame() { return this.FRAME; }

    public GameEntityRenderConfig GetEntityConfig() { return this.CONFIG; }
    public void SetEntityConfig(GameEntityRenderConfig CONFIG) {this.CONFIG = CONFIG; }

    public void UpdateAnimation()
    {
        if(!IS_MOVING) { FRAME = 0; return; }
        
        if(GameTimeLogic.FRAC_TICK && GameTimeLogic.FRAC % 2 == 0)
        FRAME = (FRAME + 1) % 4;
    }

    public override void Update()
    {
        this.UpdateAnimation();
        this.UpdateEffect();
    }
    

}

public class GameEntityRenderConfig
{
    
    public int SLICE_SIZE;
    public int SIZE;
    public string TEXTURE_PATH;
    public int ANIMATION_SPEED;

    public GameEntityRenderConfig(int SLICE_SIZE = 32, int SIZE = 1, string TEXTURE_PATH = null, int ANIMATION_SPEED = 0){
        this.SLICE_SIZE = SLICE_SIZE;
        this.SIZE = SIZE;
        this.TEXTURE_PATH = TEXTURE_PATH;
        this.ANIMATION_SPEED = ANIMATION_SPEED;
    }

}