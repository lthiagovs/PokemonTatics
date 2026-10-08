using System;
using System.Collections.Generic;
using System.Linq;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using Xunit;

namespace PokemonTFT.Tests;

public class ItemTests
{
    private static Pokemon Make() => new("Ally", 60, 70, 50, 55, 45, 80, PokemonType.FIRE, 8, 10);

    private static Item Band()
    {
        Repository.LoadItems();
        ItemLogic.Clear();
        return ItemDatabase.Find("power_band")!;
    }

    [Fact]
    public void PickedItemReachesThePokemon()
    {
        Item band = Band();
        Pokemon ally = Make();

        ItemLogic.Store(band);
        Assert.True(ItemLogic.PickUp(band));
        Assert.Equal(EquipResult.EQUIPPED, ItemLogic.EquipCarried(ally));
        Assert.Equal(["power_band"], ally.ITEMS);
        Assert.Null(ItemLogic.Carried);
        Assert.Empty(ItemLogic.Bag);
    }

    [Fact]
    public void DuplicatesStack()
    {
        Item band = Band();
        ItemLogic.Store(band);
        ItemLogic.Store(band);

        Assert.Single(ItemLogic.Bag);
        Assert.Equal(2, ItemLogic.Bag[0].COUNT);
    }

    [Fact]
    public void PokemonHoldsAtMostThreeItems()
    {
        Item band = Band();
        Pokemon ally = Make();
        for (int i = 0; i < 4; i++) ItemLogic.Store(band);

        for (int i = 0; i < 3; i++)
        {
            ItemLogic.PickUp(band);
            Assert.Equal(EquipResult.EQUIPPED, ItemLogic.EquipCarried(ally));
        }

        ItemLogic.PickUp(band);
        Assert.Equal(EquipResult.FULL, ItemLogic.EquipCarried(ally));
        Assert.Equal(band, ItemLogic.Carried);
    }

    [Fact]
    public void ItemsAreNeverCreatedOrLost()
    {
        Band();
        var random = new Random(1234);
        List<Pokemon> team = Enumerable.Range(0, 5).Select(_ => Make()).ToList();
        int stored = 0;

        for (int step = 0; step < 20_000; step++)
        {
            Pokemon target = team[random.Next(team.Count)];
            switch (random.Next(6))
            {
                case 0:
                case 1:
                    ItemLogic.Store(ItemDatabase.All[random.Next(ItemDatabase.All.Count)]);
                    stored++;
                    break;
                case 2:
                    if (ItemLogic.Bag.Count > 0) ItemLogic.PickUp(ItemLogic.Bag[random.Next(ItemLogic.Bag.Count)].ITEM);
                    break;
                case 3:
                    ItemLogic.ReturnCarried();
                    break;
                case 4:
                    ItemLogic.EquipCarried(target);
                    break;
                default:
                    ItemLogic.Reclaim(target);
                    break;
            }

            int owned = ItemLogic.Bag.Sum(pile => pile.COUNT) + (ItemLogic.Carried != null ? 1 : 0)
                      + team.Sum(pokemon => pokemon.ITEMS.Count);
            Assert.Equal(stored, owned);
            Assert.All(team, pokemon => Assert.True(pokemon.ITEMS.Count <= Balance.MAX_ITEMS_PER_POKEMON));
        }
    }
}
