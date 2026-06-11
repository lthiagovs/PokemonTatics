using System;
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
        if(ELEMENT.GetRendererConfig() == null) return;
        
        Vector2 offset = ELEMENT.EFFECT?.OFFSET ?? Vector2.Zero;
        float   alpha  = ELEMENT.EFFECT?.ALPHA  ?? 1f;

        if(ELEMENT is GameEntity) { GameRenderer.DrawEntity(ELEMENT as GameEntity, offset, alpha); return; }
        
        if(ELEMENT.GetRendererConfig().TEXT != null)
        {
            GameRenderer.Write(ELEMENT.GetRendererConfig().TEXT,
                new Point(ELEMENT.GetPosition().X + (int)offset.X, ELEMENT.GetPosition().Y + (int)offset.Y),
                ELEMENT.GetRendererConfig().COLOR * alpha);
            return;
        }
        
        if(ELEMENT.GetRendererConfig().TEXTURE_PATH != null)
        {
            if(ELEMENT.GetRendererConfig().IS_SLICE) GameRenderer.DrawNineSlice(ELEMENT, offset);
            else GameRenderer.DrawTexture(ELEMENT, offset);
            return;
        }
        
        SpriteBatch.Draw(PIXEL,
            new Rectangle(ELEMENT.GetPosition().X + (int)offset.X, ELEMENT.GetPosition().Y + (int)offset.Y, ELEMENT.SIZE_X, ELEMENT.SIZE_Y),
            ELEMENT.GetRendererConfig().COLOR * alpha);
    }

    private static void DrawTexture(GameElement ELEMENT, Vector2 offset = default)
    {
        Texture2D texture = LoadTexture(ELEMENT.GetRendererConfig().TEXTURE_PATH);
        Rectangle dest = new Rectangle(
            ELEMENT.GetRectangle().X + (int)offset.X,
            ELEMENT.GetRectangle().Y + (int)offset.Y,
            ELEMENT.GetRectangle().Width,
            ELEMENT.GetRectangle().Height);
        SpriteBatch.Draw(texture, dest, ELEMENT.GetRendererConfig().RECTANGLE, ELEMENT.GetRendererConfig().COLOR);
    }

    private static void DrawNineSlice(GameElement ELEMENT, Vector2 offset = default)
    {
        var cfg  = ELEMENT.GetRendererConfig();
        int s    = cfg.SLICE_SIZE;
        int p    = s * cfg.SLICE_PROPORTION;
        var rect = ELEMENT.GetRectangle();
        var dest = new Rectangle(rect.X + (int)offset.X, rect.Y + (int)offset.Y, rect.Width, rect.Height);
        Texture2D texture = LoadTexture(cfg.TEXTURE_PATH);

        Rectangle[] src = new Rectangle[9]
        {
            new(0,     0, s, s), new(s,     0, s, s), new(s * 2, 0, s, s),
            new(0,     s, s, s), new(s,     s, s, s), new(s * 2, s, s, s),
            new(0, s * 2, s, s), new(s, s * 2, s, s), new(s * 2, s * 2, s, s),
        };

        int iW = dest.Width  - p * 2;
        int iH = dest.Height - p * 2;
        int r  = dest.X + dest.Width  - p;
        int b  = dest.Y + dest.Height - p;

        Rectangle[] dst = new Rectangle[9]
        {
            new(dest.X,     dest.Y,     p,  p),  new(dest.X + p, dest.Y,     iW, p),  new(r, dest.Y,     p,  p),
            new(dest.X,     dest.Y + p, p,  iH), new(dest.X + p, dest.Y + p, iW, iH), new(r, dest.Y + p, p,  iH),
            new(dest.X,     b,          p,  p),  new(dest.X + p, b,          iW, p),  new(r, b,          p,  p),
        };

        for (int i = 0; i < 9; i++)
            SpriteBatch.Draw(texture, dst[i], src[i], cfg.COLOR);
    }

    private static void DrawShadow(Rectangle dest, GameEntityRenderConfig cfg)
    {
        int shadowW = 23 * cfg.SIZE;
        int shadowH = 8  * cfg.SIZE;
        int shadowX = dest.X + (dest.Width - shadowW) / 2;
        int shadowY = dest.Y + (int)(dest.Height * 0.80f) - shadowH / 2 - 20;
        Texture2D shadow = LoadTexture("Environment/shadow");
        SpriteBatch.Draw(shadow, new Rectangle(shadowX, shadowY, shadowW, shadowH), new Rectangle(0, 0, 23, 8), Color.Black * 0.4f);
    }

    private static void DrawEntity(GameEntity ENTITY, Vector2 offset = default, float alpha = 1f)
    {
        var cfg   = ENTITY.GetEntityConfig();
        int s     = cfg.SLICE_SIZE;
        int frame = ENTITY.IS_MOVING ? ENTITY.GetFrame() : 0;
        bool flip = false;
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

        int col;
        if(!ENTITY.IS_MOVING) col = 1;
        else { int[] frameOrder = { 1, 0, 1, 2 }; col = frameOrder[frame % 4]; }

        Rectangle src  = new Rectangle(col * s, row * s, s, s);
        Rectangle dest = new Rectangle(
            ENTITY.GetRectangle().X + (int)offset.X,
            ENTITY.GetRectangle().Y + (int)offset.Y,
            s * cfg.SIZE, s * cfg.SIZE);

        Texture2D texture  = LoadTexture(cfg.TEXTURE_PATH);
        SpriteEffects sfx  = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Color tint         = (ENTITY.EFFECT?.TINT ?? Color.White) * alpha;

        DrawShadow(dest, cfg);
        SpriteBatch.Draw(texture, dest, src, tint, 0f, Vector2.Zero, sfx, 0f);
        SpriteBatch.Draw(texture, dest, src, tint, 0f, Vector2.Zero, sfx, 0f);

        if(ENTITY is PokemonEntity)
        {

            PokemonEntity pkm = ENTITY as PokemonEntity;
            int barW = (int)(dest.Width * 0.7f);
            int barH = 6;
            int barX = dest.X + (dest.Width - barW) / 2;
            int barY = dest.Y - barH - 6;

            int totalBarsH = barH + 3 + barH;
            int barsMiddleY = barY + totalBarsH / 2;

            float hpPercent = Math.Clamp(pkm.POKEMON.HP / (float)pkm.POKEMON.MAX_HP, 0f, 1f);
            SpriteBatch.Draw(PIXEL, new Rectangle(barX, barY, barW, barH), Color.Black);
            SpriteBatch.Draw(PIXEL, new Rectangle(barX, barY, (int)(barW * hpPercent), barH), new Color(180, 60, 60));

            int xpBarY      = barY + barH + 3;
            float xpPercent = Math.Clamp(pkm.POKEMON.XP / (float)pkm.POKEMON.XPToNextLevel(), 0f, 1f);
            SpriteBatch.Draw(PIXEL, new Rectangle(barX, xpBarY, barW, barH), Color.Black);
            SpriteBatch.Draw(PIXEL, new Rectangle(barX, xpBarY, (int)(barW * xpPercent), barH), new Color(60, 100, 220));

            int iconSize = 20;
            int iconX    = barX - iconSize - 4;
            int iconY    = barsMiddleY - iconSize / 2;
            Texture2D typeIcon = LoadTexture($"UI/Types/{pkm.POKEMON.TYPE.ToString().ToLower()}");
            SpriteBatch.Draw(typeIcon, new Rectangle(iconX, iconY, iconSize, iconSize), new Rectangle(0, 0, 34, 34), Color.White);

            string lvlText  = $"L{pkm.POKEMON.LEVEL}";
            Vector2 lvlSize = GAME_FONT.MeasureString(lvlText);
            int lvlX = barX + barW + 4;
            int lvlY = barsMiddleY - (int)(lvlSize.Y / 2);
            SpriteBatch.DrawString(GAME_FONT, lvlText, new Vector2(lvlX, lvlY), Color.White);
            
        }


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