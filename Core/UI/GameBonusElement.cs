using GAME.UI;
using GAME.CORE;
using ENGINE.MODELS;

public class GameBonusElement : GameInterfaceElement
{
    public PokemonType TYPE { get; set; }
    public GameHint HINT { get; set; }

    public GameBonusElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) : 
    base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) {}

    public override void Update()
    {
        base.Update();

        if (this.GetRectangle().Intersects(GameMouse.GetRectangle()))
        {
            if (this.HINT != null)
            {
                if (GameMouse.GetCarryElement() == HINT) return;
                this.HINT.SetText(GameBonusLogic.GetBonusDescription(TYPE));
                
                if (!this.HINT.VISIBLE)
                {
                    
                }
                
                this.HINT.VISIBLE = true;
                GameMouse.SetCarryElement(this.HINT);
            }
        }
        else
        {
            if (this.HINT != null)
            {
                if(GameMouse.GetCarryElement() == HINT) GameMouse.ClearCarryElement();
                this.HINT.Hide();
            }
        }
    }
}