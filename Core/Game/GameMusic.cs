using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

public static class GameMusic
{

    public static Song TITLE;
    public static Song MAIN;
    private static bool IS_PLAYING = false;
    public static float VOLUME_BG = 0.2f;
    public static float VOLUME_EFFECTS = 0.4f;

    //Effects
    public static SoundEffect VICTORY;
    public static SoundEffect LEVEL_UP;
    public static SoundEffect HIT;


    public static void PlayTitle()
    {
        if(IS_PLAYING) return;
        IS_PLAYING = true;
        MediaPlayer.Volume = VOLUME_BG;
        MediaPlayer.Play(TITLE);
        MediaPlayer.IsRepeating = true;
    }

    public static void PlayMain()
    {
        if(IS_PLAYING) return;
        IS_PLAYING = true;
        MediaPlayer.Volume = VOLUME_BG;
        MediaPlayer.Play(MAIN);
        MediaPlayer.IsRepeating = true;
    }

    public static void PlayHit()
    {
        SoundEffectInstance sfx = HIT.CreateInstance();
        sfx.Volume = 0.1f;
        sfx.IsLooped = false;
        sfx.Play();
    }

    public static void PlayVictory()
    {
        SoundEffectInstance sfx = VICTORY.CreateInstance();
        sfx.Volume = VOLUME_EFFECTS;
        sfx.IsLooped = false;
        sfx.Play();
    }

    public static void PlayLevelUp()
    {
        SoundEffectInstance sfx = LEVEL_UP.CreateInstance();
        sfx.Volume = VOLUME_EFFECTS;
        sfx.IsLooped = false;
        sfx.Play();
    }

    public static void Stop()
    {
        if(!IS_PLAYING) return;
        IS_PLAYING = false;
        MediaPlayer.Stop();
    }


}
