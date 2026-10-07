using PokemonTFT.Core;
using PokemonTFT.Logic;

namespace PokemonTFT.UI;

public sealed class ItemSlotElement : GameInterfaceElement
{
    private const double HOVER_DELAY = 0.6;

    public ItemPile? STACK;

    private double _hoverElapsed;

    public ItemSlotElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE,
                           GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    public override void Update()
    {
        base.Update();

        if (STACK == null || STACK.COUNT <= 0 || !IsHovered())
        {
            _hoverElapsed = 0;
            return;
        }

        if (ItemLogic.Carried != null || GameMouse.HasCarry()) return;

        GameMouse.RequestHoverCursor();

        _hoverElapsed += GameTimeLogic.DELTA;
        if (_hoverElapsed >= HOVER_DELAY) ItemModal.Inspect(this, STACK.ITEM);

        if (!GameMouse.LeftPressed()) return;

        GameMouse.ConsumeClick();
        ItemLogic.PickUp(STACK.ITEM);
    }
}
