using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace PokemonTFT.Core;

public readonly record struct EffectClip(string PATH, int FRAMES, int CELL);

public static class EffectLibrary
{
    public const string INDEX_FILE = "Data/effects.xml";

    private static readonly Dictionary<string, EffectClip> CLIPS = [];

    public static void Load(string? PATH = null)
    {
        CLIPS.Clear();

        string path = PATH ?? Path.Combine(AppContext.BaseDirectory, INDEX_FILE);
        if (!File.Exists(path)) return;

        XElement? root = XDocument.Load(path).Root;
        if (root == null) return;

        foreach (XElement node in root.Elements("Effect"))
        {
            string name = node.Attribute("name")?.Value ?? string.Empty;
            if (name.Length == 0) continue;

            CLIPS[name] = new EffectClip(
                "Effects/" + name,
                Number(node, "frames", 1),
                Number(node, "cell", 32));
        }
    }

    public static EffectClip? Find(string NAME)
        => CLIPS.TryGetValue(NAME, out EffectClip clip) ? clip : null;

    private static int Number(XElement NODE, string ATTRIBUTE, int FALLBACK)
    {
        string? raw = NODE.Attribute(ATTRIBUTE)?.Value;
        return raw != null && int.TryParse(raw, out int value) ? value : FALLBACK;
    }
}
