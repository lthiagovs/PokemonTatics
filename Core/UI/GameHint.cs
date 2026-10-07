using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;

namespace PokemonTFT.UI;

public sealed class GameHint : GameInterfaceElement
{
    private const int PADDING_X = 10;
    private const int PADDING_Y = 6;
    private const int MIN_WIDTH  = 60 * 3;
    private const int MIN_HEIGHT = 24 * 3;
    private const int CURSOR_OFFSET = 16;

    private readonly GameInterfaceElement _text;
    private string _current = string.Empty;

    public GameHint() : base(0, 0, MIN_WIDTH, MIN_HEIGHT, VISIBLE: false)
    {
        SetRendererConfig(GameRendererConfig.NineSlice("UI/Windows/card", SLICE_SIZE: 24, SLICE_PROPORTION: 2));

        _text = new GameInterfaceElement(0, 0, 0, 0, VISIBLE: true, PARENT: this);
        _text.SetRendererConfig(GameRendererConfig.Label(string.Empty));
        AddChild(_text);
    }

    public void SetText(string TEXT)
    {
        TEXT ??= string.Empty;
        if (TEXT == _current) return;
        _current = TEXT;

        Vector2 measured = GameRenderer.GetGameFont().MeasureString(TEXT);
        SIZE_X = Math.Max(MIN_WIDTH,  (int)measured.X + PADDING_X * 2);
        SIZE_Y = Math.Max(MIN_HEIGHT, (int)measured.Y + PADDING_Y * 2);

        _text.SIZE_X = (int)measured.X;
        _text.SIZE_Y = (int)measured.Y;
        _text.SetPosition(new Point((SIZE_X - (int)measured.X) / 2, (SIZE_Y - (int)measured.Y) / 2));
        _text.GetRendererConfig().TEXT = TEXT;
    }

    public void FollowMouse()
    {
        Point mouse = GameMouse.GetPos();
        SetPosition(new Point(mouse.X + CURSOR_OFFSET, mouse.Y + CURSOR_OFFSET));
    }
}
