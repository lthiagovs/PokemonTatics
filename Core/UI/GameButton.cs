using System;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;

namespace PokemonTFT.UI;

public class GameButton : GameInterfaceElement
{
    private const float SCALE_LERP = 14f;
    private const double PUNCH_TIME = 0.09;

    public Action? ON_CLICK;

    public bool ENABLED = true;

    public float HOVER_SCALE = 1.12f;
    public float PUNCH_SCALE = 0.93f;
    public float IDLE_BOB = 3f;
    public double BOB_SPEED = 2.2;

    private float _scale = 1f;
    private double _punchLeft;
    private double _bobTime;

    public GameButton(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    public override void Update()
    {
        base.Update();

        if (!VISIBLE || !ENABLED)
        {
            _scale = 1f;
            RENDER_SCALE = 1f;
            return;
        }

        bool hovered = IsHovered();

        float target = hovered ? HOVER_SCALE : 1f;
        if (_punchLeft > 0)
        {
            _punchLeft -= GameTimeLogic.DELTA;
            target = PUNCH_SCALE;
        }

        _scale = MathHelper.Lerp(_scale, target, (float)Math.Clamp(GameTimeLogic.DELTA * SCALE_LERP, 0, 1));
        RENDER_SCALE = _scale;

        _bobTime += GameTimeLogic.DELTA;
        RENDER_OFFSET = new Vector2(0, (float)Math.Sin(_bobTime * BOB_SPEED) * IDLE_BOB);

        if (!hovered) return;

        GameMouse.RequestHoverCursor();
        if (!GameMouse.LeftPressed()) return;

        GameMouse.ConsumeClick();
        _punchLeft = PUNCH_TIME;
        ON_CLICK?.Invoke();
    }
}
