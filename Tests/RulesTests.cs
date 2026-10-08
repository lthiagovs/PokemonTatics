using System;
using System.Collections.Generic;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using Xunit;

namespace PokemonTFT.Tests;

public class RulesTests
{
    [Fact]
    public void StatsGrowLinearlyWithLevel()
    {
        Assert.Equal(1f, Balance.LevelGrowth(1));
        Assert.Equal(Balance.LevelGrowth(3) - Balance.LevelGrowth(2), Balance.LevelGrowth(11) - Balance.LevelGrowth(10), 3);
    }

    [Fact]
    public void LevelingNeverChangesBaseStats()
    {
        var pokemon = new Pokemon("Test", 50, 60, 70, 80, 90, 100, PokemonType.WATER, 8, 10);
        for (int i = 0; i < 20; i++) pokemon.LevelUp();

        Assert.Equal(60, pokemon.BASE_ATK);
        Assert.Equal((int)MathF.Round(60 * Balance.LevelGrowth(21)), pokemon.ATK);
    }

    [Fact]
    public void FieldGrowsEveryFiveRoundsUpToTheCap()
    {
        int cap = Balance.FieldCap(144);

        Assert.Equal(96, cap);
        Assert.Equal(1, Balance.TableSize(1, cap));
        Assert.Equal(1, Balance.TableSize(5, cap));
        Assert.Equal(2, Balance.TableSize(6, cap));
        Assert.Equal(3, Balance.TableSize(500, 3));
    }

    [Theory]
    [InlineData(Difficulty.EASY, 1)]
    [InlineData(Difficulty.MEDIUM, 3)]
    [InlineData(Difficulty.HARD, 6)]
    public void DifficultySetsTheOfferInterval(Difficulty LEVEL, int ROUNDS)
    {
        Assert.Equal(ROUNDS, ShopRules.OfferInterval(LEVEL));
        Assert.False(ShopRules.OfferDue(ROUNDS, 1, LEVEL));
        Assert.True(ShopRules.OfferDue(1 + ROUNDS, 1, LEVEL));
    }

    [Fact]
    public void GuaranteePutsAnOwnedLineInTheShop()
    {
        Repository.LoadRoster();
        string owned = PokemonDatabase.Create("Charmander")!.LINE;

        var deck = new List<Pokemon>();
        while (deck.Count < 7)
        {
            Pokemon card = PokemonDatabase.RandomWeighted(5, [owned])!;
            deck.Add(card);
        }

        Assert.False(ShopRules.Offers(deck, [owned]));
        Assert.True(ShopRules.Guarantee(deck, [owned], 5, new Random(3)));
        Assert.True(ShopRules.Offers(deck, [owned]));
    }

    [Fact]
    public void OvertimeOnlyStartsAfterThirtySeconds()
    {
        Assert.Equal(1f, Balance.OvertimeScale(Balance.OVERTIME_SECONDS));
        Assert.True(Balance.OvertimeScale(Balance.OVERTIME_SECONDS + 10f) > 2f);
    }
}
