using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public static class ItemDrag
{
    private static GameInterfaceElement? _ghost;
    private static Item? _shown;

    public static bool Active => ItemLogic.Carried != null;

    public static void Update()
    {
        Item? carried = ItemLogic.Carried;

        if (carried == null)
        {
            _ghost = null;
            _shown = null;
            return;
        }

        if (!ReferenceEquals(carried, _shown) || _ghost == null)
        {
            _shown = carried;
            _ghost = new GameInterfaceElement(0, 0, carried.ICON.Width, carried.ICON.Height, VISIBLE: true);
            _ghost.SetRendererConfig(GameRendererConfig.Sprite(ItemDatabase.TEXTURE, carried.ICON));
        }

        Point mouse = GameMouse.GetPos();
        _ghost.SetPosition(new Point(mouse.X - _ghost.SIZE_X / 2, mouse.Y - _ghost.SIZE_Y / 2));
    }

    public static GameElement? Ghost => Active ? _ghost : null;
}
