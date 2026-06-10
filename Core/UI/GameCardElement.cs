using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.UI;

public class GameCardElement : GameInterfaceElement
{

    private short SIZE_X_STATE;
    private short SIZE_Y_STATE;

    public GameCardElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { 
        this.SIZE_X_STATE = SIZE_X; 
        this.SIZE_Y_STATE = SIZE_Y; 
    }

    public override void Update()
    {

        this.SIZE_X = SIZE_X_STATE;
        this.SIZE_Y = SIZE_Y_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.CONFIG.IsHover()){ 
                this.SIZE_X = (short)(this.SIZE_X * 1.05); 
                this.SIZE_Y = (short)(this.SIZE_Y * 1.05);
            }

            //DRAG
            if(this.CONFIG.IsDraggable() && GameMouse.LeftPressed()) this.Move(GameMouse.GetPos());

        }

    }

}