public class GameAnimation
{
    public string TEXTURE_PATH;
    public int[]  FRAMES;
    public float  FRAME_SPEED;
    public bool   LOOP;
    public bool   DONE = false;

    private int    _currentIndex = 0;
    private double _elapsed      = 0;

    public int GetFrame() { return FRAMES[_currentIndex]; }

    public void Update()
    {
        if(DONE) return;
        _elapsed += GameTimeLogic.DELTA;
        if(_elapsed >= FRAME_SPEED)
        {
            _elapsed = 0;
            _currentIndex++;
            if(_currentIndex >= FRAMES.Length)
            {
                if(LOOP) _currentIndex = 0;
                else { _currentIndex = FRAMES.Length - 1; DONE = true; }
            }
        }
    }
}