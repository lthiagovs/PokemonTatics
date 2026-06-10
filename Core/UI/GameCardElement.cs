using ENGINE.MODELS;
using GAME.CORE;

namespace GAME.UI;

public class GameCardElement : GameInterfaceElement
{

    private short SIZE_X_STATE;
    private short SIZE_Y_STATE;
    private PokemonEntity POKEMON_ENTITY = null;

    public GameCardElement(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) { 
        this.SIZE_X_STATE = SIZE_X; 
        this.SIZE_Y_STATE = SIZE_Y; 
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

        this.POKEMON_ENTITY.SetEntityConfig(cfg);
    }

    public Pokemon GetPokemon() { return this.POKEMON_ENTITY.POKEMON; }

    public override void Update()
    {

        this.SIZE_X = SIZE_X_STATE;
        this.SIZE_Y = SIZE_Y_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            if(this.CONFIG.IsHover()) 
            { 
                this.SIZE_X = (short)(this.SIZE_X * 1.05); 
                this.SIZE_Y = (short)(this.SIZE_Y * 1.05);
            }

            //DRAG
            if(GameMouse.LeftPressed() && !GameMouse.IsCarryElement())
            {
                PokemonEntity _carry = new PokemonEntity(0, 0, 32, 32, true);
                _carry.POKEMON    = this.POKEMON_ENTITY.POKEMON;
                _carry.DIRECTION  = this.POKEMON_ENTITY.DIRECTION;
                _carry.IS_MOVING  = this.POKEMON_ENTITY.IS_MOVING;
                _carry.SetEntityConfig(this.POKEMON_ENTITY.GetEntityConfig());
                _carry.SetPosition(GameMouse.GetPos());
                GameMouse.SetCarryElement(_carry);
            }

        }

    }

}