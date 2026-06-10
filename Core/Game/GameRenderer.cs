using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GAME.CORE;

public static class GameRenderer
{
    
    private static List<GameElement> RenderList = new List<GameElement>();

    private static GraphicsDevice GraphicsDevice;
    private static SpriteBatch SpriteBatch;
    private static ContentManager ContentManager;
    private static SpriteFont GAME_FONT;

    public static int GetScreenWidth() { return GameRenderer.GraphicsDevice.Viewport.Width; }

    public static int GetScreenHeight() { return GameRenderer.GraphicsDevice.Viewport.Height; }

    public static bool InitializeRenderer(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        GameRenderer.SpriteBatch = spriteBatch;
        GameRenderer.GraphicsDevice = graphicsDevice;
        GameRenderer.ContentManager = contentManager;

        if(GameRenderer.SpriteBatch == null) return false;
        if(GameRenderer.GraphicsDevice == null) return false;

        return true;
    }

    public static void SetFont(SpriteFont FONT) { GameRenderer.GAME_FONT = FONT; }

    private static void Write(string TEXT, Point POSITION, Color COLOR)
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

    private static void Draw(GameElement ELEMENT)
    {
        //MOCK TEXTURE
        Texture2D pixel = new Texture2D(GameRenderer.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { ELEMENT.GetRendererConfig().COLOR });
        //MOCK TEXTURE

        //Render Text
        if(ELEMENT.GetRendererConfig().TEXT!=null) { GameRenderer.Write(ELEMENT.GetRendererConfig().TEXT, ELEMENT.GetPosition(), ELEMENT.GetRendererConfig().COLOR); return; }

        //Render Texture
        if(ELEMENT.GetRendererConfig().TEXTURE_PATH!=null) { GameRenderer.DrawTexture(ELEMENT); return;}

        GameRenderer.SpriteBatch.Draw(pixel, ELEMENT.GetRectangle(), ELEMENT.GetRendererConfig().COLOR);
    }

    private static void DrawTexture(GameElement ELEMENT)
    {
        Texture2D texture = GameRenderer.ContentManager.Load<Texture2D>(ELEMENT.GetRendererConfig().TEXTURE_PATH);
        SpriteBatch.Draw(texture, ELEMENT.GetRectangle(), ELEMENT.GetRendererConfig().COLOR);
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