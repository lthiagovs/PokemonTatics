using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Logic;

namespace PokemonTFT.Core;

public class GameElement
{
    private static readonly IReadOnlyList<GameElement> NO_CHILDREN = [];

    private Point _position;
    private List<GameElement>? _children;

    public int SIZE_X;
    public int SIZE_Y;
    public bool VISIBLE;
    public RenderEffect? EFFECT;

    public float RENDER_SCALE = 1f;

    public Vector2 RENDER_OFFSET = Vector2.Zero;

    public float RENDER_ALPHA = 1f;

    protected GameRendererConfig RENDER_CONFIG;

    public GameElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE)
    {
        _position     = new Point(POS_X, POS_Y);
        this.SIZE_X   = SIZE_X;
        this.SIZE_Y   = SIZE_Y;
        this.VISIBLE  = VISIBLE;
        RENDER_CONFIG = new GameRendererConfig();
    }

    #region POSITION
    public virtual Point GetPosition() => _position;

    public virtual Point SetPosition(Point NEW_POSITION) => _position = NEW_POSITION;

    public virtual Rectangle GetRectangle() => new(GetPosition(), new Point(SIZE_X, SIZE_Y));
    #endregion

    #region CHILDREN
    public IReadOnlyList<GameElement> GetChildren() => _children ?? NO_CHILDREN;

    public void AddChild(GameElement CHILD) => (_children ??= []).Add(CHILD);
    #endregion

    #region RENDER & LOGIC
    public GameRendererConfig GetRendererConfig() => RENDER_CONFIG;

    public virtual void SetRendererConfig(GameRendererConfig CONFIG) => RENDER_CONFIG = CONFIG;

    public void SetEffect(RenderEffect? EFFECT) => this.EFFECT = EFFECT;

    public void UpdateEffect()
    {
        if (EFFECT == null) return;
        EFFECT.Update(GameTimeLogic.DELTA);
        if (EFFECT.DONE) EFFECT = null;
    }

    public virtual void Update() { }

    public static void UpdateAll(IReadOnlyList<GameElement>? ELEMENTS)
    {
        if (ELEMENTS == null) return;
        for (int i = 0; i < ELEMENTS.Count; i++) ELEMENTS[i].Update();
    }
    #endregion
}
