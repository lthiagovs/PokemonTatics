using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.Data;

public static class PokemonDatabase
{
    public const string ROSTER_FILE = "Data/pokemons.xml";

    private static readonly List<Pokemon> ALL = [];
    private static readonly List<Pokemon> PLAYABLE = [];
    private static readonly List<Pokemon> LEGENDARY = [];
    private static readonly List<Pokemon> SHOP = [];

    public static bool Ready => PLAYABLE.Count > 0;

    public static string PROBLEM { get; private set; } = string.Empty;

    public static void Load(string? PATH = null)
    {
        ALL.Clear();
        PLAYABLE.Clear();
        LEGENDARY.Clear();
        SHOP.Clear();
        PROBLEM = string.Empty;

        string path = PATH ?? Path.Combine(AppContext.BaseDirectory, ROSTER_FILE);
        if (!File.Exists(path))
        {
            PROBLEM = $"{ROSTER_FILE} NOT FOUND";
            return;
        }

        XElement? root;
        try
        {
            root = XDocument.Load(path).Root;
        }
        catch (Exception error)
        {
            PROBLEM = $"{ROSTER_FILE} IS NOT VALID XML: {error.Message.ToUpperInvariant()}";
            return;
        }

        if (root == null)
        {
            PROBLEM = $"{ROSTER_FILE} IS EMPTY";
            return;
        }

        var templates = new Dictionary<string, Pokemon>(StringComparer.Ordinal);
        var links = new List<(Pokemon Form, string To)>();
        int skipped = 0;

        foreach (XElement node in root.Elements("Pokemon"))
        {
            (Pokemon? form, string? evolvesTo) = ReadForm(node);
            if (form == null)
            {
                skipped++;
                continue;
            }

            if (!templates.TryAdd(form.NAME, form))
            {
                skipped++;
                continue;
            }

            ALL.Add(form);
            if (evolvesTo != null) links.Add((form, evolvesTo));
        }

        foreach ((Pokemon form, string to) in links)
        {
            if (!templates.TryGetValue(to, out Pokemon? target)) continue;
            if (ReferenceEquals(form, target)) continue;

            form.EVOLUTION = target;
        }

        BreakCycles();
        AssignLines();
        AssignCosts();
        AssignEvolutionLevels();

        PLAYABLE.AddRange(ALL.Where(form => form.COST > 0));
        LEGENDARY.AddRange(PLAYABLE.Where(form => form.LEGENDARY));
        SHOP.AddRange(PLAYABLE.Where(form => !form.LEGENDARY));
        if (SHOP.Count == 0) SHOP.AddRange(PLAYABLE);

        if (PLAYABLE.Count > 0) return;

        PROBLEM = ALL.Count == 0
            ? $"{ROSTER_FILE} HAS NO POKEMON" + (skipped > 0 ? $" ({skipped} ENTRIES WERE INVALID)" : string.Empty)
            : "EVERY POKEMON IS AN EVOLUTION TARGET, SO NONE CAN BE BOUGHT";
    }

    private static void BreakCycles()
    {
        foreach (Pokemon form in ALL)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { form.NAME };

            Pokemon? step = form.EVOLUTION;
            Pokemon previous = form;

            while (step != null)
            {
                if (!seen.Add(step.NAME))
                {
                    previous.EVOLUTION = null;
                    break;
                }

                previous = step;
                step = step.EVOLUTION;
            }
        }
    }

    private static void AssignLines()
    {
        var evolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (Pokemon form in ALL)
            if (form.EVOLUTION != null) evolved.Add(form.EVOLUTION.NAME);

        foreach (Pokemon form in ALL)
        {
            if (evolved.Contains(form.NAME)) continue;

            Pokemon? step = form;
            var guard = new HashSet<string>(StringComparer.Ordinal);
            while (step != null && guard.Add(step.NAME))
            {
                step.LINE = form.NAME;
                step = step.EVOLUTION;
            }
        }

        foreach (Pokemon form in ALL)
            if (form.LINE.Length == 0) form.LINE = form.NAME;
    }

    private static void AssignCosts()
    {
        var evolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (Pokemon form in ALL)
            if (form.EVOLUTION != null) evolved.Add(form.EVOLUTION.NAME);

        int weakest = int.MaxValue;
        int strongest = 0;
        bool ordinary = ALL.Any(form => !evolved.Contains(form.NAME) && !form.LEGENDARY);

        foreach (Pokemon form in ALL)
        {
            if (evolved.Contains(form.NAME) || (ordinary && form.LEGENDARY)) continue;

            weakest = Math.Min(weakest, form.BaseStatTotal);
            strongest = Math.Max(strongest, form.BaseStatTotal);
        }

        if (weakest > strongest) weakest = strongest;

        foreach (Pokemon form in ALL)
        {
            form.COST = evolved.Contains(form.NAME)
                ? 0
                : RosterRules.Cost(form.BaseStatTotal, weakest, strongest);
        }
    }

    private static void AssignEvolutionLevels()
    {
        foreach (Pokemon form in ALL)
        {
            if (form.COST <= 0) continue;

            int previous = 0;
            Pokemon current = form;
            var guard = new HashSet<string>(StringComparer.Ordinal) { form.NAME };

            while (current.EVOLUTION is { } next)
            {
                current.EVOLUTION_LEVEL = RosterRules.EvolutionLevel(current, next, previous);
                previous = current.EVOLUTION_LEVEL;

                if (!guard.Add(next.NAME)) break;
                current = next;
            }
        }
    }

    private static (Pokemon? Form, string? EvolvesTo) ReadForm(XElement NODE)
    {
        string? name = NODE.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(name)) return (null, null);

        XElement? stats = NODE.Element("Stats");
        if (stats == null) return (null, null);

        XElement? sprite = NODE.Element("Sprite");
        XElement? evolution = NODE.Element("Evolution");

        if (!Enum.TryParse(NODE.Attribute("type")?.Value, ignoreCase: true, out PokemonType type))
            type = PokemonType.NORMAL;

        var form = new Pokemon(
            name,
            RosterRules.SafeStat(Number(stats, "hp", 1)),
            RosterRules.SafeStat(Number(stats, "atk", 1)),
            RosterRules.SafeStat(Number(stats, "spatk", 1)),
            RosterRules.SafeStat(Number(stats, "def", 1)),
            RosterRules.SafeStat(Number(stats, "spdef", 1)),
            RosterRules.SafeStat(Number(stats, "speed", 1)),
            type,
            COST: 0,
            EVOLUTION_LEVEL: 0,
            SPRITE: sprite?.Attribute("folder")?.Value,
            SPRITE_SLICE: Math.Max(RosterRules.MIN_SLICE,
                Number(sprite, "slice", Core.GameEntityRenderConfig.DEFAULT_SLICE)),
            SPRITE_SCALE: Math.Max(RosterRules.MIN_SCALE,
                Number(sprite, "scale", Core.GameEntityRenderConfig.DEFAULT_SCALE)),
            FOOT_Y: Number(sprite, "footY", 0),
            FOOT_W: Number(sprite, "footW", 0));

        form.LEGENDARY = NODE.Attribute("legendary")?.Value == "true";

        return (form, evolution?.Attribute("to")?.Value);
    }

    private static int Number(XElement? NODE, string ATTRIBUTE, int FALLBACK)
    {
        string? raw = NODE?.Attribute(ATTRIBUTE)?.Value;

        return raw != null && int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                   System.Globalization.CultureInfo.InvariantCulture, out int value)
            ? value
            : FALLBACK;
    }

    public static Pokemon? Create(string NAME)
    {
        for (int i = 0; i < ALL.Count; i++)
            if (string.Equals(ALL[i].NAME, NAME, StringComparison.OrdinalIgnoreCase)) return ALL[i].Clone();

        return null;
    }

    public static Pokemon? RandomLegendary()
        => LEGENDARY.Count == 0 ? null : LEGENDARY[System.Random.Shared.Next(LEGENDARY.Count)].Clone();

    public static Pokemon? RandomWeighted(int LEVEL, HashSet<string>? EXCLUDE_LINES = null)
    {
        if (SHOP.Count == 0) return null;

        int minCost = int.MaxValue;
        int maxCost = 0;
        for (int i = 0; i < SHOP.Count; i++)
        {
            minCost = Math.Min(minCost, SHOP[i].COST);
            maxCost = Math.Max(maxCost, SHOP[i].COST);
        }

        int span = Math.Max(1, maxCost - minCost);
        float unlock = Math.Clamp(LEVEL * Balance.RARITY_UNLOCK_PER_ROUND, 0f, 0.6f);

        long total = 0;
        var weights = new int[SHOP.Count];

        for (int i = 0; i < SHOP.Count; i++)
        {
            Pokemon form = SHOP[i];
            if (EXCLUDE_LINES != null && EXCLUDE_LINES.Contains(form.LINE)) continue;

            float position = (form.COST - minCost) / (float)span;
            float weight = MathF.Pow(Math.Max(0f, 1f - position + unlock), Balance.RARITY_CURVE);

            weights[i] = Math.Max(1, (int)(weight * 1000f));
            total += weights[i];
        }

        if (total <= 0) return null;

        long roll = (long)(System.Random.Shared.NextDouble() * total);
        for (int i = 0; i < SHOP.Count; i++)
        {
            if (weights[i] == 0) continue;

            roll -= weights[i];
            if (roll < 0) return AtShopLevel(SHOP[i], LEVEL);
        }

        return AtShopLevel(SHOP[^1], LEVEL);
    }

    public static Pokemon? CardOfLine(string LINE, int ROUND)
    {
        for (int i = 0; i < SHOP.Count; i++)
            if (string.Equals(SHOP[i].LINE, LINE, StringComparison.Ordinal)) return AtShopLevel(SHOP[i], ROUND);

        return null;
    }

    private static Pokemon AtShopLevel(Pokemon FORM, int ROUND)
    {
        Pokemon card = FORM.Clone();
        int level = Balance.ShopLevel(ROUND);

        while (card.LEVEL < level) card.LevelUp();
        for (int guard = 0; guard < 4 && card.CanEvolve; guard++) card.Evolve();

        return card;
    }

    public static int EffectiveTotal(Pokemon FORM, int LEVEL)
    {
        Pokemon current = FORM;
        for (int guard = 0; guard < 4 && current.EVOLUTION != null && LEVEL >= current.EVOLUTION_LEVEL; guard++)
            current = current.EVOLUTION;

        return current.BaseStatTotal;
    }

    public static int PowerCeiling(int LEVEL)
    {
        int ceiling = 0;
        for (int i = 0; i < SHOP.Count; i++) ceiling = Math.Max(ceiling, EffectiveTotal(SHOP[i], LEVEL));
        return ceiling;
    }

    public static Pokemon? RandomNearPower(float TARGET, int LEVEL, float SPREAD)
    {
        if (SHOP.Count == 0) return null;

        double spread = Math.Max(1f, SPREAD);
        var weights = new double[SHOP.Count];
        double total = 0;

        for (int i = 0; i < SHOP.Count; i++)
        {
            double z = (EffectiveTotal(SHOP[i], LEVEL) - TARGET) / spread;
            weights[i] = Math.Exp(-0.5 * z * z) + 1e-6;
            total += weights[i];
        }

        double roll = System.Random.Shared.NextDouble() * total;
        for (int i = 0; i < SHOP.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0) return SHOP[i].Clone();
        }

        return SHOP[^1].Clone();
    }
}
