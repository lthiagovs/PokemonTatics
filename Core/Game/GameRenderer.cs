using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GAME.CORE;

public static class GameRenderer
{
    
    private static List<GameElement> RenderList = new List<GameElement>();

    private static GraphicsDevice GraphicsDevice;
    private static SpriteBatch SpriteBatch;
    private static SpriteFont GAME_FONT;

    public static int GetScreenWidth() { return GameRenderer.GraphicsDevice.Viewport.Width; }

    public static int GetScreenHeight() { return GameRenderer.GraphicsDevice.Viewport.Height; }

    public static bool InitializeRenderer(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        GameRenderer.SpriteBatch = spriteBatch;
        GameRenderer.GraphicsDevice = graphicsDevice;

        if(GameRenderer.SpriteBatch == null) return false;
        if(GameRenderer.GraphicsDevice == null) return false;

        return true;
    }

    public static void SetFont(SpriteFont FONT) { GameRenderer.GAME_FONT = FONT; }

    public static void Write(string TEXT, Point POSITION, Color COLOR)
    {
       GameRenderer.SpriteBatch.DrawString(GameRenderer.GAME_FONT, TEXT, new Vector2(POSITION.X, POSITION.Y), COLOR);
    }

    public static void AddElement(GameElement ELEMENT)
    {
        GameRenderer.RenderList.Add(ELEMENT);
    }

    public static GameElement GetElementByIndex(int INDEX)
    {
        return GameRenderer.RenderList[INDEX];
    }

    public static void Draw(GameElement ELEMENT)
    {
        //MOCK TEXTURE
        Texture2D pixel = new Texture2D(GameRenderer.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { ELEMENT.GetRendererConfig().COLOR });
        //MOCK TEXTURE

        if(!(ELEMENT.GetRendererConfig().TEXT==null))
        {
            GameRenderer.Write(ELEMENT.GetRendererConfig().TEXT, ELEMENT.GetPosition(), ELEMENT.GetRendererConfig().COLOR);
            return;
        }
        GameRenderer.SpriteBatch.Draw(pixel, ELEMENT.GetRectangle(), ELEMENT.GetRendererConfig().COLOR);
    }

    public static void Render(GameElement ELEMENT)
    {
        GameRenderer.SpriteBatch.Begin();

        if(ELEMENT.VISIBLE) GameRenderer.Draw(ELEMENT);

        GameRenderer.SpriteBatch.End();
    }

    public static void Render()
    {

        GameRenderer.SpriteBatch.Begin();

        foreach(GameElement element in GameRenderer.RenderList)
        {

            if(element.VISIBLE) GameRenderer.Draw(element);
        }

        GameRenderer.SpriteBatch.End();
    }

    public static void RenderFromList(List<GameElement> CUSTOM_LIST)
    {

        GameRenderer.SpriteBatch.Begin();

        foreach(GameElement element in CUSTOM_LIST)
        {

            if(element.VISIBLE) GameRenderer.Draw(element);
        }

        GameRenderer.SpriteBatch.End();
    }

    public static void Update() { foreach(GameElement element in GameRenderer.RenderList) element.Update(); }

    public static void UpdateFromList(List<GameElement> CUSTOM_LIST) { foreach(GameElement element in CUSTOM_LIST) element.Update(); }

}