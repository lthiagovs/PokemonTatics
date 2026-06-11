using System;
using Microsoft.Xna.Framework;

namespace GAME.CORE;
public class GameElement
{

    private Point POSITION;
    public short SIZE_X;
    public short SIZE_Y;
    public bool VISIBLE;
    protected GameRendererConfig RENDER_CONFIG;

    public GameElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE)
    {
        this.SetPosition(new Point(POS_X, POS_Y));
        this.SIZE_X = SIZE_X;
        this.SIZE_Y = SIZE_Y;
        this.VISIBLE = VISIBLE;
        this.RENDER_CONFIG = new GameRendererConfig();
    }

    protected virtual void PostInit() { }

    #region OPERATIONS
    public short[] GetScale()
    {
        return [this.SIZE_X, this.SIZE_X];
    }

    public short[] Resize(short NEW_SIZE_X, short NEW_SIZE_Y)
    {
        this.SIZE_X = NEW_SIZE_X;
        this.SIZE_Y = NEW_SIZE_Y;

        return this.GetScale();
    }

    public short[] Scale(short SCALE)
    {
        this.SIZE_X*=SCALE;
        this.SIZE_Y*=SCALE;

        return this.GetScale();
    }

    public virtual Point Move(Point NEW_POSITION)
    {
        return this.SetPosition(NEW_POSITION);
    }

    public virtual Point Move(short MOVE_X = 0, short MOVE_Y = 0)
    {

        return this.SetPosition(new Point(this.POSITION.X+MOVE_X, this.POSITION.Y+MOVE_Y));
    }

    public virtual Point GetPosition() { return this.POSITION; }

    public virtual Point SetPosition(Point NEW_POSITION) { return this.POSITION = NEW_POSITION; }

    protected Point SizeAsPoint()
    {
        return new Point(this.SIZE_X, this.SIZE_Y);
    }

    public virtual Rectangle GetRectangle()
    {
        return new Rectangle(this.GetPosition(), this.SizeAsPoint());
    } 
    #endregion

    #region RENDER & LOGIC
    public virtual GameRendererConfig GetRendererConfig() { return this.RENDER_CONFIG; }

    public virtual void SetRendererConfig(GameRendererConfig CONFIG) { this.RENDER_CONFIG = CONFIG; }

    public RenderEffect EFFECT = null;

    public void SetEffect(RenderEffect effect) { EFFECT = effect; }

    public void UpdateEffect()
    {
        if(EFFECT == null) return;
        EFFECT.Update(GameTimeLogic.DELTA);
        if(EFFECT.DONE) EFFECT = null;
    }

    public virtual void Update() {}
    #endregion


}