using System;
using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.UI;

public class GameInterfaceElement : GameElement
{
    public bool MOUSE_HOVER = false;
    public bool DRAGGABLE = false;

    public GameInterfaceElement Parent;

    public Color COLOR_STATE;

    public GameInterfaceElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE)
    {
        this.COLOR_STATE = this.GetRendererConfig().COLOR;
        this.Parent = PARENT;
        if (PARENT != null) { this.SetPosition(new Point(POS_X, POS_Y)); }
    }

    public override Point Move(Point NEW_POSITION)
    {
        this.SetPosition(new Point(NEW_POSITION.X - this.SIZE_X/2, NEW_POSITION.Y - this.SIZE_Y/2));
        return this.GetPosition();
    }

    public override void SetRendererConfig(Color COLOR, String TEXT = null)
    {
        this.COLOR_STATE = COLOR;
        base.SetRendererConfig(COLOR, TEXT);
    }

    public override Point SetPosition(Point NEW_POSITION)
    {
        if(this.Parent == null) return base.SetPosition(NEW_POSITION);

        Point new_pos;
        new_pos.X = this.Parent.GetPosition().X+NEW_POSITION.X;
        new_pos.Y = this.Parent.GetPosition().Y+NEW_POSITION.Y;
        return base.SetPosition(new_pos);
        
    }

    public override void Update()
    {
        this.GetRendererConfig().COLOR = COLOR_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.MOUSE_HOVER){this.RENDER_CONFIG.COLOR = Color.Black; GameMouse.SetStateHover();}

            //DRAG
            if(this.DRAGGABLE && GameMouse.LeftPressed()) this.Move(GameMouse.GetPos());

        }

    }

}
