using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridController : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject visitedTile;
    public Vector3Int location;
    public GameObject player;

    private GridPosition[,] gridPosMatrix;
    private int[,] rewardsMatrix;
  
    private Vector3 lastPlayerPos;
    private bool startPainting = false;

    // Start is called before the first frame update
    void Start()
    {
        lastPlayerPos = player.transform.position;
        // Accede a las posiciones de los Tiles en el Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        //matrix = new int[bounds.size.x, bounds.size.y];
        gridPosMatrix = new GridPosition[bounds.size.x, bounds.size.y];

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new GridPosition(cellPosition.x, cellPosition.y, (tile != null) ? 1 : 0);
                //Instantiate(visitedTile, new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f), Quaternion.identity);
                
                if (tile != null)
                {
                    //matrix[x - bounds.x, y - bounds.y] = 1;
                    // Haz algo con el Tile en la posición cellPosition
                    Debug.Log($"Tile en la posición {cellPosition} {tile.name}");
                    //Debug.Log($"Tile en la posición ({x - bounds.x}, {(y - bounds.y) * bounds.size.x})");

                }
            }
        }
        Debug.Log("Finished!!");
        StartCoroutine(PaintAllTiles());
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
            GetTilePosition();

        if(Input.GetKeyDown(KeyCode.Space))
            startPainting = true;
    }

    void GetTilePosition() 
    {
        Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        location = tilemap.WorldToCell(mp);

        if (tilemap.GetTile(location))
        {
            Vector3 moveTo = new Vector3(location.x + 0.5f, location.y + 0.5f);
            player.transform.position = moveTo;
            Instantiate(visitedTile, moveTo, Quaternion.identity); 
            
            Debug.Log("Tile at: " + location);
        }
        else
        {
            Debug.Log("No tile at: " + location);
        }
    }

    IEnumerator PaintAllTiles()
    {
        while (true)
        {
            if (startPainting)
            {

                for (int i = 0; i < gridPosMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < gridPosMatrix.GetLength(1); j++)
                    {
                        if (gridPosMatrix[i, j].Value == 1)
                        {
                            Instantiate(visitedTile, new Vector3(gridPosMatrix[i, j].X + 0.5f, gridPosMatrix[i, j].Y + 0.5f), Quaternion.identity);
                            yield return new WaitForSeconds(0.05f);
                        }
                            
                    }
                }
                startPainting = false;
            }
            yield return null;
        }
    }
}
