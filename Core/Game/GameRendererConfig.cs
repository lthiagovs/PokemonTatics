using System;
using Microsoft.Xna.Framework;

namespace GAME.CORE;

public class GameRendererConfig
{
    public Color COLOR;
    public String TEXT;
    public String TEXTURE_PATH;

    public GameRendererConfig(Color COLOR)
    {
        this.COLOR = COLOR;
    }


    //DEFAULT CONFIG
    public GameRendererConfig()
    {
        this.COLOR = Color.White;
    }

}