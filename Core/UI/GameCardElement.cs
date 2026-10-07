using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public sealed class GameCardElement : GameInterfaceElement
{
    private const double HOVER_DELAY = 1.0;

    private Pokemon? _pokemon;
    private double _hoverElapsed;

    public GameCardElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT)
    {
        HOVERABLE = true;
    }

    public void SetPokemon(Pokemon POKEMON) => _pokemon = POKEMON;

    public Pokemon? GetPokemon() => _pokemon;

    public override void Update()
    {
        base.Update();

        if (_pokemon == null) return;

        if (!IsHovered())
        {
            _hoverElapsed = 0;
            GameTooltip.Hide(this);
            return;
        }

        GetRendererConfig().COLOR = Color.White * 0.5f;
        if (!GameGlobals.GAME_STARTED) GameMouse.RequestHoverCursor();

        _hoverElapsed += GameTimeLogic.DELTA;
        if (_hoverElapsed >= HOVER_DELAY) GameTooltip.Show(this, PokemonHintText.Build(_pokemon));

        if (GameMouse.LeftPressed() && !GameMouse.HasCarry()) TryBuy(_pokemon);
    }

    private static void TryBuy(Pokemon POKEMON)
    {
        if (POKEMON.COST > GameGlobals.PLAYER_MANA) return;

        if (GameGlobals.GAME_STARTED) return;

        GameMouse.ConsumeClick();

        PokemonEntity? owned = GameTableLogic.GetPlayerPokemon(POKEMON.NAME);
        if (owned?.POKEMON != null)
        {
            owned.POKEMON.LevelUp();
            GameMusic.PlayLevelUp();
            GameGlobals.ChangeMana(-POKEMON.COST);

            Rectangle bounds = owned.GetRectangle();
            FloatingText.SpawnStatus("LEVEL UP", new Point(bounds.X + bounds.Width / 2, bounds.Y), new Color(255, 220, 90));
            owned.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.4f));

            Screens.EvolutionScene.Enqueue(owned);
            return;
        }

        if (GameTableLogic.GetPlayerPokemonsCount() >= GameGlobals.GetTableSize()) return;

        GameGlobals.ChangeMana(-POKEMON.COST);

        var carry = new PokemonEntity(GameMouse.GetPos().X, GameMouse.GetPos().Y)
        {
            POKEMON   = POKEMON.Clone(),
            ENEMY     = false,
            PURCHASED = true,
            CARRIED   = true
        };
        carry.SetEffect(new RenderEffect(RenderEffectType.FADE_IN, 0.15f));
        GameMouse.SetCarry(carry);
    }
}
