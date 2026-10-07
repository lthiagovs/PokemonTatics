using PokemonTFT.Logic;

namespace PokemonTFT.Core;

public sealed class GameEntityRenderConfig
{
    public const int DEFAULT_SLICE = 64;
    public const int DEFAULT_SCALE = 2;

    public readonly int SLICE_SIZE;
    public readonly int SCALE;
    public readonly string TEXTURE_PATH;

    public readonly int FOOT_Y;
    public readonly int FOOT_W;

    public float SIZE_SCALE = 1f;

    public int DrawSize => (int)(SLICE_SIZE * SCALE * SIZE_SCALE);

    public GameEntityRenderConfig(string TEXTURE_PATH, int SLICE_SIZE = DEFAULT_SLICE, int SCALE = DEFAULT_SCALE,
                                 int FOOT_Y = 0, int FOOT_W = 0)
    {
        this.TEXTURE_PATH = TEXTURE_PATH;
        this.SLICE_SIZE   = SLICE_SIZE;
        this.SCALE        = SCALE;
        this.FOOT_Y       = FOOT_Y > 0 ? FOOT_Y : SLICE_SIZE;
        this.FOOT_W       = FOOT_W > 0 ? FOOT_W : SLICE_SIZE / 2;
    }
}

public class GameEntity : GameElement
{
    private static readonly GameEntityRenderConfig MISSING = new("Environment/shadow");

    private GameEntityRenderConfig _config = MISSING;
    private GameAnimation? _oneShot;

    private readonly GameAnimation _idleAnimation = GameAnimation.Idle();
    private readonly GameAnimation _walkAnimation = GameAnimation.Walk();

    public GameDirection DIRECTION = GameDirection.TOP_RIGHT;
    public bool IS_MOVING;

    public GameEntity(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE) { }

    public virtual GameEntityRenderConfig GetEntityConfig() => _config;

    public void SetEntityConfig(GameEntityRenderConfig CONFIG) => _config = CONFIG;

    public void PlayOnce(GameAnimation ANIMATION) => _oneShot = ANIMATION;

    public void ClearOneShot() => _oneShot = null;

    public void SetWalkPace(float PIXELS_PER_SECOND)
    {
        if (PIXELS_PER_SECOND <= 1f) return;
        _walkAnimation.SetFrameSpeed(Balance.WALK_CYCLE_PIXELS / (_walkAnimation.FrameCount * PIXELS_PER_SECOND));
    }

    private GameAnimation LoopAnimation => IS_MOVING ? _walkAnimation : _idleAnimation;

    public int GetFrameColumn() => (_oneShot ?? LoopAnimation).CurrentFrame;

    public override void Update()
    {
        UpdateAnimation();
        UpdateEffect();
    }

    private void UpdateAnimation()
    {
        if (_oneShot != null)
        {
            _oneShot.Update(GameTimeLogic.DELTA);
            if (!_oneShot.DONE) return;
            _oneShot = null;
        }

        LoopAnimation.Update(GameTimeLogic.DELTA);
    }
}
