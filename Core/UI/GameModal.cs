using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PokemonTFT.Core;

namespace PokemonTFT.UI;

public static class GameModal
{
    public const int TITLE_H = 64;
    public const int PAD = 22;

    private const int CLOSE_SIZE = 52;

    private static readonly List<GameElement> ELEMENTS = [];

    private static object? _owner;
    private static int _signature = int.MinValue;
    private static bool _claimed;
    private static bool _persistent;
    private static bool _escapeHeld;
    private static bool _escapeCloses = true;
    private static Rectangle _bounds;

    public static bool IsOpen => ELEMENTS.Count > 0;

    public static bool Blocking => _persistent;

    public static void BeginFrame()
    {
        if (!_persistent) _claimed = false;
    }

    public static void ShowTransient(object OWNER, int SIGNATURE, string TITLE, string ICON, int WIDTH, int HEIGHT,
                                     Action<GameInterfaceElement, int, int> CONTENT)
    {
        if (_persistent) return;
        if (IsOpen && CursorInside && !ReferenceEquals(_owner, OWNER)) return;

        _claimed = true;
        if (ReferenceEquals(_owner, OWNER) && _signature == SIGNATURE) return;

        _owner = OWNER;
        _signature = SIGNATURE;
        Build(TITLE, ICON, WIDTH, HEIGHT, CONTENT, CLOSABLE: false);
    }

    public static void Open(string TITLE, string ICON, int WIDTH, int HEIGHT,
                            Action<GameInterfaceElement, int, int> CONTENT, bool ESCAPE_CLOSES = true)
    {
        _escapeCloses = ESCAPE_CLOSES;
        Logic.ItemLogic.ReturnCarried();

        _persistent = true;
        _claimed = true;
        _owner = null;
        _signature = int.MinValue;
        Build(TITLE, ICON, WIDTH, HEIGHT, CONTENT, CLOSABLE: true);
    }

    public static void Close()
    {
        ELEMENTS.Clear();
        _persistent = false;
        _claimed = false;
        _owner = null;
        _signature = int.MinValue;
    }

    public static void Update()
    {
        if (!_persistent)
        {
            if (!IsOpen) return;

            GameElement[] hovered = ELEMENTS.ToArray();
            for (int i = 0; i < hovered.Length; i++) UpdateTree(hovered[i]);
            return;
        }

        GameElement[] snapshot = ELEMENTS.ToArray();
        for (int i = 0; i < snapshot.Length; i++) UpdateTree(snapshot[i]);

        bool escape = Keyboard.GetState().IsKeyDown(Keys.Escape);
        if (escape && !_escapeHeld && _escapeCloses) Close();
        _escapeHeld = escape;

        if (GameMouse.LeftPressed()) GameMouse.ConsumeClick();
    }

    public static bool CursorInside => IsOpen && _bounds.Contains(GameMouse.GetPos());

    public static void EndFrame()
    {
        if (_claimed || _persistent || CursorInside) return;

        ELEMENTS.Clear();
        _owner = null;
        _signature = int.MinValue;
    }

    public static void Render() => GameRenderer.Render(ELEMENTS);

    private static void UpdateTree(GameElement ELEMENT)
    {
        ELEMENT.Update();
        IReadOnlyList<GameElement> children = ELEMENT.GetChildren();
        for (int i = 0; i < children.Count; i++) UpdateTree(children[i]);
    }

    private static void Build(string TITLE, string ICON, int WIDTH, int HEIGHT,
                              Action<GameInterfaceElement, int, int> CONTENT, bool CLOSABLE)
    {
        ELEMENTS.Clear();

        int screenW = GameRenderer.GetScreenWidth();
        int screenH = GameRenderer.GetScreenHeight();

        var dim = new GameInterfaceElement(0, 0, screenW, screenH, VISIBLE: true);
        dim.SetRendererConfig(GameRendererConfig.Glass(UITheme.GLASS_DEEP));
        ELEMENTS.Add(dim);

        int width = Math.Min(WIDTH, screenW - PAD * 4);
        int height = Math.Min(HEIGHT, screenH - PAD * 4);

        _bounds = new Rectangle((screenW - width) / 2, (screenH - height) / 2, width, height);

        GameInterfaceElement panel = UIFactory.Panel(_bounds.X, _bounds.Y, width, height);
        ELEMENTS.Add(panel);

        int inner = UIFactory.BORDER;
        UIFactory.Solid(inner, inner, width - inner * 2, TITLE_H, UITheme.SURFACE_DEEP, panel);
        UIFactory.Solid(inner, inner + TITLE_H, width - inner * 2, 2, UITheme.ACCENT, panel);

        Vector2 titleSize = GameFonts.Measure(TITLE, GameFonts.MEDIUM);
        int titleGlyph = UIIcons.Width(ICON, UIIcons.LARGE);
        UIFactory.Icon(ICON, PAD + titleGlyph / 2, inner + TITLE_H / 2, UIIcons.LARGE, panel, UITheme.ACCENT);
        UIFactory.Label(TITLE, PAD + titleGlyph + PAD / 2, inner + (TITLE_H - (int)titleSize.Y) / 2,
            panel, UITheme.ACCENT, GameFonts.MEDIUM);

        CONTENT(panel, width, height);

        if (!CLOSABLE) return;

        var close = new GameButton(width - CLOSE_SIZE - PAD, inner + (TITLE_H - CLOSE_SIZE) / 2,
            CLOSE_SIZE, CLOSE_SIZE, VISIBLE: true, PARENT: panel)
        {
            HOVERABLE = true,
            IDLE_BOB = 0f,
            ON_CLICK = Close
        };
        close.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));
        UIFactory.Solid(UIFactory.BORDER, UIFactory.BORDER, CLOSE_SIZE - UIFactory.BORDER * 2,
            CLOSE_SIZE - UIFactory.BORDER * 2, UITheme.SURFACE_DEEP, close);
        UIFactory.Icon("close", CLOSE_SIZE / 2, CLOSE_SIZE / 2, UIIcons.INLINE, close, UITheme.TEXT);
        ELEMENTS.Add(close);
    }
}
