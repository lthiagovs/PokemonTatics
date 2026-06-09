using Microsoft.Xna.Framework;

namespace GAME.CORE;
public class GameElement
{

    public GamePoint POSITION;
    public short SIZE_X;
    public short SIZE_Y;
    public bool VISIBLE;

    public GameElement(GamePoint POSITION, short SIZE_X, short SIZE_Y, bool VISIBLE)
    {
        this.POSITION = POSITION;
        this.SIZE_X = SIZE_X;
        this.SIZE_Y = SIZE_Y;
        this.VISIBLE = VISIBLE;
    }

    public short[] GetScale()
    {
        return [this.SIZE_X, this.SIZE_X];
    }

    public GamePoint GetPos()
    {
        return this.POSITION;
    }

    public short[] Resize(short NEW_SIZE_X, short NEW_SIZE_Y)
    {
        this.SIZE_X = NEW_SIZE_X;
        this.SIZE_Y = NEW_SIZE_Y;

        return this.GetScale();
    }

    public short[] Scale(short SCALE)
    {
        this.SIZE_X*=SCALE;
        this.SIZE_Y*=SCALE;

        return this.GetScale();
    }

    public GamePoint Move(GamePoint NEW_POSITION)
    {
        this.POSITION.POS_X = NEW_POSITION.POS_X;
        this.POSITION.POS_Y = NEW_POSITION.POS_Y;

        return NEW_POSITION;
    }

    public GamePoint Move(short MOVE_X = 0, short MOVE_Y = 0)
    {
        this.POSITION.POS_X+=MOVE_X;
        this.POSITION.POS_Y+=MOVE_Y;

        return this.POSITION;
    }

    protected Point PositionAsPoint()
    {
        return new Point(this.POSITION.POS_X, this.POSITION.POS_Y);
    }

    protected Point SizeAsPoint()
    {
        return new Point(this.SIZE_X, this.SIZE_Y);
    }

    public Rectangle GetRectangle()
    {
        return new Rectangle(this.PositionAsPoint(), this.SizeAsPoint());
    } 



}