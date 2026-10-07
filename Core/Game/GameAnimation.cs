namespace PokemonTFT.Core;

public sealed class GameAnimation
{
    private static readonly int[] IDLE_FRAMES   = [1];
    private static readonly int[] WALK_FRAMES   = [1, 0, 1, 2];
    private static readonly int[] ATTACK_FRAMES = [2, 1, 0, 1];

    private readonly int[] _frames;
    private readonly double _frameSpeed;
    private readonly bool _loop;

    private int _index;
    private double _elapsed;

    public bool DONE { get; private set; }

    public GameAnimation(int[] FRAMES, double FRAME_SPEED, bool LOOP)
    {
        _frames     = FRAMES.Length > 0 ? FRAMES : IDLE_FRAMES;
        _frameSpeed = FRAME_SPEED > 0 ? FRAME_SPEED : 0.1;
        _loop       = LOOP;
    }

    public static GameAnimation Idle()   => new(IDLE_FRAMES,   1.0,  LOOP: true);
    public static GameAnimation Walk()   => new(WALK_FRAMES,   0.2,  LOOP: true);
    public static GameAnimation Attack() => new(ATTACK_FRAMES, 0.06, LOOP: false);

    public int CurrentFrame => _frames[_index];

    public void Update(double DELTA)
    {
        if (DONE) return;

        _elapsed += DELTA;
        if (_elapsed < _frameSpeed) return;

        _elapsed = 0;
        _index++;
        if (_index < _frames.Length) return;

        if (_loop) _index = 0;
        else { _index = _frames.Length - 1; DONE = true; }
    }
}
