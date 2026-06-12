using System;
using Microsoft.Xna.Framework;

namespace GAME.CORE;

public class GameRendererConfig
{
    public Color COLOR = Color.White;
    public String TEXT = null;
    public bool IS_HOVERING = false;
    public String TEXTURE_PATH = null;
    public Rectangle? RECTANGLE = null;
    public bool IS_SLICE = false;
    public int SLICE_SIZE = 0;
    public int SLICE_PROPORTION = 1;

    //ALLOW DEFAULT CONFIG
    public GameRendererConfig() { }
    
    public GameRendererConfig(Color COLOR, String TEXT, String TEXTURE_PATH, Rectangle RECTANGLE, bool IS_SLICE)
    {
        this.COLOR = COLOR;
        this.TEXT = TEXT;
        this.TEXTURE_PATH = TEXTURE_PATH;
        this.RECTANGLE = RECTANGLE;
        this.IS_SLICE = IS_SLICE;
    }

}