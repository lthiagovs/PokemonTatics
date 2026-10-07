using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public sealed class TypeChipElement : GameButton
{
    public PokemonType TYPE;

    public TypeChipElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE,
                           GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    public override void Update()
    {
        base.Update();

        if (VISIBLE && ENABLED && IsHovered()) TypeHighlight.Request(TYPE);
    }
}
