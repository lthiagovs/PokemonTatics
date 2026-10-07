using PokemonTFT.Core;

namespace PokemonTFT.UI;

public static class GameTooltip
{
    private static GameHint? _hint;
    private static object? _owner;
    private static bool _claimed;

    private static GameHint Hint => _hint ??= new GameHint();

    public static void BeginFrame() => _claimed = false;

    public static void Show(object OWNER, string TEXT)
    {
        if (_claimed && !ReferenceEquals(_owner, OWNER)) return;

        _owner   = OWNER;
        _claimed = true;

        Hint.SetText(TEXT);
        Hint.VISIBLE = true;
        Hint.FollowMouse();
    }

    public static void Hide(object OWNER)
    {
        if (!ReferenceEquals(_owner, OWNER)) return;
        _owner = null;
    }

    public static void EndFrame()
    {
        if (_claimed) return;
        Clear();
    }

    public static void Clear()
    {
        _owner   = null;
        _claimed = false;
        if (_hint != null) _hint.VISIBLE = false;
    }

    public static GameElement? GetElement() => _hint is { VISIBLE: true } ? _hint : null;
}
