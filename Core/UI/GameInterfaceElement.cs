using System;
using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.UI;

public class GameInterfaceElement : GameElement
{
    public GameInterfaceElement PARENT;
    public GameInterfaceConfig CONFIG;

    private Color COLOR_STATE;

    public GameInterfaceElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE)
    {
        this.COLOR_STATE = this.GetRendererConfig().COLOR;

        //DEFAULT CONFIGS
        this.RENDER_CONFIG = new GameRendererConfig(Color.White, null, null, Rectangle.Empty, false);
        this.CONFIG = new GameInterfaceConfig(false, false);

        this.PARENT = PARENT;
        if (PARENT != null) { this.SetPosition(new Point(POS_X, POS_Y)); }
    }

    public override Point Move(Point NEW_POSITION)
    {
        this.SetPosition(new Point(NEW_POSITION.X - this.SIZE_X/2, NEW_POSITION.Y - this.SIZE_Y/2));
        return this.GetPosition();
    }

    public override void SetRendererConfig(GameRendererConfig CONFIG)
    {
        this.COLOR_STATE = CONFIG.COLOR;
        base.SetRendererConfig(CONFIG);
    }

    public override Point SetPosition(Point NEW_POSITION)
    {
        if(this.PARENT == null) return base.SetPosition(NEW_POSITION);

        Point new_pos;
        new_pos.X = this.PARENT.GetPosition().X+NEW_POSITION.X;
        new_pos.Y = this.PARENT.GetPosition().Y+NEW_POSITION.Y;
        return base.SetPosition(new_pos);
        
    }

    public override void Update()
    {
        this.GetRendererConfig().COLOR = COLOR_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.CONFIG.IsHover()){this.RENDER_CONFIG.COLOR = Color.Black; }

            //DRAG
            if(this.CONFIG.IsDraggable() && GameMouse.LeftPressed()) this.Move(GameMouse.GetPos());

        }

    }

}
