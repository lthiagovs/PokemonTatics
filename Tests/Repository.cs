using System;
using System.IO;
using PokemonTFT.Data;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PokemonTFT.Tests;

public static class Repository
{
    public static string Root { get; } = Find();

    public static string Path(string RELATIVE) => System.IO.Path.Combine(Root, RELATIVE);

    public static void LoadRoster() => PokemonDatabase.Load(Path("Data/pokemons.xml"));

    public static void LoadItems() => ItemDatabase.Load(Path("Data/items.xml"));

    private static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(System.IO.Path.Combine(directory.FullName, "PokemonTFT.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("repository root not found");
    }
}
