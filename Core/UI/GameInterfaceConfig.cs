public class GameInterfaceConfig
{
    private bool IS_HOVER = false;
    private bool IS_DRAGGABLE = false;

    public GameInterfaceConfig(bool IS_HOVER = false, bool IS_DRAGGABLE = false)
    {
        this.IS_HOVER = IS_HOVER;
        this.IS_DRAGGABLE = IS_DRAGGABLE;
    }

    public bool IsHover() { return IS_HOVER; }

    public bool IsDraggable() { return IS_DRAGGABLE; }

}

