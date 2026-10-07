using Microsoft.Xna.Framework;

namespace PokemonTFT.Models;

public static class PokemonTypeColors
{
    private static readonly Color[] COLORS = BuildColors();

    private static Color[] BuildColors()
    {
        var colors = new Color[System.Enum.GetValues<PokemonType>().Length];

        colors[(int)PokemonType.GRASS]    = new Color(120, 200,  80);
        colors[(int)PokemonType.FIRE]     = new Color(255, 130,  60);
        colors[(int)PokemonType.WATER]    = new Color( 90, 160, 255);
        colors[(int)PokemonType.GROUND]   = new Color(205, 165, 100);
        colors[(int)PokemonType.ROCK]     = new Color(180, 160, 110);
        colors[(int)PokemonType.STEEL]    = new Color(190, 200, 215);
        colors[(int)PokemonType.FAIRY]    = new Color(255, 160, 220);
        colors[(int)PokemonType.DARK]     = new Color(130, 105,  95);
        colors[(int)PokemonType.GHOST]    = new Color(150, 120, 200);
        colors[(int)PokemonType.POISON]   = new Color(190, 110, 200);
        colors[(int)PokemonType.BUG]      = new Color(170, 200,  70);
        colors[(int)PokemonType.DRAGON]   = new Color(130, 110, 240);
        colors[(int)PokemonType.FLY]      = new Color(170, 190, 245);
        colors[(int)PokemonType.ICE]      = new Color(160, 230, 235);
        colors[(int)PokemonType.NORMAL]   = new Color(220, 215, 195);
        colors[(int)PokemonType.PSYCHIC]  = new Color(255, 120, 160);
        colors[(int)PokemonType.ELECTRIC] = new Color(255, 220,  80);
        colors[(int)PokemonType.FIGHT]    = new Color(215,  95,  85);

        return colors;
    }

    public static Color Of(PokemonType TYPE) => COLORS[(int)TYPE];
}
