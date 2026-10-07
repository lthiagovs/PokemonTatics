using System;
using System.Collections.Generic;
using System.IO;
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
    private const int HIGHLIGHT_THICKNESS = 3;

    private static Texture2D _pixel = null!;
    private static Texture2D _ring = null!;

    private const int RING_SIZE = 96;
    private const float RING_THICKNESS = 0.13f;

    private const int BACKDROP_DOWNSCALE = 8;

    private static RenderTarget2D? _scene;
    private static RenderTarget2D? _backdropSmall;
    private static RenderTarget2D? _backdrop;
    private static RenderTarget2D? _frame;

    private static readonly Vector2[] OUTLINE_STEPS =
    [
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
        new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)
    ];

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
        _ring = BuildRing(GRAPHICS_DEVICE);
        return true;
    }

    private static void EnsureTargets()
    {
        int width = GetScreenWidth();
        int height = GetScreenHeight();
        if (width <= 0 || height <= 0) return;
        if (_scene != null && _scene.Width == width && _scene.Height == height) return;

        _scene?.Dispose();
        _frame?.Dispose();
        _backdropSmall?.Dispose();
        _backdrop?.Dispose();

        int smallW = Math.Max(1, width / BACKDROP_DOWNSCALE);
        int smallH = Math.Max(1, height / BACKDROP_DOWNSCALE);

        _scene = new RenderTarget2D(_graphicsDevice, width, height);
        _frame = new RenderTarget2D(_graphicsDevice, width, height);
        _backdropSmall = new RenderTarget2D(_graphicsDevice, smallW, smallH);
        _backdrop = new RenderTarget2D(_graphicsDevice, width, height);
    }

    public static void BeginFrame(Color CLEAR)
    {
        EnsureTargets();
        _graphicsDevice.SetRenderTarget(_scene);
        _graphicsDevice.Clear(CLEAR);
        Begin();
    }

    public static void ResolveBackdrop()
    {
        End();

        if (_scene == null || _backdropSmall == null || _backdrop == null)
        {
            Begin();
            return;
        }

        _graphicsDevice.SetRenderTarget(_backdropSmall);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_scene, new Rectangle(0, 0, _backdropSmall.Width, _backdropSmall.Height), Color.White);
        _spriteBatch.End();

        _graphicsDevice.SetRenderTarget(_backdrop);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_backdropSmall, new Rectangle(0, 0, _backdrop.Width, _backdrop.Height), Color.White);
        _spriteBatch.End();

        _graphicsDevice.SetRenderTarget(_frame);
        _graphicsDevice.Clear(Color.Black);
        Begin();
        _spriteBatch.Draw(_scene, new Rectangle(0, 0, GetScreenWidth(), GetScreenHeight()), Color.White);
    }

    private static void DrawGlass(Rectangle DEST, Color TINT)
    {
        if (_backdrop != null)
        {
            Rectangle clip = Rectangle.Intersect(DEST, new Rectangle(0, 0, _backdrop.Width, _backdrop.Height));
            if (clip.Width > 0 && clip.Height > 0) _spriteBatch.Draw(_backdrop, clip, clip, Color.White);
        }

        _spriteBatch.Draw(_pixel, DEST, TINT);
    }

    private static Texture2D BuildRing(GraphicsDevice DEVICE)
    {
        var pixels = new Color[RING_SIZE * RING_SIZE];
        float radius = RING_SIZE / 2f - 1f;
        float inner = radius * (1f - RING_THICKNESS * 2f);

        for (int y = 0; y < RING_SIZE; y++)
        {
            for (int x = 0; x < RING_SIZE; x++)
            {
                float dx = x - RING_SIZE / 2f + 0.5f;
                float dy = (y - RING_SIZE / 2f + 0.5f) * 2.1f;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                float edge = Math.Min(radius - distance, distance - inner);
                float alpha = Math.Clamp(edge / 2.5f, 0f, 1f);
                pixels[y * RING_SIZE + x] = Color.White * alpha;
            }
        }

        var texture = new Texture2D(DEVICE, RING_SIZE, RING_SIZE);
        texture.SetData(pixels);
        return texture;
    }

    public static int GetScreenWidth()  => _graphicsDevice.Viewport.Width;
    public static int GetScreenHeight() => _graphicsDevice.Viewport.Height;

    public static float OpticalCenter { get; private set; } = 0.5f;

    public static void SetFont(SpriteFont FONT)
    {
        _font = FONT;

        float line = FONT.LineSpacing;
        if (line <= 0) return;

        foreach (char sample in "AHMW0")
        {
            if (!FONT.GetGlyphs().TryGetValue(sample, out SpriteFont.Glyph glyph)) continue;

            OpticalCenter = (glyph.Cropping.Y + glyph.BoundsInTexture.Height / 2f) / line;
            return;
        }
    }
    public static SpriteFont GetGameFont() => _font;

    private const string RAW_ROOT = "Pokemons/";

    private static readonly Dictionary<string, Texture2D> RAW_TEXTURES = new(StringComparer.Ordinal);

    private static Texture2D LoadTexture(string PATH)
    {
        if (PATH.StartsWith(RAW_ROOT, StringComparison.Ordinal)) return LoadRaw(PATH);

        try
        {
            return _content.Load<Texture2D>(PATH);
        }
        catch (ContentLoadException)
        {
            ReportMissing(PATH);
            return _pixel;
        }
    }

    private static Texture2D LoadRaw(string PATH)
    {
        if (RAW_TEXTURES.TryGetValue(PATH, out Texture2D? cached)) return cached;

        Texture2D? texture = ReadPng(PATH);
        if (texture == null) ReportMissing(PATH);

        RAW_TEXTURES[PATH] = texture ?? _pixel;
        return RAW_TEXTURES[PATH];
    }

    private static Texture2D? ReadPng(string PATH)
    {
        string file = Path.Combine(AppContext.BaseDirectory, _content.RootDirectory, PATH + ".png");
        if (!File.Exists(file)) return null;

        try
        {
            using FileStream stream = File.OpenRead(file);
            Texture2D texture = Texture2D.FromStream(_graphicsDevice, stream);

            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                pixels[i] = pixel.R == 255 && pixel.G == 0 && pixel.B == 255
                    ? Color.Transparent
                    : Color.FromNonPremultiplied(pixel.R, pixel.G, pixel.B, pixel.A);
            }
            texture.SetData(pixels);

            return texture;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ReportMissing(string PATH)
    {
        if (_missingTextures.Add(PATH)) Console.Error.WriteLine($"[GameRenderer] missing texture: {PATH}");
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

    public static Texture2D? FrameTexture => _frame;

    public static void BeginDirect(Color CLEAR)
    {
        EnsureTargets();
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(CLEAR);
        Begin();
    }

    public static void PresentFrame()
    {
        End();

        if (_frame == null) return;

        _graphicsDevice.SetRenderTarget(null);
        Begin();
        _spriteBatch.Draw(_frame, new Rectangle(0, 0, GetScreenWidth(), GetScreenHeight()), Color.White);
    }

    public static void DrawFragment(Texture2D SOURCE, Rectangle FROM, Rectangle DEST, Color TINT)
        => _spriteBatch.Draw(SOURCE, DEST, FROM, TINT);
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

    private static readonly List<GameElement> DEPTH_BUFFER = [];

    public static void RenderSortedByDepth(IReadOnlyList<GameElement>? ELEMENTS)
    {
        if (ELEMENTS == null) return;

        DEPTH_BUFFER.Clear();
        for (int i = 0; i < ELEMENTS.Count; i++)
        {
            if (ELEMENTS[i] is { VISIBLE: true }) DEPTH_BUFFER.Add(ELEMENTS[i]);
        }

        DEPTH_BUFFER.Sort(static (left, right) =>
            left.GetRectangle().Bottom.CompareTo(right.GetRectangle().Bottom));

        for (int i = 0; i < DEPTH_BUFFER.Count; i++)
        {
            _occlusion = OverlapBehind(i);
            Render(DEPTH_BUFFER[i]);
        }

        _occlusion = null;
    }

    private static Rectangle? OverlapBehind(int INDEX)
    {
        Rectangle front = Torso(DEPTH_BUFFER[INDEX]);

        for (int i = INDEX - 1; i >= 0; i--)
        {
            Rectangle behind = Torso(DEPTH_BUFFER[i]);
            if (!behind.Intersects(front)) continue;

            Rectangle overlap = Rectangle.Intersect(behind, front);
            if (overlap.Width > 2 && overlap.Height > 2) return overlap;
        }

        return null;
    }

    private static Rectangle Torso(GameElement ELEMENT)
    {
        Rectangle bounds = ELEMENT.GetRectangle();
        int inset = bounds.Width / 4;

        return new Rectangle(bounds.X + inset, bounds.Y + bounds.Height / 3,
            Math.Max(1, bounds.Width - inset * 2), Math.Max(1, bounds.Height * 2 / 3));
    }

    private static void DrawOccluded(Texture2D TEXTURE, Rectangle SOURCE, Rectangle DEST,
                                     Color TINT, SpriteEffects FLIP)
    {
        Rectangle cover = Rectangle.Intersect(DEST, _occlusion!.Value);
        if (cover.Width <= 0 || cover.Height <= 0)
        {
            _spriteBatch.Draw(TEXTURE, DEST, SOURCE, TINT, 0f, Vector2.Zero, FLIP, 0f);
            return;
        }

        DrawSlice(TEXTURE, SOURCE, DEST, new Rectangle(DEST.X, DEST.Y, DEST.Width, cover.Y - DEST.Y), TINT, FLIP);
        DrawSlice(TEXTURE, SOURCE, DEST,
            new Rectangle(DEST.X, cover.Bottom, DEST.Width, DEST.Bottom - cover.Bottom), TINT, FLIP);
        DrawSlice(TEXTURE, SOURCE, DEST,
            new Rectangle(DEST.X, cover.Y, cover.X - DEST.X, cover.Height), TINT, FLIP);
        DrawSlice(TEXTURE, SOURCE, DEST,
            new Rectangle(cover.Right, cover.Y, DEST.Right - cover.Right, cover.Height), TINT, FLIP);

        DrawSlice(TEXTURE, SOURCE, DEST, cover, TINT * OCCLUDER_ALPHA, FLIP);
    }

    private static void DrawSlice(Texture2D TEXTURE, Rectangle SOURCE, Rectangle DEST,
                                  Rectangle PIECE, Color TINT, SpriteEffects FLIP)
    {
        if (PIECE.Width <= 0 || PIECE.Height <= 0 || DEST.Width <= 0 || DEST.Height <= 0) return;

        float scaleX = SOURCE.Width / (float)DEST.Width;
        float scaleY = SOURCE.Height / (float)DEST.Height;

        int offsetX = PIECE.X - DEST.X;
        int width = Math.Max(1, (int)Math.Round(PIECE.Width * scaleX));

        int sourceX = FLIP == SpriteEffects.FlipHorizontally
            ? SOURCE.X + SOURCE.Width - (int)Math.Round((offsetX + PIECE.Width) * scaleX)
            : SOURCE.X + (int)Math.Round(offsetX * scaleX);

        var from = new Rectangle(sourceX,
            SOURCE.Y + (int)Math.Round((PIECE.Y - DEST.Y) * scaleY),
            width,
            Math.Max(1, (int)Math.Round(PIECE.Height * scaleY)));

        _spriteBatch.Draw(TEXTURE, PIECE, FitSource(TEXTURE, from), TINT, 0f, Vector2.Zero, FLIP, 0f);
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
            Color outline = (SHADOW_COLOR ?? Color.Black) * SHADOW_ALPHA;
            for (int i = 0; i < OUTLINE_STEPS.Length; i++)
            {
                _spriteBatch.DrawString(_font, TEXT, POSITION + OUTLINE_STEPS[i] * SHADOW,
                    outline, 0f, Vector2.Zero, SCALE, SpriteEffects.None, 0f);
            }
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
            Vector2 origin = new(position.X + offset.X, position.Y + offset.Y);

            if (config.TEXT_CENTER)
            {
                Vector2 measured = _font.MeasureString(config.TEXT) * config.FONT_SCALE;
                origin = new Vector2(
                    position.X + offset.X + (ELEMENT.SIZE_X - measured.X) / 2f,
                    position.Y + offset.Y + ELEMENT.SIZE_Y / 2f - measured.Y * OpticalCenter);
            }

            DrawString(config.TEXT, origin,
                config.COLOR * alpha, config.FONT_SCALE, config.TEXT_SHADOW, alpha, config.TEXT_SHADOW_COLOR);
        }
        else if (config.TEXTURE_PATH != null)
        {
            DrawTexture(ELEMENT, config, offset, alpha);
        }
        else if (config.IS_GLASS)
        {
            DrawGlass(DestRect(ELEMENT, offset), config.COLOR * alpha);
        }
        else
        {
            _spriteBatch.Draw(_pixel, DestRect(ELEMENT, offset), config.COLOR * alpha);
        }

        if (config.IS_HOVERING) DrawOutline(DestRect(ELEMENT, offset), UI.UITheme.ACCENT * alpha, HIGHLIGHT_THICKNESS);
    }

    public static void DrawOutline(Rectangle DEST, Color COLOR, int THICKNESS)
    {
        int edge = Math.Max(1, THICKNESS);
        _spriteBatch.Draw(_pixel, new Rectangle(DEST.X, DEST.Y, DEST.Width, edge), COLOR);
        _spriteBatch.Draw(_pixel, new Rectangle(DEST.X, DEST.Bottom - edge, DEST.Width, edge), COLOR);
        _spriteBatch.Draw(_pixel, new Rectangle(DEST.X, DEST.Y, edge, DEST.Height), COLOR);
        _spriteBatch.Draw(_pixel, new Rectangle(DEST.Right - edge, DEST.Y, edge, DEST.Height), COLOR);
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

    private static void DrawTypeRing(GameEntity ENTITY, Rectangle DEST, GameEntityRenderConfig CONFIG, float ALPHA)
    {
        if (ENTITY is not PokemonEntity { POKEMON: not null, DEAD: false } pokemon) return;
        if (!Logic.TypeHighlight.Matches(pokemon.POKEMON.TYPE)) return;

        float unit = DEST.Height / (float)Math.Max(1, CONFIG.SLICE_SIZE);
        int width = Math.Max(40, (int)(CONFIG.FOOT_W * unit * 2.1f));
        int height = Math.Max(18, width / 2);
        int x = DEST.X + (DEST.Width - width) / 2;
        int y = DEST.Y + (int)(CONFIG.FOOT_Y * unit) - height / 2;

        float pulse = 0.55f + 0.45f * (float)Math.Sin(Logic.TypeHighlight.PULSE * 6.0);
        Color tint = Models.PokemonTypeColors.Of(pokemon.POKEMON.TYPE) * (ALPHA * pulse);

        _spriteBatch.Draw(_ring, new Rectangle(x, y, width, height), tint);
    }

    private static void DrawShadow(Rectangle DEST, GameEntityRenderConfig CONFIG, float ALPHA)
    {
        float unit = DEST.Height / (float)Math.Max(1, CONFIG.SLICE_SIZE);
        int shadowW = Math.Max(8, (int)(CONFIG.FOOT_W * unit));
        int shadowH = Math.Max(4, shadowW * 8 / 23);
        int shadowX = DEST.X + (DEST.Width - shadowW) / 2;
        int shadowY = DEST.Y + (int)(CONFIG.FOOT_Y * unit) - shadowH;

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

        Color tint = ENTITY.EFFECT?.TINT ?? Color.White;
        if (ENTITY is PokemonEntity { POKEMON: not null, DEAD: false } tinted && tinted.STATUS.Any)
            tint = tinted.STATUS.Tint(Logic.GameTimeLogic.TOTAL, tint);

        tint *= ALPHA;
        if (ENTITY is PokemonEntity { HOVERED: true }) tint = Color.Lerp(tint, Color.White, 0.35f);

        Texture2D texture = LoadTexture(config.TEXTURE_PATH);

        DrawShadow(dest, config, ALPHA);
        DrawTypeRing(ENTITY, dest, config, ALPHA);

        SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Rectangle? fitted = FitSource(texture, source);

        if (_occlusion.HasValue && fitted.HasValue)
            DrawOccluded(texture, fitted.Value, dest, tint, effects);
        else
            _spriteBatch.Draw(texture, dest, fitted, tint, 0f, Vector2.Zero, effects, 0f);

        if (ENTITY is PokemonEntity { POKEMON: not null, DEAD: false } pokemon) DrawPokemonBars(pokemon, dest, ALPHA);
    }

    private const int ITEM_PIP = 12;
    private const int ITEM_PIP_GAP = 2;
    private const float OCCLUDER_ALPHA = 0.38f;

    private static Rectangle? _occlusion;

    private static void DrawItemPips(Pokemon POKEMON, int LEFT, int TOP, float ALPHA)
    {
        Texture2D texture = LoadTexture(Data.ItemDatabase.TEXTURE);
        int drawn = 0;

        for (int i = 0; i < POKEMON.ITEMS.Count; i++)
        {
            if (Data.ItemDatabase.Find(POKEMON.ITEMS[i]) is not { } item) continue;

            var dest = new Rectangle(LEFT + drawn * (ITEM_PIP + 2), TOP, ITEM_PIP, ITEM_PIP);

            _spriteBatch.Draw(texture, dest, FitSource(texture, item.ICON), Color.White * ALPHA);
            drawn++;
        }
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
        int levelX = barX + barW + 4;
        int levelY = (int)(middleY - size.Y / 2);
        DrawString(level, new Vector2(levelX, levelY), Color.White * ALPHA, GameFonts.SMALL, SHADOW: 1, ALPHA);

        if (pokemon.ITEMS.Count > 0)
            DrawItemPips(pokemon, levelX, levelY + (int)size.Y + ITEM_PIP_GAP, ALPHA);
    }
    #endregion
}
