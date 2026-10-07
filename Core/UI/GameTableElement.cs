using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Table;

namespace PokemonTFT.UI;

public sealed class GameTableElement : GameInterfaceElement
{
    private static readonly Color DROP_TARGET_TINT = new(180, 255, 180);

    private static readonly List<GameElement> ENTITIES = [];

    public int TABLE_POSITION_X;
    public int TABLE_POSITION_Y;
    public bool PLAYER_OWN;

    public bool ENEMY_ZONE;

    private PokemonEntity? _pokemon;

    public GameTableElement(int POS_X, int POS_Y, int SIZE_X, int SIZE_Y, bool VISIBLE, GameInterfaceElement? PARENT = null)
        : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    #region ENTITY REGISTRY
    public static List<GameElement> GetTableElements() => ENTITIES;

    public static void RegisterEntity(PokemonEntity ENTITY) => ENTITIES.Add(ENTITY);

    public static void UnregisterEntity(PokemonEntity ENTITY) => ENTITIES.Remove(ENTITY);

    public static void RemoveEnemies() => ENTITIES.RemoveAll(e => e is PokemonEntity { ENEMY: true });
    #endregion

    #region SLOT
    public bool HasPokemon() => _pokemon != null;

    public PokemonEntity? GetPokemon() => _pokemon;

    public void ClearPokemon() => _pokemon = null;

    public void InsertPokemon(PokemonEntity POKEMON)
    {
        Rectangle bounds = GetRectangle();
        int drawSize = POKEMON.GetEntityConfig().DrawSize;

        var position = new Point(
            bounds.X + bounds.Width  / 2 - drawSize / 2,
            bounds.Y + bounds.Height / 2 - drawSize / 2);

        POKEMON.SetPosition(position);
        POKEMON.START     = position;
        POKEMON.DIRECTION = GameDirection.BOTTOM_LEFT;

        _pokemon = POKEMON;
        RegisterEntity(POKEMON);
    }
    #endregion

    public override void Update()
    {
        base.Update();

        GetRendererConfig().IS_HOVERING = false;

        if (GameTable.TryGetZoneFlash(this, out Color flash)) GetRendererConfig().COLOR = flash;

        if (!PLAYER_OWN) return;

        bool carrying = GameMouse.GetCarry() is PokemonEntity;
        bool canPlace = carrying && _pokemon == null;
        bool canPick  = !GameMouse.HasCarry() && _pokemon != null;

        if (canPlace && !GameGlobals.GAME_STARTED)
            GetRendererConfig().COLOR = DROP_TARGET_TINT;

        if (!IsHovered()) return;

        GetRendererConfig().IS_HOVERING = true;

        if (!GameGlobals.GAME_STARTED && (canPlace || canPick)) GameMouse.RequestHoverCursor();

        if (!GameMouse.LeftPressed()) return;

        if (GameMouse.GetCarry() is PokemonEntity carried) PlaceCarried(carried);
        else if (_pokemon != null) PickUp();
    }

    private void PlaceCarried(PokemonEntity CARRIED)
    {
        if (GameGlobals.GAME_STARTED) return;
        if (_pokemon != null) return;

        GameMouse.ConsumeClick();
        GameMouse.ClearCarry();

        var placed = new PokemonEntity(0, 0)
        {
            POKEMON   = CARRIED.POKEMON,
            DIRECTION = CARRIED.DIRECTION,
            ENEMY     = false,
            PURCHASED = CARRIED.PURCHASED,
            CARRIED   = false
        };

        InsertPokemon(placed);
        placed.SetEffect(new RenderEffect(RenderEffectType.FADE_IN, 0.18f));
    }

    private void PickUp()
    {
        if (GameGlobals.GAME_STARTED) return;

        GameMouse.ConsumeClick();

        PokemonEntity picked = _pokemon!;
        picked.DIRECTION = GameDirection.TOP_RIGHT;

        PokemonEntity carry = PokemonEntity.Copy(picked);
        carry.PURCHASED = false;
        carry.CARRIED   = true;
        carry.SetEffect(new RenderEffect(RenderEffectType.FADE_IN, 0.15f));

        GameMouse.SetCarry(carry);
        UnregisterEntity(picked);
        _pokemon = null;
    }
}
