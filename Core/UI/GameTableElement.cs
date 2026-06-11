using System;
using System.Collections.Generic;
using GAME.CORE;
using GAME.UI;
using Microsoft.Xna.Framework;

public class GameTableElement : GameInterfaceElement
{

    private static List<GameElement> TABLE_ELEMENTS = new List<GameElement>();

    public int TABLE_POSITION_X = 0;
    public int TABLE_POSITION_Y = 0;

    public GameTableElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) : 
    base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { }

    private PokemonEntity POKEMON_ENTITY = null;

    public bool PLAYER_OWN = false;

    public static List<GameElement> GetTableElements() { return GameTableElement.TABLE_ELEMENTS; }

    public bool HasPokemon() { return POKEMON_ENTITY!=null; }

    public PokemonEntity GetPokemon() { return POKEMON_ENTITY; }

    public void InsertPokemon(PokemonEntity POKEMON) { 
        GameEntityRenderConfig cfg = new GameEntityRenderConfig();
                cfg.TEXTURE_PATH   = "Pokemons/" + POKEMON.POKEMON.NAME + "/moveset";
                cfg.SLICE_SIZE     = 32;
                cfg.SIZE           = 3;
                cfg.ANIMATION_SPEED = 1;
        
        POKEMON.SetPosition(new Point(
                    this.GetRectangle().X + (this.GetRectangle().Width  / 2) - (cfg.SLICE_SIZE * cfg.SIZE / 2),
                    this.GetRectangle().Y + (this.GetRectangle().Height / 2) - (cfg.SLICE_SIZE * cfg.SIZE / 2)));
        
        POKEMON.DIRECTION = GameDirection.BOTTOM_LEFT;

        if(POKEMON!=null) this.POKEMON_ENTITY = POKEMON; 
        GameTableElement.TABLE_ELEMENTS.Add(POKEMON);
    }

    public override void Update()
    {

        this.GetRendererConfig().COLOR = COLOR_STATE;

        if(!this.PLAYER_OWN) return;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.CONFIG.IsHover()) 
            { 
                this.RENDER_CONFIG.COLOR = Color.Black * 0.7f;
            }

            //PLACE POKEMON
            if(GameMouse.LeftPressed() && GameMouse.IsCarryElement() && PLAYER_OWN)
            {
                if(!(GameMouse.GetCarryElement() is PokemonEntity)) return;
                if(this.POKEMON_ENTITY!!=null) return;
                PokemonEntity _source = GameMouse.GetCarryElement() as PokemonEntity;
                GameMouse.ClearCarryElement();

                PokemonEntity _pokemon = new PokemonEntity(0, 0, 32, 32, true);
                _pokemon.POKEMON    = _source.POKEMON;
                _pokemon.DIRECTION  = _source.DIRECTION;
                _pokemon.IS_MOVING  = _source.IS_MOVING;

                this.POKEMON_ENTITY = _pokemon;

                GameEntityRenderConfig cfg = new GameEntityRenderConfig();
                cfg.TEXTURE_PATH   = "Pokemons/" + _pokemon.POKEMON.NAME + "/moveset";
                cfg.SLICE_SIZE     = 32;
                cfg.SIZE           = 3;
                cfg.ANIMATION_SPEED = 1;

                this.POKEMON_ENTITY.SetPosition(new Point(
                    this.GetRectangle().X + (this.GetRectangle().Width  / 2) - (cfg.SLICE_SIZE * cfg.SIZE / 2),
                    this.GetRectangle().Y + (this.GetRectangle().Height / 2) - (cfg.SLICE_SIZE * cfg.SIZE / 2)
                ));
                this.POKEMON_ENTITY.SetEntityConfig(cfg);
                this.POKEMON_ENTITY.ENEMY = false;
                GameTableElement.TABLE_ELEMENTS.Add(this.POKEMON_ENTITY);
                Console.WriteLine("PLACED");
            } else if (GameMouse.LeftPressed() && !GameMouse.IsCarryElement() && this.POKEMON_ENTITY!=null)
            {
                this.POKEMON_ENTITY.DIRECTION = GameDirection.TOP_RIGHT;
                GameMouse.SetCarryElement(PokemonEntity.Copy(this.POKEMON_ENTITY));
                TABLE_ELEMENTS.Remove(this.POKEMON_ENTITY);
                this.POKEMON_ENTITY = null;
            }

        }

    }


}
