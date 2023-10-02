using UnityEngine;

public class TileState
{
    public int X { get; set; }
    public int Y  { get; set; }
    public int Reward { get; set; }
    public float[] qValues { get; set; }

    public GameObject TileType { get; set; }

    public TileState(int x, int y, int reawrd, GameObject tileType) 
    {
        X = x;
        Y = y;
        Reward = reawrd;
        qValues = new float[4];
        TileType = tileType;
    }
}
