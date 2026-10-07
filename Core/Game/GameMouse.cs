using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PokemonTFT.Core;

public static class GameMouse
{
    private const int CURSOR_SIZE = 1;

    private static MouseState _current;
    private static MouseState _previous;
    private static bool _clickConsumed;
    private static bool _hoverCursor;
    private static bool _cursorIsHand;
    private static bool _cursorApplied;

    private static GameElement? _carry;

    #region FRAME
    public static void BeginFrame()
    {
        _previous      = _current;
        _current       = Mouse.GetState();
        _clickConsumed = false;
        _hoverCursor   = false;
    }

    public static void EndFrame()
    {
        _carry?.SetPosition(GetPos());

        _carry?.Update();

        if (_cursorApplied && _hoverCursor == _cursorIsHand) return;

        _cursorIsHand  = _hoverCursor;
        _cursorApplied = true;
        Mouse.SetCursor(_cursorIsHand ? MouseCursor.Hand : MouseCursor.Arrow);
    }
    #endregion

    #region STATE
    public static Point GetPos() => new(_current.X, _current.Y);

    public static Rectangle GetRectangle() => new(_current.X, _current.Y, CURSOR_SIZE, CURSOR_SIZE);

    public static bool IsOver(GameElement ELEMENT) => ELEMENT.GetRectangle().Intersects(GetRectangle());

    public static void RequestHoverCursor() => _hoverCursor = true;
    #endregion

    #region BUTTONS
    public static bool LeftPressed()
        => !_clickConsumed
        && _current.LeftButton == ButtonState.Pressed
        && _previous.LeftButton == ButtonState.Released;

    public static bool RightPressed()
        => _current.RightButton == ButtonState.Pressed
        && _previous.RightButton == ButtonState.Released;

    public static void ConsumeClick() => _clickConsumed = true;
    #endregion

    #region CARRY
    public static void SetCarry(GameElement? ELEMENT) => _carry = ELEMENT;

    public static void ClearCarry() => _carry = null;

    public static GameElement? GetCarry() => _carry;

    public static bool HasCarry() => _carry != null;
    #endregion
}
