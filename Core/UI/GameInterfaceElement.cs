using Microsoft.Xna.Framework;
using PokemonTFT.Core;

namespace PokemonTFT.UI;

public class GameInterfaceElement : GameElement
{
    public GameInterfaceElement? PARENT;

    public bool HOVERABLE;

    protected Color COLOR_STATE = Color.White;

    public GameInterfaceElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE)
    {
        this.PARENT = PARENT;
    }

    public override Point GetPosition()
        => PARENT == null ? base.GetPosition() : base.GetPosition() + PARENT.GetPosition();

    public override void SetRendererConfig(GameRendererConfig CONFIG)
    {
        COLOR_STATE = CONFIG.COLOR;
        base.SetRendererConfig(CONFIG);
    }

    protected bool IsHovered() => GameMouse.IsOver(this);

    public override void Update()
    {
        GetRendererConfig().COLOR = COLOR_STATE;

        if (!HOVERABLE || !IsHovered()) return;

        OnHover();
    }

    protected virtual void OnHover() { }
}
