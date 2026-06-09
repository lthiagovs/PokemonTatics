
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GAME.CORE;

public static class GameMouse{

    private static short CURSOR_SIZE = 1;

    private static MouseState GetState() { return Mouse.GetState(); }

    public static Point GetPos()
    {
        MouseState _state = GameMouse.GetState();
        return new Point(_state.X, _state.Y);
    }

    public static Rectangle GetRectangle()
    {
        MouseState _state = GameMouse.GetState();
        return new Rectangle(_state.X, _state.Y, CURSOR_SIZE, CURSOR_SIZE);
    }

    public static void SetStateHover()
    {
        Mouse.SetCursor(MouseCursor.Hand);
    }

    public static void SetStateDefault()
    {
        Mouse.SetCursor(MouseCursor.Arrow);
    }

    public static bool LeftPressed()
    {
        MouseState _state = GameMouse.GetState();
        return _state.LeftButton == ButtonState.Pressed;
    }
    
}