public class TileState
{
    public int X { get; set; }
    public int Y  { get; set; }
    public int Reward { get; set; }
    public float[] qValues { get; set; }

    public TileState(int x, int y, int reawrd) 
    {
        X = x;
        Y = y;
        Reward = reawrd;
        qValues = new float[4];
    }
}
