using System.Collections.Generic;
using ENGINE.MODELS;
public static class PokemonDatabase
{
    public static List<Pokemon> PokemonList = new List<Pokemon>()
    {
        new Pokemon("Bulbasaur",  45,  49,  65,  49,  65,  45, PokemonType.GRASS,  1),
        new Pokemon("Ivysaur",    60,  62,  80,  63,  80,  60, PokemonType.GRASS,  2),
        new Pokemon("Venusaur",   80,  82, 100,  83, 100,  80, PokemonType.GRASS,  3),
    };
}