using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using PokemonTFT.Models;

namespace PokemonTFT.Data;

public static class ItemDatabase
{
    public const string INDEX_FILE = "Data/items.xml";

    private static readonly List<Item> ALL = [];
    private static readonly Dictionary<string, Item> BY_ID = new(StringComparer.Ordinal);

    public static string TEXTURE { get; private set; } = "UI/items";

    public static IReadOnlyList<Item> All => ALL;

    public static void Load(string? PATH = null)
    {
        ALL.Clear();
        BY_ID.Clear();

        string path = PATH ?? Path.Combine(AppContext.BaseDirectory, INDEX_FILE);
        if (!File.Exists(path)) return;

        XElement? root = XDocument.Load(path).Root;
        if (root == null) return;

        TEXTURE = root.Attribute("texture")?.Value ?? TEXTURE;

        foreach (XElement node in root.Elements("Item"))
        {
            string id = node.Attribute("id")?.Value ?? string.Empty;
            if (id.Length == 0) continue;

            if (!Enum.TryParse(node.Attribute("effect")?.Value, ignoreCase: true, out ItemEffect effect))
                continue;

            var item = new Item
            {
                ID = id,
                NAME = node.Attribute("name")?.Value ?? id.ToUpperInvariant(),
                EFFECT = effect,
                VALUE = Number(node, "value", 0),
                ICON = new Rectangle(Number(node, "x", 0), Number(node, "y", 0),
                                     Number(node, "w", 0), Number(node, "h", 0))
            };

            ALL.Add(item);
            BY_ID[id] = item;
        }
    }

    public static Item? Find(string ID) => BY_ID.GetValueOrDefault(ID);

    public static Item? Random()
        => ALL.Count == 0 ? null : ALL[System.Random.Shared.Next(ALL.Count)];

    private static int Number(XElement NODE, string ATTRIBUTE, int FALLBACK)
    {
        string? raw = NODE.Attribute(ATTRIBUTE)?.Value;
        return raw != null && int.TryParse(raw, out int value) ? value : FALLBACK;
    }
}
