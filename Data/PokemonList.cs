using System.Collections.Generic;
using ENGINE.MODELS;

public static class PokemonDatabase
{
    // === FORMAS FINAIS (sem evolução) ===
    private static Pokemon _charizard  = new Pokemon("Charizard",   78,  84, 109,  78,  85, 100, PokemonType.FIRE,    0, -1, "scratch", null);
    private static Pokemon _blastoise  = new Pokemon("Blastoise",   79,  83,  85, 100, 105,  78, PokemonType.WATER,   0, -1, "scratch", null);
    private static Pokemon _venusaur   = new Pokemon("Venusaur",    80,  82, 100,  83, 100,  80, PokemonType.GRASS,   0, -1, "scratch", null);
    private static Pokemon _dragonite  = new Pokemon("Dragonite",   91, 134, 100,  95,  80,  80, PokemonType.DRAGON,  0, -1, "scratch", null);
    private static Pokemon _gengar     = new Pokemon("Gengar",       60,  65, 130,  60,  75, 110, PokemonType.GHOST,   0, -1, "scratch", null);
    private static Pokemon _muk        = new Pokemon("Muk",         105, 105,  65,  65, 100,  50, PokemonType.POISON,  0, -1, "scratch", null);
    private static Pokemon _sandslash  = new Pokemon("Sandslash",    75, 100,  45,  55,  55,  65, PokemonType.GROUND,  0, -1, "scratch", null);
    private static Pokemon _primeape   = new Pokemon("Primeape",     65, 105,  60,  60,  70,  95, PokemonType.FIGHT,   0, -1, "scratch", null);

    // === FORMAS INTERMEDIÁRIAS ===
    private static Pokemon _charmeleon = new Pokemon("Charmeleon",   58,  64,  80,  58,  65,  80, PokemonType.FIRE,    0, 36, "scratch", _charizard);
    private static Pokemon _wartortle  = new Pokemon("Wartortle",    59,  63,  65,  80,  80,  58, PokemonType.WATER,   0, 36, "scratch", _blastoise);
    private static Pokemon _ivysaur    = new Pokemon("Ivysaur",      60,  62,  80,  63,  80,  60, PokemonType.GRASS,   0, 32, "scratch", _venusaur);
    private static Pokemon _dragonair  = new Pokemon("Dragonair",    61,  84,  65,  70,  70,  70, PokemonType.DRAGON,  0, 55, "scratch", _dragonite);
    private static Pokemon _haunter    = new Pokemon("Haunter",       45,  50,  95,  45,  55,  95, PokemonType.GHOST,   0, 36, "scratch", _gengar);
    private static Pokemon _grimer     = new Pokemon("Grimer",        80,  80,  50,  50,  40,  25, PokemonType.POISON,  0, 38, "scratch", _muk);

    public static List<Pokemon> PokemonList = new List<Pokemon>()
    {
        // ---------- GRASS ----------
        new Pokemon("Bulbasaur",  45,  49,  65,  49,  65,  45, PokemonType.GRASS,  10, 2, "scratch", _ivysaur),

        // ---------- FIRE ----------
        new Pokemon("Charmander", 39,  52,  60,  43,  50,  65, PokemonType.FIRE,   10, 2, "scratch", _charmeleon),

        // ---------- WATER ----------
        new Pokemon("Squirtle",   44,  48,  50,  65,  64,  43, PokemonType.WATER,  10, 2, "scratch", _wartortle),

        // ---------- GROUND ----------
        new Pokemon("Sandshrew",  50,  75,  20,  35,  30,  40, PokemonType.GROUND, 10, 2, "scratch", _sandslash),

        // ---------- DRAGON ----------
        new Pokemon("Dratini",    41,  64,  45,  50,  50,  50, PokemonType.DRAGON, 10, 2, "scratch", _dragonair),

        // ---------- BUG ----------
        new Pokemon("Pinsir",     65, 125, 100,  55,  70,  85, PokemonType.BUG,    10, -1, "scratch", null),

        // ---------- GHOST ----------
        new Pokemon("Gastly",     30,  35,  65,  35,  35,  80, PokemonType.GHOST,  10, 2, "scratch", _haunter),

        // ---------- POISON ----------
        new Pokemon("Grimer",     80,  80,  50,  50,  40,  25, PokemonType.POISON, 10, 2, "scratch", _grimer.EVOLUTION),

        // ---------- FIGHTING ----------
        new Pokemon("Mankey",     40,  80,  35,  35,  45,  70, PokemonType.FIGHT,  10, 2, "scratch", _primeape),
    };
}