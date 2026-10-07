using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;

namespace PokemonTFT.UI;

public static class UIFactory
{
    public const int BORDER = 2;
    public const int EDGE = 1;
    public const int PAD = 8;

    public static GameInterfaceElement Solid(int X, int Y, int WIDTH, int HEIGHT, Color COLOR,
                                             GameInterfaceElement? PARENT = null)
    {
        var element = new GameInterfaceElement(X, Y, WIDTH, HEIGHT, VISIBLE: true, PARENT);
        element.SetRendererConfig(GameRendererConfig.Solid(COLOR));
        PARENT?.AddChild(element);
        return element;
    }

    public static GameInterfaceElement Panel(int X, int Y, int WIDTH, int HEIGHT,
                                             GameInterfaceElement? PARENT = null, Color? FILL = null)
    {
        var frame = new GameInterfaceElement(X, Y, WIDTH, HEIGHT, VISIBLE: true, PARENT);
        frame.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));
        PARENT?.AddChild(frame);

        if (FILL.HasValue)
        {
            Solid(BORDER, BORDER, WIDTH - BORDER * 2, HEIGHT - BORDER * 2, FILL.Value, frame);
        }
        else
        {
            var glass = new GameInterfaceElement(BORDER, BORDER, WIDTH - BORDER * 2, HEIGHT - BORDER * 2,
                VISIBLE: true, frame);
            glass.SetRendererConfig(GameRendererConfig.Glass(UITheme.GLASS));
            frame.AddChild(glass);
        }

        Solid(BORDER, BORDER, WIDTH - BORDER * 2, EDGE, UITheme.EDGE, frame);
        return frame;
    }

    public static GameInterfaceElement Label(string TEXT, int X, int Y, GameInterfaceElement? PARENT = null,
                                             Color? COLOR = null, float SCALE = GameFonts.BODY)
    {
        Vector2 size = GameFonts.Measure(TEXT, SCALE);
        var element = new GameInterfaceElement(X, Y, (int)size.X, (int)size.Y, VISIBLE: true, PARENT);
        element.SetRendererConfig(GameRendererConfig.Label(TEXT, COLOR ?? UITheme.TEXT, SCALE, TEXT_SHADOW: 2));
        PARENT?.AddChild(element);
        return element;
    }

    public static GameInterfaceElement LabelBox(string TEXT, Rectangle BOX, GameInterfaceElement? PARENT = null,
                                                Color? COLOR = null, float SCALE = GameFonts.BODY)
    {
        var element = new GameInterfaceElement(BOX.X, BOX.Y, BOX.Width, BOX.Height, VISIBLE: true, PARENT);

        GameRendererConfig config = GameRendererConfig.Label(TEXT, COLOR ?? UITheme.TEXT, SCALE, TEXT_SHADOW: 2);
        config.TEXT_CENTER = true;
        element.SetRendererConfig(config);

        PARENT?.AddChild(element);
        return element;
    }

    public static GameInterfaceElement Bar(int X, int Y, int WIDTH, int HEIGHT, Color COLOR,
                                           GameInterfaceElement? PARENT, out GameInterfaceElement FILL)
    {
        var track = new GameInterfaceElement(X, Y, WIDTH, HEIGHT, VISIBLE: true, PARENT);
        track.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));
        PARENT?.AddChild(track);

        Solid(BORDER, BORDER, WIDTH - BORDER * 2, HEIGHT - BORDER * 2, UITheme.TRACK, track);
        FILL = Solid(BORDER, BORDER, WIDTH - BORDER * 2, HEIGHT - BORDER * 2, COLOR, track);
        Solid(BORDER, BORDER, WIDTH - BORDER * 2, EDGE, UITheme.EDGE, track);
        return track;
    }

    public static GameInterfaceElement IconLabel(string ICON, string TEXT, int X, int Y,
                                                 GameInterfaceElement? PARENT = null, Color? COLOR = null)
    {
        Vector2 size = GameFonts.Measure(TEXT, GameFonts.BODY);
        int glyph = UIIcons.Width(ICON);
        int line = (int)size.Y;

        Icon(ICON, X + glyph / 2, Y + line / 2, UIIcons.INLINE, PARENT, COLOR ?? UITheme.TEXT_DIM);

        return LabelBox(TEXT, new Rectangle(X + glyph + 12, Y, (int)size.X, line), PARENT,
            COLOR ?? UITheme.TEXT_DIM);
    }

    public static GameInterfaceElement? LastLabel { get; private set; }

    public static GameInterfaceElement? LastIcon { get; private set; }

    public static GameButton Button(string TEXT, Action ON_CLICK, Color? FILL = null, Color? TEXT_COLOR = null,
                                    int MIN_WIDTH = 0, GameInterfaceElement? PARENT = null, bool GLASS = true,
                                    string? ICON = null)
    {
        Vector2 size = GameFonts.Measure(TEXT, GameFonts.BODY);
        int glyph = ICON == null ? 0 : UIIcons.Width(ICON);
        int gap = glyph == 0 ? 0 : PAD;
        int content = glyph + gap + (int)size.X;

        int width = Math.Max(MIN_WIDTH, content + PAD * 3);
        int height = (int)size.Y + PAD * 2;

        var button = new GameButton(0, 0, width, height, VISIBLE: true, PARENT)
        {
            HOVERABLE = true,
            IDLE_BOB = 0f,
            HOVER_SCALE = 1.05f,
            ON_CLICK = ON_CLICK
        };
        button.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));

        if (GLASS)
        {
            var glass = new GameInterfaceElement(BORDER, BORDER, width - BORDER * 2, height - BORDER * 2,
                VISIBLE: true, button);
            glass.SetRendererConfig(GameRendererConfig.Glass(FILL ?? UITheme.GLASS));
            button.AddChild(glass);
        }
        else
        {
            Solid(BORDER, BORDER, width - BORDER * 2, height - BORDER * 2, FILL ?? UITheme.SURFACE, button);
        }

        Solid(BORDER, BORDER, width - BORDER * 2, EDGE, UITheme.EDGE, button);

        int start = (width - content) / 2;
        if (glyph > 0) Icon(ICON!, start + glyph / 2, height / 2, UIIcons.INLINE, button, TEXT_COLOR ?? UITheme.TEXT);

        LastLabel = LabelBox(TEXT,
            new Rectangle(start + glyph + gap, 0, (int)size.X, height), button, TEXT_COLOR ?? UITheme.TEXT);
        return button;
    }

    public static GameInterfaceElement? Icon(string NAME, int CENTER_X, int CENTER_Y, int VARIANT,
                                             GameInterfaceElement? PARENT = null, Color? COLOR = null)
    {
        if (UIIcons.Find(NAME, VARIANT) is not { } source) return null;

        var element = new GameInterfaceElement(CENTER_X - source.Width / 2, CENTER_Y - source.Height / 2,
            source.Width, source.Height, VISIBLE: true, PARENT);
        element.SetRendererConfig(GameRendererConfig.Sprite(UIIcons.TEXTURE, source, COLOR ?? UITheme.TEXT));
        PARENT?.AddChild(element);
        LastIcon = element;
        return element;
    }

    public static GameButton IconButton(string ICON, Action ON_CLICK, Color? FILL = null, Color? TINT = null,
                                        GameInterfaceElement? PARENT = null)
    {
        int glyph = UIIcons.LARGE;
        int size = glyph + PAD * 2;

        var button = new GameButton(0, 0, size, size, VISIBLE: true, PARENT)
        {
            HOVERABLE = true,
            IDLE_BOB = 0f,
            HOVER_SCALE = 1.08f,
            ON_CLICK = ON_CLICK
        };
        button.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));

        var glass = new GameInterfaceElement(BORDER, BORDER, size - BORDER * 2, size - BORDER * 2,
            VISIBLE: true, button);
        glass.SetRendererConfig(GameRendererConfig.Glass(FILL ?? UITheme.GLASS));
        button.AddChild(glass);

        Solid(BORDER, BORDER, size - BORDER * 2, EDGE, UITheme.EDGE, button);
        Icon(ICON, size / 2, size / 2, UIIcons.LARGE, button, TINT);
        return button;
    }

    public static int Section(string ICON, string TITLE, int X, int Y, int WIDTH,
                              GameInterfaceElement? PARENT = null)
    {
        int line = (int)GameFonts.Measure(TITLE, GameFonts.BODY).Y;

        IconLabel(ICON, TITLE, X, Y, PARENT, UITheme.ACCENT);
        Solid(X, Y + line + 6, WIDTH, 2, UITheme.BORDER, PARENT);

        return Y + line + 16;
    }

    public static List<string> Wrap(string TEXT, int MAX_WIDTH, float SCALE)
    {
        var lines = new List<string>();
        string current = string.Empty;

        foreach (string word in TEXT.Split(' '))
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && GameFonts.Measure(candidate, SCALE).X > MAX_WIDTH)
            {
                lines.Add(current);
                current = word;
                continue;
            }

            current = candidate;
        }

        if (current.Length > 0) lines.Add(current);
        return lines;
    }

    public static int BarFillWidth(int WIDTH, int VALUE, int MAX)
    {
        if (MAX <= 0) return 0;
        int span = WIDTH - BORDER * 2;
        return System.Math.Clamp(span * VALUE / MAX, 0, span);
    }
}
