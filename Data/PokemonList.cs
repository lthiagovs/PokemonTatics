using System.Collections.Generic;
using ENGINE.MODELS;

public static class PokemonDatabase
{
    public static List<Pokemon> PokemonList = new List<Pokemon>()
    {
        new Pokemon("Bulbasaur", 45, 49, 65, 49, 65, 45),
        new Pokemon("Ivysaur", 60, 62, 80, 63, 80, 60),
        new Pokemon("Venusaur", 80, 82, 100, 83, 100, 80),

        new Pokemon("Charmander", 39, 52, 60, 43, 50, 65),
        new Pokemon("Charmeleon", 58, 64, 80, 58, 65, 80),
        new Pokemon("Charizard", 78, 84, 109, 78, 85, 100),

        new Pokemon("Squirtle", 44, 48, 50, 65, 64, 43),
        new Pokemon("Wartortle", 59, 63, 65, 80, 80, 58),
        new Pokemon("Blastoise", 79, 83, 85, 100, 105, 78),

        new Pokemon("Caterpie", 45, 30, 20, 35, 20, 45),
        new Pokemon("Metapod", 50, 20, 25, 55, 25, 30),
        new Pokemon("Butterfree", 60, 45, 90, 50, 80, 70),

        new Pokemon("Weedle", 40, 35, 20, 30, 20, 50),
        new Pokemon("Kakuna", 45, 25, 25, 50, 25, 35),
        new Pokemon("Beedrill", 65, 90, 45, 40, 80, 75),
    };
}