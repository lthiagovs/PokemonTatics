using Microsoft.Xna.Framework;

namespace PokemonTFT.Core;

public sealed class GameRendererConfig
{
    public Color COLOR = Color.White;
    public string? TEXT;
    public string? TEXTURE_PATH;
    public Rectangle? SOURCE;
    public bool IS_SLICE;
    public int SLICE_SIZE;
    public int SLICE_PROPORTION = 1;
    public bool IS_HOVERING;

    public float FONT_SCALE = GameFonts.SMALL;

    public int TEXT_SHADOW;

    public Color TEXT_SHADOW_COLOR = Color.Black;

    public static GameRendererConfig Solid(Color COLOR) => new() { COLOR = COLOR };

    public static GameRendererConfig Label(string TEXT, Color? COLOR = null, float FONT_SCALE = GameFonts.SMALL, int TEXT_SHADOW = 0)
        => new()
        {
            TEXT        = TEXT,
            COLOR       = COLOR ?? Color.White,
            FONT_SCALE  = FONT_SCALE,
            TEXT_SHADOW = TEXT_SHADOW
        };

    public static GameRendererConfig Sprite(string TEXTURE_PATH, Rectangle? SOURCE = null, Color? COLOR = null)
        => new() { TEXTURE_PATH = TEXTURE_PATH, SOURCE = SOURCE, COLOR = COLOR ?? Color.White };

    public static GameRendererConfig NineSlice(string TEXTURE_PATH, int SLICE_SIZE, int SLICE_PROPORTION = 1, Color? COLOR = null)
        => new()
        {
            TEXTURE_PATH     = TEXTURE_PATH,
            IS_SLICE         = true,
            SLICE_SIZE       = SLICE_SIZE,
            SLICE_PROPORTION = SLICE_PROPORTION,
            COLOR            = COLOR ?? Color.White
        };
}
