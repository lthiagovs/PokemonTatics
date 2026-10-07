using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace PokemonTFT.Core;

public static class GameMusic
{
    public const float VOLUME_BG      = 0.2f;
    public const float VOLUME_EFFECTS = 0.4f;
    public const float VOLUME_HIT     = 0.1f;

    public static Song? TITLE;
    public static Song? MAIN;

    public static SoundEffect? VICTORY;
    public static SoundEffect? LEVEL_UP;
    public static SoundEffect? HIT;

    private static Song? _playing;

    public static void PlayTitle() => PlaySong(TITLE);

    public static void PlayMain() => PlaySong(MAIN);

    private static void PlaySong(Song? SONG)
    {
        if (SONG == null || ReferenceEquals(_playing, SONG)) return;

        _playing            = SONG;
        MediaPlayer.Volume  = VOLUME_BG;
        MediaPlayer.Play(SONG);
        MediaPlayer.IsRepeating = true;
    }

    public static void Stop()
    {
        if (_playing == null) return;
        _playing = null;
        MediaPlayer.Stop();
    }

    public static void PlayHit()      => HIT?.Play(VOLUME_HIT, 0f, 0f);
    public static void PlayVictory()  => VICTORY?.Play(VOLUME_EFFECTS, 0f, 0f);
    public static void PlayLevelUp()  => LEVEL_UP?.Play(VOLUME_EFFECTS, 0f, 0f);
}
