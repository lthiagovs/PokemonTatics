using ENGINE.MODELS;
using GAME.CORE;
using Microsoft.Xna.Framework;

namespace GAME.UI;

public class GameCardElement : GameInterfaceElement
{

    private PokemonEntity POKEMON_ENTITY = null;

    private double _hoverElapsed = 0;
    private const double HOVER_DELAY = 1;

    GameHint HINT = null;

    public GameCardElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { 
        this.COLOR_STATE = this.GetRendererConfig().COLOR;

    }

    public void SetPokemon(Pokemon POKEMON)
    {
        this.POKEMON_ENTITY = new PokemonEntity(0, 0, 32, 32, true);
        this.POKEMON_ENTITY.POKEMON = POKEMON;

        GameEntityRenderConfig cfg = new GameEntityRenderConfig();
        cfg.TEXTURE_PATH = "Pokemons/"+POKEMON.NAME+"/moveset";
        cfg.SLICE_SIZE = 32;
        cfg.SIZE = 3;
        cfg.ANIMATION_SPEED = 1;
        this.HINT = new GameHint(POKEMON.BuildPokemonHint());
        this.HINT.VISIBLE = false;
        this.POKEMON_ENTITY.SetEntityConfig(cfg);
    }

    public Pokemon GetPokemon() { return this.POKEMON_ENTITY.POKEMON; }

    public override void Update()
    {

        this.GetRendererConfig().COLOR = COLOR_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.CONFIG.IsHover()) { 
                this.RENDER_CONFIG.COLOR = Color.White * 0.5f; 
                _hoverElapsed += GameTimeLogic.DELTA;
                if(_hoverElapsed >= HOVER_DELAY)
                {
                    this.HINT.VISIBLE = true;
                    if(!GameMouse.IsCarryElement()) GameMouse.SetCarryElement(HINT);
                }

            }
            
            //DRAG
            if(GameMouse.LeftPressed() && !GameMouse.IsCarryElement())
            {

                if(POKEMON_ENTITY.POKEMON.COST > GameGlobals.PLAYER_MANA) return;

                if(GameTableLogic.GetPlayerPokemon(this.POKEMON_ENTITY.POKEMON.NAME)!=null)
                {
                        GameTableLogic.GetPlayerPokemon(this.POKEMON_ENTITY.POKEMON.NAME).POKEMON.LevelUp();
                        GameGlobals.ChangeMana(POKEMON_ENTITY.POKEMON.COST*-1);
                        return;
                }

                if(GameGlobals.GAME_STARTED) return;

                //TABLE LIMIT
                if (GameTableLogic.GetPlayerPokemonsCount() >= GameGlobals.GetTableSize()) return;

                GameGlobals.ChangeMana(POKEMON_ENTITY.POKEMON.COST*-1);

                PokemonEntity _carry = new PokemonEntity(0, 0, 32, 32, true);
                _carry.POKEMON    = this.POKEMON_ENTITY.POKEMON;
                _carry.DIRECTION  = this.POKEMON_ENTITY.DIRECTION;
                _carry.IS_MOVING  = this.POKEMON_ENTITY.IS_MOVING;
                _carry.SetEntityConfig(this.POKEMON_ENTITY.GetEntityConfig());
                _carry.SetPosition(GameMouse.GetPos());
                GameMouse.SetCarryElement(_carry);
            }

        }
        else
        {

            if(GameMouse.GetCarryElement() == HINT) 
            {
                GameMouse.ClearCarryElement();
                _hoverElapsed = 0;
                this.HINT.Hide();
            }
                    
        }

    }

}