using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public static class TypeHighlight
{
    private static PokemonType? _requested;
    private static PokemonType? _active;

    public static double PULSE { get; private set; }

    public static PokemonType? Current => _active;

    public static void BeginFrame() => _requested = null;

    public static void Request(PokemonType TYPE) => _requested = TYPE;

    public static void EndFrame()
    {
        _active = _requested;
        PULSE = _active == null ? 0 : PULSE + GameTimeLogic.DELTA;
    }

    public static bool Matches(PokemonType TYPE) => _active.HasValue && _active.Value == TYPE;
}
