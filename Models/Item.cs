namespace PokemonTFT.Models;

public enum ItemEffect
{
    ATTACK,
    DEFENCE,
    HEALTH,
    SPEED,
    SPECIAL_POWER,
    GIANT,
    SPLASH,
    BURN_HIT,
    POISON_HIT,
    PARALYSIS_HIT,
    LIFESTEAL,
    MANA_ON_KILL
}

public sealed class Item
{
    public required string ID { get; init; }
    public required string NAME { get; init; }
    public required ItemEffect EFFECT { get; init; }
    public required int VALUE { get; init; }
    public required Microsoft.Xna.Framework.Rectangle ICON { get; init; }

    public string Describe() => EFFECT switch
    {
        ItemEffect.ATTACK => $"+{VALUE}% ATTACK",
        ItemEffect.DEFENCE => $"+{VALUE}% DEFENCE AND SP.DEFENCE",
        ItemEffect.HEALTH => $"+{VALUE}% MAX HP",
        ItemEffect.SPEED => $"+{VALUE}% SPEED",
        ItemEffect.SPECIAL_POWER => $"+{VALUE}% SP.ATTACK",
        ItemEffect.GIANT => $"GROWS {VALUE}% LARGER, +15% MAX HP, -10% SPEED",
        ItemEffect.SPLASH => $"BASIC ATTACKS SPLASH {VALUE}% TO NEARBY FOES",
        ItemEffect.BURN_HIT => $"{VALUE}% TO BURN ON EVERY HIT",
        ItemEffect.POISON_HIT => $"{VALUE}% TO POISON ON EVERY HIT",
        ItemEffect.PARALYSIS_HIT => $"{VALUE}% TO PARALYSE ON EVERY HIT",
        ItemEffect.LIFESTEAL => $"HEALS {VALUE}% OF THE DAMAGE DEALT",
        _ => $"+{VALUE} MANA FOR EVERY KILL"
    };
}
