using ENGINE.MODELS;

public class PokemonEntity : GameEntity
{
    public PokemonEntity(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE) : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE) { }

    public Pokemon POKEMON;

    public static PokemonEntity Copy(PokemonEntity SOURCE)
    {

        PokemonEntity _copy = new PokemonEntity((short)SOURCE.GetPosition().X, (short)SOURCE.GetPosition().Y, 32, 32, true);
        _copy.POKEMON   = SOURCE.POKEMON;
        _copy.DIRECTION = SOURCE.DIRECTION;
        _copy.IS_MOVING = SOURCE.IS_MOVING;
        _copy.SetEntityConfig(SOURCE.GetEntityConfig());
        _copy.SetPosition(SOURCE.GetPosition());
        _copy.SIZE_X    = SOURCE.SIZE_X;
        _copy.SIZE_Y    = SOURCE.SIZE_Y;
        _copy.VISIBLE   = SOURCE.VISIBLE;
        return _copy;
    }
    

}