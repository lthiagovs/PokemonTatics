using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PokemonTFT.Models;

namespace PokemonTFT.Core;

public static class GameRenderer
{
    private static GraphicsDevice _graphicsDevice = null!;
    private static SpriteBatch _spriteBatch = null!;
    private static ContentManager _content = null!;
    private static SpriteFont _font = null!;
    private static Texture2D _pixel = null!;

    private static readonly HashSet<string> _missingTextures = [];
    private static bool _batchOpen;

    #region SETUP
    public static bool InitializeRenderer(SpriteBatch SPRITE_BATCH, GraphicsDevice GRAPHICS_DEVICE, ContentManager CONTENT)
    {
        if (SPRITE_BATCH == null || GRAPHICS_DEVICE == null) return false;

        _spriteBatch    = SPRITE_BATCH;
        _graphicsDevice = GRAPHICS_DEVICE;
        _content        = CONTENT;

        _pixel = new Texture2D(GRAPHICS_DEVICE, 1, 1);
        _pixel.SetData([Color.White]);
        return true;
    }

    public static int GetScreenWidth()  => _graphicsDevice.Viewport.Width;
    public static int GetScreenHeight() => _graphicsDevice.Viewport.Height;

    public static void SetFont(SpriteFont FONT) => _font = FONT;
    public static SpriteFont GetGameFont() => _font;

    private static Texture2D LoadTexture(string PATH)
    {
        try
        {
            return _content.Load<Texture2D>(PATH);
        }
        catch (ContentLoadException)
        {
            if (_missingTextures.Add(PATH))
                Console.Error.WriteLine($"[GameRenderer] textura ausente: {PATH}");
            return _pixel;
        }
    }

    public static Rectangle CoverSource(int TEXTURE_W, int TEXTURE_H, int DEST_W, int DEST_H)
    {
        if (TEXTURE_W <= 0 || TEXTURE_H <= 0 || DEST_W <= 0 || DEST_H <= 0)
            return new Rectangle(0, 0, TEXTURE_W, TEXTURE_H);

        float textureAspect = TEXTURE_W / (float)TEXTURE_H;
        float destAspect    = DEST_W / (float)DEST_H;

        if (destAspect > textureAspect)
        {
            int height = (int)(TEXTURE_W / destAspect);
            return new Rectangle(0, (TEXTURE_H - height) / 2, TEXTURE_W, height);
        }

        int width = (int)(TEXTURE_H * destAspect);
        return new Rectangle((TEXTURE_W - width) / 2, 0, width, TEXTURE_H);
    }
    #endregion

    #region FRAME
    public static void Begin()
    {
        if (_batchOpen) return;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _batchOpen = true;
    }

    public static void End()
    {
        if (!_batchOpen) return;
        _spriteBatch.End();
        _batchOpen = false;
    }
    #endregion

    #region RENDER
    public static void Render(GameElement? ELEMENT)
    {
        if (ELEMENT is not { VISIBLE: true }) return;

        Draw(ELEMENT);
        foreach (GameElement child in ELEMENT.GetChildren()) Render(child);
    }

    public static void Render(IReadOnlyList<GameElement>? ELEMENTS)
    {
        if (ELEMENTS == null) return;
        for (int i = 0; i < ELEMENTS.Count; i++) Render(ELEMENTS[i]);
    }

    public static void RenderEffects()
    {
        IReadOnlyList<GameEffect> effects = GameEffect.All;
        for (int i = 0; i < effects.Count; i++)
        {
            GameEffect effect = effects[i];
            Texture2D texture = LoadTexture(effect.PATH);

            int frameW = texture.Width;
            int frameH = texture.Height / effect.FRAMES;
            int frame  = Math.Clamp(effect.FrameIndex, 0, effect.FRAMES - 1);

            int drawW = (int)(frameW * effect.SCALE);
            int drawH = (int)(frameH * effect.SCALE);

            var source = new Rectangle(0, frame * frameH, frameW, frameH);
            var dest   = new Rectangle(effect.X - drawW / 2, effect.Y - drawH / 2, drawW, drawH);

            _spriteBatch.Draw(texture, dest, FitSource(texture, source), effect.TINT);
        }
    }

    public static void RenderFloatingText()
    {
        IReadOnlyList<FloatingText> items = FloatingText.All;
        for (int i = 0; i < items.Count; i++)
        {
            FloatingText item = items[i];

            Vector2 position = item.Position;
            if (item.CENTERED) position.X -= GameFonts.Measure(item.TEXT, item.SCALE).X / 2f;

            DrawString(item.TEXT, position, item.COLOR * item.Alpha, item.SCALE, SHADOW: 2, item.Alpha);
        }
    }
    #endregion

    #region PRIMITIVAS PUBLICAS
    public static void FillScreen(Color COLOR)
        => _spriteBatch.Draw(_pixel, new Rectangle(0, 0, GetScreenWidth(), GetScreenHeight()), COLOR);

    public static void DrawRect(Rectangle DEST, Color COLOR) => _spriteBatch.Draw(_pixel, DEST, COLOR);

    public static void DrawSprite(string PATH, Rectangle DEST, Rectangle? SOURCE, Color COLOR, bool FLIP = false)
    {
        Texture2D texture = LoadTexture(PATH);
        _spriteBatch.Draw(texture, DEST, FitSource(texture, SOURCE), COLOR, 0f, Vector2.Zero,
            FLIP ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    public static void DrawRay(Vector2 CENTER, float ANGLE, float LENGTH, float THICKNESS, Color COLOR)
        => _spriteBatch.Draw(_pixel, CENTER, null, COLOR, ANGLE,
            new Vector2(0f, 0.5f), new Vector2(LENGTH, THICKNESS), SpriteEffects.None, 0f);

    public static void DrawCenteredText(string TEXT, float CENTER_X, float Y, Color COLOR, float SCALE, int SHADOW = 3)
    {
        Vector2 size = GameFonts.Measure(TEXT, SCALE);
        DrawString(TEXT, new Vector2(CENTER_X - size.X / 2f, Y), COLOR, SCALE, SHADOW, COLOR.A / 255f);
    }
    #endregion

    #region DRAW
    private static void DrawString(string TEXT, Vector2 POSITION, Color COLOR, float SCALE, int SHADOW, float SHADOW_ALPHA = 1f, Color? SHADOW_COLOR = null)
    {
        if (SHADOW > 0)
        {
            _spriteBatch.DrawString(_font, TEXT, POSITION + new Vector2(SHADOW, SHADOW),
                (SHADOW_COLOR ?? Color.Black) * SHADOW_ALPHA, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
        }

        _spriteBatch.DrawString(_font, TEXT, POSITION, COLOR, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
    }

    private static void Draw(GameElement ELEMENT)
    {
        GameRendererConfig config = ELEMENT.GetRendererConfig();

        Vector2 offset = (ELEMENT.EFFECT?.OFFSET ?? Vector2.Zero) + ELEMENT.RENDER_OFFSET;
        float alpha    = (ELEMENT.EFFECT?.ALPHA ?? 1f) * ELEMENT.RENDER_ALPHA;

        if (ELEMENT is GameEntity entity)
        {
            DrawEntity(entity, offset, alpha);
            return;
        }

        if (config.TEXT != null)
        {
            Point position = ELEMENT.GetPosition();
            DrawString(config.TEXT,
                new Vector2(position.X + offset.X, position.Y + offset.Y),
                config.COLOR * alpha,
                config.FONT_SCALE,
                config.TEXT_SHADOW,
                alpha,
                config.TEXT_SHADOW_COLOR);
        }
        else if (config.TEXTURE_PATH != null)
        {
            if (config.IS_SLICE) DrawNineSlice(ELEMENT, config, offset, alpha);
            else DrawTexture(ELEMENT, config, offset, alpha);
        }
        else
        {
            _spriteBatch.Draw(_pixel, DestRect(ELEMENT, offset), config.COLOR * alpha);
        }

        if (config.IS_HOVERING)
            DrawNineSliceRaw("UI/Windows/tile_select", 24, 2, DestRect(ELEMENT, offset), Color.White * alpha);
    }

    private static Rectangle DestRect(GameElement ELEMENT, Vector2 OFFSET)
    {
        Rectangle rect = ELEMENT.GetRectangle();
        rect = new Rectangle(rect.X + (int)OFFSET.X, rect.Y + (int)OFFSET.Y, rect.Width, rect.Height);
        return Scaled(rect, DrawScale(ELEMENT));
    }

    private static float DrawScale(GameElement ELEMENT) => ELEMENT.RENDER_SCALE * (ELEMENT.EFFECT?.SCALE ?? 1f);

    private static Rectangle Scaled(Rectangle RECTANGLE, float SCALE)
    {
        if (Math.Abs(SCALE - 1f) < 0.001f) return RECTANGLE;

        int width  = (int)(RECTANGLE.Width  * SCALE);
        int height = (int)(RECTANGLE.Height * SCALE);
        return new Rectangle(
            RECTANGLE.X + (RECTANGLE.Width  - width)  / 2,
            RECTANGLE.Y + (RECTANGLE.Height - height) / 2,
            width, height);
    }

    private static Rectangle? FitSource(Texture2D TEXTURE, Rectangle? SOURCE)
    {
        if (SOURCE is not { } source) return null;
        if (source.Right <= TEXTURE.Width && source.Bottom <= TEXTURE.Height) return source;
        return null;
    }

    private static void DrawTexture(GameElement ELEMENT, GameRendererConfig CONFIG, Vector2 OFFSET, float ALPHA)
    {
        Texture2D texture = LoadTexture(CONFIG.TEXTURE_PATH!);
        _spriteBatch.Draw(texture, DestRect(ELEMENT, OFFSET), FitSource(texture, CONFIG.SOURCE), CONFIG.COLOR * ALPHA);
    }

    private static void DrawNineSlice(GameElement ELEMENT, GameRendererConfig CONFIG, Vector2 OFFSET, float ALPHA) =>
        DrawNineSliceRaw(CONFIG.TEXTURE_PATH!, CONFIG.SLICE_SIZE, CONFIG.SLICE_PROPORTION,
            DestRect(ELEMENT, OFFSET), CONFIG.COLOR * ALPHA);

    private static void DrawNineSliceRaw(string PATH, int SLICE_SIZE, int SLICE_PROPORTION, Rectangle DEST, Color COLOR)
    {
        if (SLICE_SIZE <= 0) return;

        Texture2D texture = LoadTexture(PATH);
        int s = SLICE_SIZE;

        if (texture.Width < s * 3 || texture.Height < s * 3)
        {
            _spriteBatch.Draw(texture, DEST, COLOR);
            return;
        }

        int p = s * Math.Max(1, SLICE_PROPORTION);

        int innerW = DEST.Width  - p * 2;
        int innerH = DEST.Height - p * 2;
        int right  = DEST.X + DEST.Width  - p;
        int bottom = DEST.Y + DEST.Height - p;

        for (int i = 0; i < 9; i++)
        {
            int col = i % 3;
            int row = i / 3;

            var source = new Rectangle(col * s, row * s, s, s);
            var dest = new Rectangle(
                col switch { 0 => DEST.X, 1 => DEST.X + p, _ => right },
                row switch { 0 => DEST.Y, 1 => DEST.Y + p, _ => bottom },
                col == 1 ? innerW : p,
                row == 1 ? innerH : p);

            _spriteBatch.Draw(texture, dest, source, COLOR);
        }
    }

    private static void DrawShadow(Rectangle DEST, GameEntityRenderConfig CONFIG, float ALPHA)
    {
        int shadowW = 23 * CONFIG.SCALE;
        int shadowH = 8  * CONFIG.SCALE;
        int shadowX = DEST.X + (DEST.Width - shadowW) / 2;
        int shadowY = DEST.Y + (int)(DEST.Height * 0.80f) - shadowH / 2 - 20;

        _spriteBatch.Draw(LoadTexture("Environment/shadow"),
            new Rectangle(shadowX, shadowY, shadowW, shadowH),
            new Rectangle(0, 0, 23, 8),
            Color.Black * (0.4f * ALPHA));
    }

    private static void DrawEntity(GameEntity ENTITY, Vector2 OFFSET, float ALPHA)
    {
        GameEntityRenderConfig config = ENTITY.GetEntityConfig();
        int slice = config.SLICE_SIZE;

        (int row, bool flip) = GameDirectionExtensions.SpriteCell(ENTITY.DIRECTION);

        var source = new Rectangle(ENTITY.GetFrameColumn() * slice, row * slice, slice, slice);

        Rectangle bounds = ENTITY.GetRectangle();
        var dest = Scaled(new Rectangle(
            bounds.X + (int)OFFSET.X,
            bounds.Y + (int)OFFSET.Y,
            config.DrawSize,
            config.DrawSize), DrawScale(ENTITY));

        Color tint = (ENTITY.EFFECT?.TINT ?? Color.White) * ALPHA;
        if (ENTITY is PokemonEntity { HOVERED: true }) tint = Color.Lerp(tint, Color.White, 0.35f);

        Texture2D texture = LoadTexture(config.TEXTURE_PATH);

        DrawShadow(dest, config, ALPHA);
        _spriteBatch.Draw(texture, dest, FitSource(texture, source), tint, 0f, Vector2.Zero,
            flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        if (ENTITY is PokemonEntity { POKEMON: not null, DEAD: false } pokemon) DrawPokemonBars(pokemon, dest, ALPHA);
    }

    private static void DrawPokemonBars(PokemonEntity ENTITY, Rectangle DEST, float ALPHA)
    {
        Pokemon pokemon = ENTITY.POKEMON!;

        const int BAR_H = 6;
        const int GAP   = 3;

        int barW = (int)(DEST.Width * 0.7f);
        int barX = DEST.X + (DEST.Width - barW) / 2;
        int barY = DEST.Y - BAR_H - 6;
        int middleY = barY + (BAR_H + GAP + BAR_H) / 2;

        Color backdrop = Color.Black * ALPHA;

        float hpPercent = pokemon.MAX_HP > 0 ? Math.Clamp(pokemon.HP / (float)pokemon.MAX_HP, 0f, 1f) : 0f;
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barW, BAR_H), backdrop);
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(barW * hpPercent), BAR_H), new Color(180, 60, 60) * ALPHA);

        int secondBarY = barY + BAR_H + GAP;
        _spriteBatch.Draw(_pixel, new Rectangle(barX, secondBarY, barW, BAR_H), backdrop);

        if (GameGlobals.GAME_STARTED)
        {
            float special = pokemon.SPECIAL_MAX > 0
                ? Math.Clamp(pokemon.SPECIAL_COUNTER / (float)pokemon.SPECIAL_MAX, 0f, 1f) : 0f;
            _spriteBatch.Draw(_pixel, new Rectangle(barX, secondBarY, (int)(barW * special), BAR_H), new Color(220, 190, 40) * ALPHA);
        }
        else
        {
            int next = pokemon.XPToNextLevel();
            float xp = next > 0 ? Math.Clamp(pokemon.XP / (float)next, 0f, 1f) : 0f;
            _spriteBatch.Draw(_pixel, new Rectangle(barX, secondBarY, (int)(barW * xp), BAR_H), new Color(60, 100, 220) * ALPHA);
        }

        const int ICON_SIZE = 20;
        Texture2D icon = LoadTexture(pokemon.IconPath);
        _spriteBatch.Draw(icon,
            new Rectangle(barX - ICON_SIZE - 4, middleY - ICON_SIZE / 2, ICON_SIZE, ICON_SIZE),
            FitSource(icon, new Rectangle(0, 0, 34, 34)), Color.White * ALPHA);

        string level = $"L{pokemon.LEVEL}";
        Vector2 size = _font.MeasureString(level);
        DrawString(level, new Vector2(barX + barW + 4, middleY - size.Y / 2), Color.White * ALPHA, GameFonts.SMALL, SHADOW: 1, ALPHA);
    }
    #endregion
}
