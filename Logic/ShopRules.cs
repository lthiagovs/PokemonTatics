using System;
using System.Collections.Generic;
using PokemonTFT.Data;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public enum Difficulty
{
    EASY,
    MEDIUM,
    HARD
}

public static class ShopRules
{
    public static int OfferInterval(Difficulty LEVEL) => LEVEL switch
    {
        Difficulty.EASY => Balance.OFFER_EVERY_EASY,
        Difficulty.HARD => Balance.OFFER_EVERY_HARD,
        _               => Balance.OFFER_EVERY_MEDIUM
    };

    public static bool OfferDue(int ROUND, int LAST_OFFER, Difficulty LEVEL)
        => ROUND - LAST_OFFER >= OfferInterval(LEVEL);

    public static bool Offers(IReadOnlyList<Pokemon> DECK, IReadOnlyList<string> OWNED)
    {
        for (int i = 0; i < DECK.Count; i++)
            for (int j = 0; j < OWNED.Count; j++)
                if (string.Equals(DECK[i].LINE, OWNED[j], StringComparison.Ordinal)) return true;
        return false;
    }

    public static bool Guarantee(List<Pokemon> DECK, IReadOnlyList<string> OWNED, int ROUND, Random RANDOM)
    {
        if (DECK.Count == 0 || OWNED.Count == 0) return false;

        var present = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < DECK.Count; i++) present.Add(DECK[i].LINE);

        var candidates = new List<string>();
        for (int i = 0; i < OWNED.Count; i++)
            if (!present.Contains(OWNED[i]) && !candidates.Contains(OWNED[i])) candidates.Add(OWNED[i]);

        for (int attempt = candidates.Count; attempt > 0; attempt--)
        {
            int pick = RANDOM.Next(candidates.Count);
            string line = candidates[pick];
            candidates.RemoveAt(pick);

            if (PokemonDatabase.CardOfLine(line, ROUND) is not { } card) continue;

            DECK[RANDOM.Next(DECK.Count)] = card;
            return true;
        }

        return false;
    }
}
