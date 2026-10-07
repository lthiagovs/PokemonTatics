using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.UI;

namespace PokemonTFT.Screens;

public static class GameOverScene
{
    private const int CELL = 18;
    private const double COLLAPSE = 3.0;
    private const float SPIN = 7.5f;
    private const float PULL_CURVE = 2.6f;

    private const int BUTTON_W = 360;
    private const int BUTTON_H = 86;
    private const int EDGE = 4;

    private struct Shard
    {
        public Rectangle SOURCE;
        public float RADIUS;
        public float ANGLE;
    }

    private static Shard[] _shards = [];
    private static Texture2D? _frame;
    private static double _elapsed;
    private static bool _active;
    private static int _round = 1;

    public static bool IsActive => _active;

    public static bool HasCapture => _active && _frame != null;

    public static void Begin()
    {
        if (_active) return;

        ItemLogic.ReturnCarried();
        _active = true;
        _elapsed = 0;
        _frame = null;
        _shards = [];
        _round = GameGlobals.LEVEL;
    }

    public static void Clear()
    {
        _active = false;
        _elapsed = 0;
        _frame = null;
        _shards = [];
    }

    private static Rectangle ButtonRect()
    {
        int x = GameRenderer.GetScreenWidth() / 2 - BUTTON_W / 2;
        int y = GameRenderer.GetScreenHeight() / 2 + 40;
        return new Rectangle(x, y, BUTTON_W, BUTTON_H);
    }

    public static void Update()
    {
        if (!_active) return;

        _elapsed += GameTimeLogic.DELTA;
        if (_elapsed < COLLAPSE) return;

        if (!ButtonRect().Intersects(GameMouse.GetRectangle())) return;

        GameMouse.RequestHoverCursor();
        if (!GameMouse.LeftPressed()) return;

        GameMouse.ConsumeClick();
        Clear();
        GameTableLogic.RestartRun();
    }

    public static void Capture(Texture2D? FRAME)
    {
        if (!_active || _frame != null || FRAME == null) return;

        _frame = FRAME;
        BuildShards(FRAME.Width, FRAME.Height);
    }

    private static void BuildShards(int WIDTH, int HEIGHT)
    {
        int columns = Math.Max(1, WIDTH / CELL);
        int rows = Math.Max(1, HEIGHT / CELL);

        _shards = new Shard[columns * rows];

        float centreX = WIDTH / 2f;
        float centreY = HEIGHT / 2f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float dx = column * CELL + CELL / 2f - centreX;
                float dy = row * CELL + CELL / 2f - centreY;

                _shards[row * columns + column] = new Shard
                {
                    SOURCE = new Rectangle(column * CELL, row * CELL, CELL, CELL),
                    RADIUS = (float)Math.Sqrt(dx * dx + dy * dy),
                    ANGLE = (float)Math.Atan2(dy, dx)
                };
            }
        }
    }

    public static void Render()
    {
        if (!_active) return;

        GameRenderer.FillScreen(Color.Black);

        if (_elapsed < COLLAPSE)
        {
            if (_frame != null) RenderShards();
            return;
        }

        RenderPanel();
    }

    private static void RenderShards()
    {
        float progress = (float)Math.Clamp(_elapsed / COLLAPSE, 0, 1);
        float eased = (float)Math.Pow(progress, PULL_CURVE);

        float centreX = GameRenderer.GetScreenWidth() / 2f;
        float centreY = GameRenderer.GetScreenHeight() / 2f;
        float alpha = 1f - eased * 0.85f;

        for (int i = 0; i < _shards.Length; i++)
        {
            Shard shard = _shards[i];

            float radius = shard.RADIUS * (1f - eased);
            float angle = shard.ANGLE + SPIN * eased;

            int size = Math.Max(1, (int)(CELL * (1f - eased * 0.6f)));
            int x = (int)(centreX + Math.Cos(angle) * radius) - size / 2;
            int y = (int)(centreY + Math.Sin(angle) * radius) - size / 2;

            GameRenderer.DrawFragment(_frame!, shard.SOURCE, new Rectangle(x, y, size, size),
                Color.White * alpha);
        }
    }

    private static void RenderPanel()
    {
        int centreX = GameRenderer.GetScreenWidth() / 2;
        int centreY = GameRenderer.GetScreenHeight() / 2;

        Color accent = new(255, 198, 88);
        Color text = new(255, 255, 255);
        Color dim = new(176, 184, 200);
        Color surface = new(16, 18, 26);

        GameRenderer.DrawCenteredText("GAME OVER", centreX, centreY - 190, text, GameFonts.TITLE, SHADOW: 5);
        GameRenderer.DrawCenteredText($"YOU REACHED ROUND {_round}", centreX, centreY - 90, dim,
            GameFonts.BODY, SHADOW: 3);

        Rectangle button = ButtonRect();
        bool hovered = button.Intersects(GameMouse.GetRectangle());

        GameRenderer.DrawRect(button, accent);
        GameRenderer.DrawRect(
            new Rectangle(button.X + EDGE, button.Y + EDGE, button.Width - EDGE * 2, button.Height - EDGE * 2),
            hovered ? accent : surface);

        Vector2 size = GameFonts.Measure("TRY AGAIN", GameFonts.MEDIUM);
        GameRenderer.DrawCenteredText("TRY AGAIN", button.Center.X,
            button.Center.Y - size.Y * GameRenderer.OpticalCenter,
            hovered ? surface : accent, GameFonts.MEDIUM, SHADOW: 3);
    }
}
