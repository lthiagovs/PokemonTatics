using System.Collections.Generic;
using ENGINE.MODELS;
public static class PokemonDatabase
{
    public static List<Pokemon> PokemonList = new List<Pokemon>()
    {
        new Pokemon("Bulbasaur",  45, 49, 65, 49, 65, 45, PokemonType.GRASS,  10, 16, null),
        new Pokemon("Charmander", 39, 52, 43, 60, 50, 65, PokemonType.FIRE,   10, 16, null),
        new Pokemon("Squirtle",   44, 48, 65, 43, 64, 43, PokemonType.WATER,  10, 16, null),
        new Pokemon("Sandshrew",  50, 75, 85, 20, 30, 40, PokemonType.GROUND, 10, 22, null),
    };
}