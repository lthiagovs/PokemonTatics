using System.Collections.Generic;
using GAME.CORE;
using Microsoft.Xna.Framework;

public static class TitleScreen
{
    
    public static List<GameElement> TITLE_ELEMENTS = new List<GameElement>();

    public static void Initialize()
    {

        GameMusic.PlayTitle();

        GameElement background = new GameElement(0,0, (short) GameRenderer.GetScreenWidth(), (short) GameRenderer.GetScreenHeight(), true);
        background.SetRendererConfig(new GameRendererConfig(Color.White, null, "Environment/wallpaper", new Rectangle(0, 0, 1920, 1200), false));

        GameButton button = new GameButton((short)(GameRenderer.GetScreenWidth()/2), (short) (GameRenderer.GetScreenHeight()/2), 100, 100, true);
        button.SetPosition(new Point((short)(GameRenderer.GetScreenWidth()/2), (short) (GameRenderer.GetScreenHeight()/2)));
        button.SetRendererConfig(new GameRendererConfig(Color.White, "PLAY", null, new Rectangle(0, 0, 1920, 1200), false));
        

        TITLE_ELEMENTS.Add(background);
        TITLE_ELEMENTS.Add(button);

    }

}