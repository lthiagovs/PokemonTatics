using ENGINE.MODELS;

public class PokemonEntity : GameEntity
{
    public PokemonEntity(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE) : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE) { }

    public Pokemon POKEMON;
    

}