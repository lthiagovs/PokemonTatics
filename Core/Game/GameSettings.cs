using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using PokemonTFT.Logic;

namespace PokemonTFT.Core;

public static class GameSettings
{
    private const string FOLDER = "PokemonTactics";
    private const string FILE = "settings.json";

    private static readonly JsonSerializerOptions FORMAT = new() { WriteIndented = true };

    public static bool MUSIC = true;
    public static bool SOUND = true;
    public static bool FULLSCREEN;
    public static Difficulty DIFFICULTY = Difficulty.MEDIUM;

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FOLDER, FILE);

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            if (JsonNode.Parse(File.ReadAllText(FilePath)) is not JsonObject root) return;

            MUSIC = Read(root, "music", MUSIC);
            SOUND = Read(root, "sound", SOUND);
            FULLSCREEN = Read(root, "fullscreen", FULLSCREEN);

            if (root["difficulty"] is JsonValue value && value.TryGetValue(out string? name)
                && Enum.TryParse(name, ignoreCase: true, out Difficulty difficulty)
                && Enum.IsDefined(difficulty))
                DIFFICULTY = difficulty;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            MUSIC = true;
            SOUND = true;
            FULLSCREEN = false;
            DIFFICULTY = Difficulty.MEDIUM;
        }
    }

    public static void Save()
    {
        try
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            var root = new JsonObject
            {
                ["music"] = MUSIC,
                ["sound"] = SOUND,
                ["fullscreen"] = FULLSCREEN,
                ["difficulty"] = DIFFICULTY.ToString()
            };

            File.WriteAllText(FilePath, root.ToJsonString(FORMAT));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("[GameSettings] could not save: " + error.Message);
        }
    }

    public static void ApplyAudio()
    {
        MediaPlayer.IsMuted = !MUSIC;
        SoundEffect.MasterVolume = SOUND ? 1f : 0f;
    }

    private static bool Read(JsonObject ROOT, string KEY, bool FALLBACK)
        => ROOT[KEY] is JsonValue value && value.TryGetValue(out bool result) ? result : FALLBACK;
}
