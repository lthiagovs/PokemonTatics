using PokemonTFT.Models;

namespace PokemonTFT.UI;

public static class PokemonHintText
{
    public static string Build(Pokemon POKEMON) =>
        "\n" +
        $"    --- {POKEMON.NAME.ToUpperInvariant()} ---    \n" +
        $"    STYLE: {POKEMON.GetStyle().ToString().Replace("_", " ")}    \n" +
        $"    TYPE: {POKEMON.TYPE}\n" +
        "    ---------------------    \n" +
        $"    LVL: {POKEMON.LEVEL} | XP: {POKEMON.XP}/{POKEMON.XPToNextLevel()}    \n" +
        $"    HP: {POKEMON.HP}/{POKEMON.MAX_HP}    \n" +
        $"    ATK: {POKEMON.ATK} | DEF: {POKEMON.DEF}    \n" +
        $"    SPATK: {POKEMON.SPATK} | SPDEF: {POKEMON.SPDEF}    \n" +
        $"    SPEED: {POKEMON.SPEED}    \n" +
        $"    MANA COST: {POKEMON.COST}    " +
        "\n";
}
