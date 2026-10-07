using System;

namespace PokemonTFT.Core;

public static class GameHost
{
    public static Action? TOGGLE_FULLSCREEN;
    public static Action? QUIT_HANDLER;

    public static bool FULLSCREEN { get; set; }

    public static void ToggleFullscreen() => TOGGLE_FULLSCREEN?.Invoke();

    public static void Quit() => QUIT_HANDLER?.Invoke();
}
