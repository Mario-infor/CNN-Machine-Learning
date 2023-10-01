using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using UnityEditor.Tilemaps;

public class GridController : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject visitedTile;
    [SerializeField] private GameObject startTile;
    [SerializeField] private GameObject GoalTile;
    [SerializeField] private Vector3Int location;
    [SerializeField] private GameObject player;
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [SerializeField] private float speed = 0.05f;
    [SerializeField] private float learningRate = 0.9f;
    [SerializeField] private int episodes = 1000;

    private TileState[,] gridPosMatrix;
    private string[] actions = { "up", "right", "down", "left" };
    private bool startPainting = false;
    private bool startTraining = false;

    // Start is called before the first frame update
    void Start()
    {
        // Accede a las posiciones de los Tiles en el Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        //matrix = new int[bounds.size.x, bounds.size.y];
        gridPosMatrix = new TileState[bounds.size.x, bounds.size.y];

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new TileState(cellPosition.x, cellPosition.y, (tile != null) ? -1 : -100);

                if (tile != null)
                {
                    Debug.Log($"Tile en la posición {cellPosition} {tile.name}");
                }
            }
        }

        int randomGoalX;
        int randomGoalY;
     
        getStartingLocation(out randomGoalX, out randomGoalY);
        gridPosMatrix[randomGoalX, randomGoalY].Reward = 100;
        paintTile(gridPosMatrix[randomGoalX, randomGoalY].X, gridPosMatrix[randomGoalX, randomGoalY].Y, GoalTile);
        
        StartCoroutine(TrainQLearning());
        StartCoroutine(PaintAllTiles());
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
            GetTilePosition();

        /*if(Input.GetKeyDown(KeyCode.Space))
            startPainting = true;*/

        if (Input.GetKeyDown(KeyCode.Space))
            startTraining = true;
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

    private bool isTerminalState(int x, int y)
    {
        return (gridPosMatrix[x, y].Reward == -100);
    }

    private void getStartingLocation(out int X, out int Y)
    {
        X = UnityEngine.Random.Range(0, gridPosMatrix.GetLength(0));
        Y = UnityEngine.Random.Range(0, gridPosMatrix.GetLength(1));

        while (isTerminalState(X, Y))
        {
            X = UnityEngine.Random.Range(0, gridPosMatrix.GetLength(0));
            Y = UnityEngine.Random.Range(0, gridPosMatrix.GetLength(1));
        }
    }

    private int getNextAction(int x, int y, float epsilon)
    {
        System.Random random = new System.Random();
        if (UnityEngine.Random.Range(0f, 1f) < epsilon)
        {
            return Array.IndexOf(gridPosMatrix[x, y].qValues, gridPosMatrix[x, y].qValues.Max());
        }
        else 
        {
            return UnityEngine.Random.Range(0, 4);
        }
    }

    private void getNextLocation(int x, int y, int action, out int newX, out int newY)
    {
        newX = x; 
        newY = y;

        if (actions[action] == "up")
        {
            newX -= 1;
        }
        else if (actions[action] == "down")
        {
            newX += 1;
        }
        else if (actions[action] == "right")
        {
            newY += 1;
        }
        else if (actions[action] == "left")
        {
            newY -= 1;
        }
    }

    private void paintTile(int x, int y, GameObject tile) 
    {
        Vector3 pos = new Vector3(gridPosMatrix[x, y].X + 0.5f, gridPosMatrix[x, y].Y + 0.5f);
        Instantiate(tile, pos, Quaternion.identity);
    }

    IEnumerator TrainQLearning()
    {
        while (true)
        {
            if (startTraining)
            {

                for (int i = 0; i < episodes; i++)
                {
                    int x;
                    int y;

                    getStartingLocation(out x, out y);
                    paintTile(gridPosMatrix[x, y].X, gridPosMatrix[x, y].Y, startTile);

                    while (!isTerminalState(x, y))
                    {
                        int actionIndex = getNextAction(x, y, epsilon);

                        int oldX = x;
                        int oldY = y;

                        getNextLocation(oldX, oldY, actionIndex, out x, out y);

                        /********************************************************************/
                        paintTile(gridPosMatrix[x, y].X, gridPosMatrix[x, y].Y, visitedTile);
                        /********************************************************************/

                        int reward = gridPosMatrix[x, y].Reward;
                        float oldQValue = gridPosMatrix[oldX, oldY].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x, y].qValues.Max() - oldQValue);

                        float newQValue = oldQValue + (learningRate * temporalDifference);
                        gridPosMatrix[oldX, oldY].qValues[actionIndex] = newQValue;
                    }
                    yield return new WaitForSeconds(speed);
                }
                startPainting = startTraining;
            }
            yield return null;
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
                        if (gridPosMatrix[i, j].Reward == 1)
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
