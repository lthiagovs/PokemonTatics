using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public sealed class GameCardElement : GameInterfaceElement
{
    private const double PULSE_SPEED = 4.2;
    private const float PULSE_DEPTH = 0.22f;

    private readonly System.Collections.Generic.List<GameElement> _parts = [];

    private Pokemon? _pokemon;
    private double _pulse;

    public GameCardElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT)
    {
        HOVERABLE = true;
    }

    public void SetPokemon(Pokemon POKEMON) => _pokemon = POKEMON;

    public void AddPart(GameElement PART) => _parts.Add(PART);

    private void SetPartsAlpha(float ALPHA)
    {
        RENDER_ALPHA = ALPHA;
        for (int i = 0; i < _parts.Count; i++) _parts[i].RENDER_ALPHA = ALPHA;
    }

    public Pokemon? GetPokemon() => _pokemon;

    public override void Update()
    {
        base.Update();

        if (_pokemon == null) return;

        bool owned = !GameGlobals.GAME_STARTED && GameTableLogic.GetPlayerLine(_pokemon.LINE) != null;
        if (owned)
        {
            _pulse += GameTimeLogic.DELTA * PULSE_SPEED;
            SetPartsAlpha(1f - PULSE_DEPTH * (0.5f + 0.5f * (float)System.Math.Sin(_pulse)));
        }
        else if (_pulse != 0)
        {
            _pulse = 0;
            SetPartsAlpha(1f);
        }

        if (!IsHovered()) return;

        GetRendererConfig().COLOR = Color.White * 0.5f;
        if (!GameGlobals.GAME_STARTED) GameMouse.RequestHoverCursor();

        if (GameMouse.RightPressed() && PokemonModal.CanOpen)
        {
            PokemonModal.Open(_pokemon);
            return;
        }

        if (GameMouse.LeftPressed() && !GameMouse.HasCarry()) TryBuy(_pokemon);
    }

    private static void TryBuy(Pokemon POKEMON)
    {
        if (ItemLogic.Carried != null) return;
        if (POKEMON.COST > GameGlobals.PLAYER_MANA) return;

        if (GameGlobals.GAME_STARTED) return;

        GameMouse.ConsumeClick();

        PokemonEntity? owned = GameTableLogic.GetPlayerLine(POKEMON.LINE);
        if (owned?.POKEMON != null)
        {
            GameGlobals.ChangeMana(-POKEMON.COST);
            bool leveled = owned.POKEMON.GainXP(Balance.DUPLICATE_XP) > 0;
            if (leveled) GameMusic.PlayLevelUp();

            Rectangle bounds = owned.GetRectangle();
            FloatingText.SpawnStatus(leveled ? "LEVEL UP" : $"+{Balance.DUPLICATE_XP} XP",
                new Point(bounds.X + bounds.Width / 2, bounds.Y), new Color(255, 220, 90));
            owned.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.4f));

            Screens.EvolutionScene.Enqueue(owned);
            Table.GameDeck.ReplaceCard(POKEMON);
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

        Table.GameDeck.ReplaceCard(POKEMON);
    }
}
