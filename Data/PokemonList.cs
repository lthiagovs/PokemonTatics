using System.Collections.Generic;
using PokemonTFT.Models;

namespace PokemonTFT.Data;

public static class PokemonDatabase
{
    private const int SLICE_40 = 40;

    private static readonly Pokemon CHARIZARD = new("Charizard", 78,  84, 109,  78,  85, 100, PokemonType.FIRE,   0, -1);
    private static readonly Pokemon BLASTOISE = new("Blastoise", 79,  83,  85, 100, 105,  78, PokemonType.WATER,  0, -1);
    private static readonly Pokemon VENUSAUR  = new("Venusaur",  80,  82, 100,  83, 100,  80, PokemonType.GRASS,  0, -1);
    private static readonly Pokemon GENGAR    = new("Gengar",    60,  65, 130,  60,  75, 110, PokemonType.GHOST,  0, -1);
    private static readonly Pokemon MUK       = new("Muk",      105, 105,  65,  65, 100,  50, PokemonType.POISON, 0, -1);
    private static readonly Pokemon SANDSLASH = new("Sandslash", 75, 100,  45,  55,  55,  65, PokemonType.GROUND, 0, -1);
    private static readonly Pokemon PRIMEAPE  = new("Primeape",  65, 105,  60,  60,  70,  95, PokemonType.FIGHT,  0, -1);

    private static readonly Pokemon DRAGONITE = new("Dragonite", 91, 134, 100,  95,  80,  80, PokemonType.DRAGON, 0, -1,
        SPRITE_SLICE: SLICE_40);

    private static readonly Pokemon CHARMELEON = new("Charmeleon", 58, 64, 80, 58, 65, 80, PokemonType.FIRE,  0, 36, EVOLUTION: CHARIZARD);
    private static readonly Pokemon IVYSAUR    = new("Ivysaur",    60, 62, 80, 63, 80, 60, PokemonType.GRASS, 0, 32, EVOLUTION: VENUSAUR);
    private static readonly Pokemon HAUNTER    = new("Haunter",    45, 50, 95, 45, 55, 95, PokemonType.GHOST, 0, 36, EVOLUTION: GENGAR);

    private static readonly Pokemon WARTORTLE = new("Wartortle", 59, 63, 65, 80, 80, 58, PokemonType.WATER, 0, 36,
        EVOLUTION: BLASTOISE, SPRITE: "Wartotle");

    private static readonly Pokemon DRAGONAIR = new("Dragonair", 61, 84, 65, 70, 70, 70, PokemonType.DRAGON, 0, 55,
        EVOLUTION: DRAGONITE, SPRITE_SLICE: SLICE_40);

    public static readonly IReadOnlyList<Pokemon> PokemonList = new List<Pokemon>
    {
        new("Bulbasaur",  45, 49, 65, 49, 65, 45, PokemonType.GRASS,  10, 2, EVOLUTION: IVYSAUR),
        new("Charmander", 39, 52, 60, 43, 50, 65, PokemonType.FIRE,   10, 2, EVOLUTION: CHARMELEON),
        new("Squirtle",   44, 48, 50, 65, 64, 43, PokemonType.WATER,  10, 2, EVOLUTION: WARTORTLE),
        new("Sandshrew",  50, 75, 20, 35, 30, 40, PokemonType.GROUND, 10, 2, EVOLUTION: SANDSLASH),
        new("Dratini",    41, 64, 45, 50, 50, 50, PokemonType.DRAGON, 10, 2, EVOLUTION: DRAGONAIR),
        new("Pinsir",     65,125,100, 55, 70, 85, PokemonType.BUG,    10, -1),
        new("Gastly",     30, 35, 65, 35, 35, 80, PokemonType.GHOST,  10, 2, EVOLUTION: HAUNTER),
        new("Grimer",     80, 80, 50, 50, 40, 25, PokemonType.POISON, 10, 2, EVOLUTION: MUK),
        new("Mankey",     40, 80, 35, 35, 45, 70, PokemonType.FIGHT,  10, 2, EVOLUTION: PRIMEAPE),
    };

    public static Pokemon Random() => PokemonList[System.Random.Shared.Next(PokemonList.Count)].Clone();

    private static readonly Pokemon[] BY_POWER =
        System.Linq.Enumerable.ToArray(
            System.Linq.Enumerable.OrderBy(PokemonList, p => p.BaseStatTotal));

    public static Pokemon RandomInBand(int MAX_STAT_TOTAL)
    {
        int allowed = 0;
        while (allowed < BY_POWER.Length && BY_POWER[allowed].BaseStatTotal <= MAX_STAT_TOTAL) allowed++;
        if (allowed == 0) allowed = 1;

        return BY_POWER[System.Random.Shared.Next(allowed)].Clone();
    }
}
