using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GAME.CORE;

public static class GameRenderer
{

    private static GraphicsDevice GraphicsDevice;
    private static SpriteBatch SpriteBatch;
    private static ContentManager ContentManager;
    private static SpriteFont GAME_FONT;
    private static Texture2D PIXEL;

    //ENTITY
    public static List<GameElement> ENTITIES = new List<GameElement>();

    //CACHE
    private static Dictionary<string, Texture2D> _textureCache = new();

    #region HELPERS
    public static bool InitializeRenderer(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            GameRenderer.SpriteBatch = spriteBatch;
            GameRenderer.GraphicsDevice = graphicsDevice;
            GameRenderer.ContentManager = contentManager;

            if(GameRenderer.SpriteBatch == null) return false;
            if(GameRenderer.GraphicsDevice == null) return false;

            GameRenderer.PIXEL = new Texture2D(GameRenderer.GraphicsDevice, 1, 1);
            GameRenderer.PIXEL.SetData(new[] { Color.White });

            return true;
        }

    public static int GetScreenWidth() { return GameRenderer.GraphicsDevice.Viewport.Width; }

    public static int GetScreenHeight() { return GameRenderer.GraphicsDevice.Viewport.Height; }

    public static void SetFont(SpriteFont FONT) { GameRenderer.GAME_FONT = FONT; }

    public static SpriteFont GetGameFont() { return GameRenderer.GAME_FONT; }
    private static Texture2D LoadTexture(string path)
    {
        if (!_textureCache.TryGetValue(path, out Texture2D texture))
        {
            texture = GameRenderer.ContentManager.Load<Texture2D>(path);
            _textureCache[path] = texture;
        }
        return texture;
    }

    #endregion

    #region DRAW
    private static void Write(string TEXT, Point POSITION, Color COLOR)
    {
       GameRenderer.SpriteBatch.DrawString(GameRenderer.GAME_FONT, TEXT, new Vector2(POSITION.X, POSITION.Y), COLOR);
    }

    private static void Draw(GameElement ELEMENT)
    {

        if(ELEMENT is GameEntity) { GameRenderer.DrawEntity(ELEMENT as GameEntity); return; }

        if (ELEMENT.GetRendererConfig().TEXT != null)
        {
            GameRenderer.Write(ELEMENT.GetRendererConfig().TEXT, ELEMENT.GetPosition(), ELEMENT.GetRendererConfig().COLOR);
            return;
        }

        if (ELEMENT.GetRendererConfig().TEXTURE_PATH != null)
        {
            if (ELEMENT.GetRendererConfig().IS_SLICE)
                GameRenderer.DrawNineSlice(ELEMENT);
            else
                GameRenderer.DrawTexture(ELEMENT);
            return;
        }

        GameRenderer.SpriteBatch.Draw(PIXEL, ELEMENT.GetRectangle(), ELEMENT.GetRendererConfig().COLOR);
    }

    private static void DrawTexture(GameElement ELEMENT)
    {
        Texture2D texture = LoadTexture(ELEMENT.GetRendererConfig().TEXTURE_PATH);
        SpriteBatch.Draw(
            texture,
            ELEMENT.GetRectangle(),
            ELEMENT.GetRendererConfig().RECTANGLE,
            ELEMENT.GetRendererConfig().COLOR
        );
    }

    private static void DrawNineSlice(GameElement ELEMENT)
    {
        var cfg     = ELEMENT.GetRendererConfig();
        var dest    = ELEMENT.GetRectangle();
        int s       = cfg.SLICE_SIZE;
        int p       = s * cfg.SLICE_PROPORTION;
        Texture2D texture = LoadTexture(cfg.TEXTURE_PATH);

        Rectangle[] src = new Rectangle[9]
        {
            new(0,     0, s, s),
            new(s,     0, s, s),
            new(s * 2, 0, s, s),
            new(0,     s, s, s),
            new(s,     s, s, s),
            new(s * 2, s, s, s),
            new(0,     s * 2, s, s),
            new(s,     s * 2, s, s),
            new(s * 2, s * 2, s, s),
        };

        int iW = dest.Width  - p * 2;
        int iH = dest.Height - p * 2;
        int r  = dest.X + dest.Width  - p;
        int b  = dest.Y + dest.Height - p;

        Rectangle[] dst = new Rectangle[9]
        {
            new(dest.X,     dest.Y,     p,  p),
            new(dest.X + p, dest.Y,     iW, p),
            new(r,          dest.Y,     p,  p),
            new(dest.X,     dest.Y + p, p,  iH),
            new(dest.X + p, dest.Y + p, iW, iH),
            new(r,          dest.Y + p, p,  iH),
            new(dest.X,     b,          p,  p),
            new(dest.X + p, b,          iW, p),
            new(r,          b,          p,  p),
        };

        for (int i = 0; i < 9; i++)
            SpriteBatch.Draw(texture, dst[i], src[i], cfg.COLOR);
    }

    private static void DrawEntity(GameEntity ENTITY)
    {
        var cfg    = ENTITY.GetEntityConfig();
        int s      = cfg.SLICE_SIZE;
        int frame  = ENTITY.IS_MOVING ? ENTITY.GetFrame() : 0;
        bool flip  = false;

        int row;
        switch (ENTITY.DIRECTION)
        {
            case GameDirection.BOTTOM:       row = 0; break;
            case GameDirection.TOP:          row = 1; break;
            case GameDirection.LEFT:         row = 2; break;
            case GameDirection.RIGHT:        row = 2; flip = true; break;
            case GameDirection.TOP_LEFT:     row = 3; break;
            case GameDirection.TOP_RIGHT:    row = 3; flip = true; break;
            case GameDirection.BOTTOM_LEFT:  row = 4; break;
            case GameDirection.BOTTOM_RIGHT: row = 4; flip = true; break;
            default:                         row = 0; break;
        }

        int[] frameOrder = { 1, 2, 1, 2 };
        int col = frameOrder[frame % 4];

        Rectangle src  = new Rectangle(col * s, row * s, s, s);
        Rectangle dest = new Rectangle(
            ENTITY.GetRectangle().X,
            ENTITY.GetRectangle().Y,
            s * cfg.SIZE,
            s * cfg.SIZE
        );

        Texture2D texture = LoadTexture(cfg.TEXTURE_PATH);

        SpriteEffects effect = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        SpriteBatch.Draw(texture, dest, src, Color.White, 0f, Vector2.Zero, effect, 0f);
    }

    public static void Render(GameElement ELEMENT)
    {
        if(ELEMENT==null) return;
        GameRenderer.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if(ELEMENT.VISIBLE) GameRenderer.Draw(ELEMENT);

        GameRenderer.SpriteBatch.End();
    }

    public static void Render(List<GameElement> CUSTOM_LIST)
    {

        GameRenderer.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        foreach(GameElement element in CUSTOM_LIST) if(element.VISIBLE) GameRenderer.Draw(element);

        GameRenderer.SpriteBatch.End();
    }
    #endregion

    #region LOGIC
    public static void Update(List<GameElement> CUSTOM_LIST) { foreach(GameElement element in CUSTOM_LIST) element.Update(); }
    #endregion

}