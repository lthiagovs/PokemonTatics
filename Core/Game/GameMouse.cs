using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GAME.CORE;

public static class GameMouse{

    private static short CURSOR_SIZE = 1;

    private static GameElement CARRY_ELEMENT = null;

    #region HELPERS
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

    public static void SetCarryElement(GameElement ELEMENT) { GameMouse.CARRY_ELEMENT = ELEMENT; }
    public static void ClearCarryElement() { GameMouse.CARRY_ELEMENT = null; } 
    public static GameElement GetCarryElement() { return GameMouse.CARRY_ELEMENT; }
    public static bool IsCarryElement() { return GameMouse.CARRY_ELEMENT != null; }
    #endregion

    #region STATES
    public static void SetStateHover()
    {
        Mouse.SetCursor(MouseCursor.Hand);
    }

    public static void SetStateDefault()
    {
        Mouse.SetCursor(MouseCursor.Arrow);
    }

    #endregion

    #region INPUTS

    private static MouseState _previousState;

    public static void Update()
    {
        if(GameMouse.CARRY_ELEMENT != null) CARRY_ELEMENT.SetPosition(GameMouse.GetPos());
        if(GameMouse.RightPressed()) GameMouse.ClearCarryElement();
        _previousState = Mouse.GetState();
    }

    public static bool LeftPressed()
    {
        MouseState _state = GameMouse.GetState();
        return _state.LeftButton == ButtonState.Pressed && _previousState.LeftButton == ButtonState.Released;
    }

    public static bool RightPressed()
    {
        MouseState _state = GameMouse.GetState();
        return _state.RightButton == ButtonState.Pressed && _previousState.RightButton == ButtonState.Released;
    }
    #endregion
    
}