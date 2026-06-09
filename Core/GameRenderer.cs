using System;
using System.Collections.Generic;
using GAME.CORE;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class GameRenderer
{
    
    private static List<GameElement> RenderList = new List<GameElement>();

    private static GraphicsDevice GraphicsDevice;
    private static SpriteBatch SpriteBatch;

    public static bool InitializeRenderer(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        GameRenderer.SpriteBatch = spriteBatch;
        GameRenderer.GraphicsDevice = graphicsDevice;

        if(GameRenderer.SpriteBatch == null) return false;
        if(GameRenderer.GraphicsDevice == null) return false;

        return true;
    }

    public static void AddElement(GameElement ELEMENT)
    {
        GameRenderer.RenderList.Add(ELEMENT);
    }

    public static void Render()
    {

        Console.WriteLine(GameRenderer.SpriteBatch == null);
        Console.WriteLine(GameRenderer.GraphicsDevice == null);

        GameRenderer.SpriteBatch.Begin();
        //MOCK TEXTURE
        Texture2D pixel = new Texture2D(GameRenderer.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        //MOCK TEXTURE

        foreach(GameElement element in GameRenderer.RenderList)
        {
            Console.WriteLine("RENDER");

            if(element.VISIBLE) GameRenderer.SpriteBatch.Draw(pixel, element.GetRectangle(), Color.White);
        }

        Console.WriteLine("END");
        GameRenderer.SpriteBatch.End();
    }


}