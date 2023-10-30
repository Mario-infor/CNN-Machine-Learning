using UnityEngine;

/*
 * Auxiliary class which goes on every useful position of the tilemap and stores the qValues 
 * for all four moves, and the reward of the corresponding cell.
 */

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
