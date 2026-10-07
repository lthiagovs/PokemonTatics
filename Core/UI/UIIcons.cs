using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace PokemonTFT.UI;

public static class UIIcons
{
    public const string INDEX_FILE = "Data/icons.xml";

    private static readonly Dictionary<string, Rectangle> ICONS = [];

    public static string TEXTURE { get; private set; } = "UI/icons";

    public const int LARGE = 48;
    public const int INLINE = 36;
    public const int SMALL = 24;

    public static void Load(string? PATH = null)
    {
        ICONS.Clear();

        string path = PATH ?? Path.Combine(AppContext.BaseDirectory, INDEX_FILE);
        if (!File.Exists(path)) return;

        XElement? root = XDocument.Load(path).Root;
        if (root == null) return;

        TEXTURE = root.Attribute("texture")?.Value ?? TEXTURE;

        foreach (XElement node in root.Elements("Icon"))
        {
            string name = node.Attribute("name")?.Value ?? string.Empty;
            if (name.Length == 0) continue;

            int size = Number(node, "size", LARGE);
            ICONS[Key(name, size)] = new Rectangle(
                Number(node, "x", 0), Number(node, "y", 0),
                Number(node, "w", size), Number(node, "h", size));
        }
    }

    public static Rectangle? Find(string NAME, int SIZE = LARGE)
        => ICONS.TryGetValue(Key(NAME, SIZE), out Rectangle bounds) ? bounds : null;

    public static int Width(string NAME, int SIZE = INLINE) => Find(NAME, SIZE)?.Width ?? 0;

    private static string Key(string NAME, int SIZE) => NAME + ":" + SIZE;

    private static int Number(XElement NODE, string ATTRIBUTE, int FALLBACK)
    {
        string? raw = NODE.Attribute(ATTRIBUTE)?.Value;
        return raw != null && int.TryParse(raw, out int value) ? value : FALLBACK;
    }
}
