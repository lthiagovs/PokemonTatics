using System;
using System.Collections.Generic;
using PokemonTFT.Data;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public enum ItemCombo
{
    NONE,
    PLAGUE,
    FRENZY,
    BULWARK,
    CATACLYSM,
    SIPHON
}

public enum EquipResult
{
    NOTHING,
    EQUIPPED,
    FULL
}

public sealed class ItemPile
{
    public required Item ITEM { get; init; }
    public int COUNT { get; set; }
}

public static class ItemLogic
{
    private static readonly List<ItemPile> BAG = [];

    public static IReadOnlyList<ItemPile> Bag => BAG;

    public static Item? Carried { get; private set; }

    public static int Signature
    {
        get
        {
            int hash = Carried?.ID.GetHashCode() ?? 17;
            for (int i = 0; i < BAG.Count; i++)
                hash = hash * 31 + BAG[i].ITEM.ID.GetHashCode() * 7 + BAG[i].COUNT;
            return hash;
        }
    }

    public static void Clear()
    {
        BAG.Clear();
        Carried = null;
    }

    public static void Store(Item ITEM)
    {
        ItemPile? stack = Find(ITEM.ID);
        if (stack != null)
        {
            stack.COUNT++;
            return;
        }

        BAG.Add(new ItemPile { ITEM = ITEM, COUNT = 1 });
    }

    public static bool PickUp(Item ITEM)
    {
        if (Carried != null) return false;

        ItemPile? stack = Find(ITEM.ID);
        if (stack == null || stack.COUNT <= 0) return false;

        stack.COUNT--;
        if (stack.COUNT <= 0) BAG.Remove(stack);

        Carried = stack.ITEM;
        return true;
    }

    public static void ReturnCarried()
    {
        if (Carried == null) return;

        Item item = Carried;
        Carried = null;
        Store(item);
    }

    public static bool IsFull(Pokemon POKEMON) => POKEMON.ITEMS.Count >= Balance.MAX_ITEMS_PER_POKEMON;

    public static EquipResult EquipCarried(Pokemon POKEMON)
    {
        if (Carried == null) return EquipResult.NOTHING;
        if (IsFull(POKEMON)) return EquipResult.FULL;

        POKEMON.ITEMS.Add(Carried.ID);
        Carried = null;

        POKEMON.RecalculateStats();
        return EquipResult.EQUIPPED;
    }

    public static void Reclaim(Pokemon POKEMON)
    {
        if (POKEMON.ITEMS.Count == 0) return;

        for (int i = 0; i < POKEMON.ITEMS.Count; i++)
        {
            if (ItemDatabase.Find(POKEMON.ITEMS[i]) is { } item) Store(item);
        }

        POKEMON.ITEMS.Clear();
        POKEMON.RecalculateStats();
    }

    private static ItemPile? Find(string ID)
    {
        for (int i = 0; i < BAG.Count; i++)
            if (string.Equals(BAG[i].ITEM.ID, ID, StringComparison.Ordinal)) return BAG[i];
        return null;
    }

    public static StatModifiers Modifiers(IReadOnlyList<string> ITEMS)
    {
        if (ITEMS.Count == 0) return StatModifiers.NONE;

        float hp = 0, atk = 0, spatk = 0, def = 0, spdef = 0, speed = 0, size = 0;

        for (int i = 0; i < ITEMS.Count; i++)
        {
            if (ItemDatabase.Find(ITEMS[i]) is not { } item) continue;

            switch (item.EFFECT)
            {
                case ItemEffect.ATTACK:        atk   += item.VALUE; break;
                case ItemEffect.SPECIAL_POWER: spatk += item.VALUE; break;
                case ItemEffect.HEALTH:        hp    += item.VALUE; break;
                case ItemEffect.SPEED:         speed += item.VALUE; break;

                case ItemEffect.DEFENCE:
                    def   += item.VALUE;
                    spdef += item.VALUE;
                    break;

                case ItemEffect.GIANT:
                    size  += item.VALUE;
                    hp    += Balance.GIANT_HEALTH_BONUS;
                    speed -= Balance.GIANT_SPEED_PENALTY;
                    break;
            }
        }

        return new StatModifiers(
            Factor(hp), Factor(atk), Factor(spatk), Factor(def), Factor(spdef), Factor(speed),
            1f + size / 100f);
    }

    private static float Factor(float PERCENT) => Math.Max(0.1f, 1f + PERCENT / 100f);

    public static bool Has(Pokemon POKEMON, ItemEffect EFFECT) => Value(POKEMON, EFFECT) > 0;

    public static int Value(Pokemon POKEMON, ItemEffect EFFECT)
    {
        int total = 0;
        for (int i = 0; i < POKEMON.ITEMS.Count; i++)
        {
            Item? item = ItemDatabase.Find(POKEMON.ITEMS[i]);
            if (item != null && item.EFFECT == EFFECT) total += item.VALUE;
        }
        return total;
    }

    public static List<ItemCombo> Combos(Pokemon POKEMON)
    {
        var found = new List<ItemCombo>();
        if (POKEMON.ITEMS.Count < 2) return found;

        if (Has(POKEMON, ItemEffect.BURN_HIT) && Has(POKEMON, ItemEffect.POISON_HIT)) found.Add(ItemCombo.PLAGUE);
        if (Has(POKEMON, ItemEffect.ATTACK) && Has(POKEMON, ItemEffect.SPEED)) found.Add(ItemCombo.FRENZY);
        if (Has(POKEMON, ItemEffect.DEFENCE) && Has(POKEMON, ItemEffect.HEALTH)) found.Add(ItemCombo.BULWARK);
        if (Has(POKEMON, ItemEffect.GIANT) && Has(POKEMON, ItemEffect.SPLASH)) found.Add(ItemCombo.CATACLYSM);
        if (Has(POKEMON, ItemEffect.LIFESTEAL) && Has(POKEMON, ItemEffect.SPECIAL_POWER)) found.Add(ItemCombo.SIPHON);

        return found;
    }

    public static bool HasCombo(Pokemon POKEMON, ItemCombo COMBO) => Combos(POKEMON).Contains(COMBO);

    public static string Name(ItemCombo COMBO) => COMBO switch
    {
        ItemCombo.PLAGUE    => "PLAGUE",
        ItemCombo.FRENZY    => "FRENZY",
        ItemCombo.BULWARK   => "BULWARK",
        ItemCombo.CATACLYSM => "CATACLYSM",
        ItemCombo.SIPHON    => "SIPHON",
        _                   => "NONE"
    };

    public static string Describe(ItemCombo COMBO) => COMBO switch
    {
        ItemCombo.PLAGUE    => $"BURN AND POISON LAST {Balance.COMBO_PLAGUE_BONUS}% LONGER",
        ItemCombo.FRENZY    => $"ATTACKS {Balance.COMBO_FRENZY_BONUS}% FASTER",
        ItemCombo.BULWARK   => $"TAKES {Balance.COMBO_BULWARK_BONUS}% LESS DAMAGE",
        ItemCombo.CATACLYSM => $"SPLASH REACHES {Balance.COMBO_CATACLYSM_BONUS}% FURTHER",
        ItemCombo.SIPHON    => "LIFESTEAL ALSO WORKS ON SPLASH DAMAGE",
        _                   => string.Empty
    };
}
