using System;
using PokemonTFT.Models;

namespace PokemonTFT.Logic;

public sealed class TypeBonusSnapshot
{
    private static readonly int TYPE_COUNT = Enum.GetValues<PokemonType>().Length;

    public static readonly TypeBonusSnapshot EMPTY = new();

    private readonly int[] _counts = new int[TYPE_COUNT];

    public int TOTAL { get; private set; }

    public void Clear()
    {
        Array.Clear(_counts);
        TOTAL = 0;
    }

    public void Add(PokemonType TYPE)
    {
        _counts[(int)TYPE]++;
        TOTAL++;
    }

    public int Count(PokemonType TYPE) => _counts[(int)TYPE];

    public int Signature()
    {
        int hash = 17;
        for (int i = 0; i < _counts.Length; i++) hash = hash * 31 + _counts[i];
        return hash;
    }
}
