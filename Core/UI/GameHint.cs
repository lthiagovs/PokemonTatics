using System;
using Microsoft.Xna.Framework;
using GAME.CORE;
using GAME.UI;

public class GameHint : GameInterfaceElement
{
    private const short PADDING_X = 10;
    private const short PADDING_Y = 6;
    private const short MIN_WIDTH  = 60*3;
    private const short MIN_HEIGHT = 24*3;

    public GameInterfaceElement _textElement;

    public GameHint(string text = "") : base(0, 0, MIN_WIDTH, MIN_HEIGHT, true)
    {

        SetRendererConfig(new GameRendererConfig(Color.White, null, "UI/Windows/card", Rectangle.Empty, true)
        {
            SLICE_SIZE = 24,
            SLICE_PROPORTION = 2
        });

        _textElement = new GameInterfaceElement(0, 0, 0, 0, false);
        _textElement.PARENT = this;
        Point mousePos = GameMouse.GetPos();
        //SetPosition(new Point(mousePos.X + 16, mousePos.Y + 16));

        _textElement.SetRendererConfig(new GameRendererConfig(Color.White, "", null, Rectangle.Empty, false));

        SetText(text);
    }

    public string TEXT
    {
        get => _textElement.GetRendererConfig().TEXT;
        set => SetText(value);
    }

    public void SetText(string text)
    {
        text ??= "";
        Vector2 measured = GameRenderer.GetGameFont().MeasureString(text);
        SIZE_X = (short)Math.Max(MIN_WIDTH,  measured.X + PADDING_X * 2);
        SIZE_Y = (short)Math.Max(MIN_HEIGHT, measured.Y + PADDING_Y * 2);

        // Offset relativo ao container, sempre baseado em (0,0)
        int textX = (SIZE_X - (int)measured.X) / 2;
        int textY = (SIZE_Y - (int)measured.Y) / 2;
        _textElement.SetPosition(new Point(textX, textY));
        _textElement.GetRendererConfig().TEXT = text;
    }
    
    public override void Update()
    {
        Point mousePos = GameMouse.GetPos();
        SetPosition(new Point(mousePos.X + 16, mousePos.Y + 16));
    }

    public void Show(System.Collections.Generic.List<GameElement> list)
    {
        Update();
        VISIBLE = true;
        _textElement.VISIBLE = true;
        if (!list.Contains(this))         list.Add(this);
        if (!list.Contains(_textElement)) list.Add(_textElement);
    }

    public void Hide()
    {
        VISIBLE = false;
        _textElement.VISIBLE = false;
    }
}