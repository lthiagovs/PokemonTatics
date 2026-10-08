using System.IO;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;
using Xunit;

namespace PokemonTFT.Tests;

public class RosterTests
{
    [Fact]
    public void RealRosterLoads()
    {
        Repository.LoadRoster();
        Assert.True(PokemonDatabase.Ready, PokemonDatabase.PROBLEM);
    }

    [Fact]
    public void ShopCardsRespectCostLevelAndLegendaryRules()
    {
        Repository.LoadRoster();

        for (int round = 1; round <= 60; round++)
        {
            Pokemon? card = PokemonDatabase.RandomWeighted(round);
            Assert.NotNull(card);
            Assert.InRange(card!.COST, 6, 16);
            Assert.False(card.LEGENDARY);
            Assert.Equal(Balance.ShopLevel(round), card.LEVEL);
            Assert.False(card.CanEvolve);
        }
    }

    [Fact]
    public void EvolutionLevelsGrowWithPowerAndStayCapped()
    {
        Repository.LoadRoster();

        Pokemon charmander = PokemonDatabase.Create("Charmander")!;
        Pokemon magikarp = PokemonDatabase.Create("Magikarp")!;

        Assert.InRange(charmander.EVOLUTION_LEVEL, 5, 30);
        Assert.True(charmander.EVOLUTION!.EVOLUTION_LEVEL >= charmander.EVOLUTION_LEVEL + 8);
        Assert.True(magikarp.EVOLUTION_LEVEL > charmander.EVOLUTION_LEVEL);
        Assert.True(magikarp.EVOLUTION_LEVEL <= 30);
    }

    [Fact]
    public void LegendariesOnlyShowUpAsBosses()
    {
        Repository.LoadRoster();

        Assert.True(PokemonDatabase.RandomLegendary()?.LEGENDARY ?? true);
        for (int i = 0; i < 200; i++)
            Assert.False(PokemonDatabase.RandomNearPower(600f, 30, 35f)!.LEGENDARY);
    }

    [Theory]
    [InlineData("<Roster></Roster>")]
    [InlineData("<Roster><Pokemon name=\"A\"")]
    public void BrokenRostersAreReportedInsteadOfThrowing(string XML)
    {
        string path = Path.Combine(Path.GetTempPath(), "pokemon_tactics_roster_test.xml");
        File.WriteAllText(path, XML);

        PokemonDatabase.Load(path);

        Assert.False(PokemonDatabase.Ready);
        Assert.NotEmpty(PokemonDatabase.PROBLEM);
    }

    [Fact]
    public void MissingRosterIsReported()
    {
        PokemonDatabase.Load(Path.Combine(Path.GetTempPath(), "pokemon_tactics_missing.xml"));
        Assert.False(PokemonDatabase.Ready);
    }
}
