using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public sealed class GameBonusElement : GameInterfaceElement
{
    public PokemonType TYPE { get; set; }

    public GameBonusElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    public override void Update()
    {
        base.Update();

        if (IsHovered()) GameTooltip.Show(this, GameBonusLogic.GetBonusDescription(TYPE));
        else GameTooltip.Hide(this);
    }
}
