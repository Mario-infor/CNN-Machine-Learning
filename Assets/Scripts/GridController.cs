using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using UnityEditor.Tilemaps;
using UnityEngine.UI;
using TMPro;
using TMPro.EditorUtilities;

public class GridController : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject visitedTile;
    [SerializeField] private GameObject startTile;
    [SerializeField] private GameObject GoalTile;
    [SerializeField] private Vector3Int location;
    [SerializeField] private GameObject player;
    [SerializeField] private TMP_Text textWins;
    [SerializeField] private TMP_Text textEpisodes;
    [SerializeField] private TMP_Text textWinsPercentage;
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [SerializeField] private float delay = 0.05f;
    [SerializeField] private float learningRate = 0.9f;
    [SerializeField] private int episodes = 1000;
    [SerializeField] private bool startRandomEachEpisode = true;
    [SerializeField] private string qValuesFileName;
    [SerializeField] private string qValuesFileExtension;

    private TileState[,] gridPosMatrix;
    private FileManager fileManager;
    private string[] actions = { "up", "right", "down", "left" };
    private bool startTraining = false;
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;

    void Start()
    {
        fileManager = new FileManager(qValuesFileName, qValuesFileExtension);
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
        gridPosMatrix = new TileState[bounds.size.x, bounds.size.y];

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new TileState(cellPosition.x, cellPosition.y, (tile != null) ? -1 : -100, null);
                GameObject tempTile = createTile(x - bounds.x, y - bounds.y, visitedTile);

            }
        }

        int randomGoalX;
        int randomGoalY;
     
        getStartingLocation(out randomGoalX, out randomGoalY);
        gridPosMatrix[randomGoalX, randomGoalY].Reward = 100;
        createTile(randomGoalX, randomGoalY, GoalTile);
        movePlayer(randomGoalX, randomGoalY);

        if(startRandomEachEpisode)
            StartCoroutine(TrainQLearningStartRandom());
        else
            StartCoroutine(TrainQLearningStartFix());
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(1))
            GetTilePosition();

        if (Input.GetKeyDown(KeyCode.Q))
            readStoredDataForQvalues();

        textWins.text = $"Wins: {winsCount}";
        textEpisodes.text = $"Episode: {episodesCount}";
        textWinsPercentage.text = $"Wins %: {winsPercentageCount} %";
    }

    public void play() 
    {
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
        }
    }

    private bool isTerminalState(int x, int y)
    {
        return (gridPosMatrix[x, y].Reward != -1);
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

    private GameObject createTile(int x, int y, GameObject tile) 
    {
        Vector3 pos = new Vector3(gridPosMatrix[x, y].X + 0.5f, gridPosMatrix[x, y].Y + 0.5f);
        return Instantiate(tile, pos, Quaternion.identity);
    }

    private void movePlayer(int x, int y)
    {
        Vector3 pos = new Vector3(gridPosMatrix[x, y].X + 0.5f, gridPosMatrix[x, y].Y + 0.5f);
        player.transform.position = pos;
    }

    IEnumerator TrainQLearningStartRandom()
    {
        while (true)
        {
            if (startTraining)
            {
                for (int i = 0; i < episodes; i++)
                {
                    episodesCount = i;
                    int x;
                    int y;
                    getStartingLocation(out x, out y);
                    movePlayer(x, y);

                    while (!isTerminalState(x, y))
                    {
                        int actionIndex = getNextAction(x, y, epsilon);

                        int oldX = x;
                        int oldY = y;

                        getNextLocation(oldX, oldY, actionIndex, out x, out y);

                        /********************************************************************/
                        //paintTile(x, y, visitedTile);
                        movePlayer(x, y);
                        /********************************************************************/

                        int reward = gridPosMatrix[x, y].Reward;
                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        float oldQValue = gridPosMatrix[oldX, oldY].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x, y].qValues.Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);
                        gridPosMatrix[oldX, oldY].qValues[actionIndex] = newQValue;
                        yield return new WaitForSeconds(delay);
                    }
                    
                }
                startTraining = false;
            }
            yield return null;
        }
    }

    IEnumerator TrainQLearningStartFix()
    {
        while (true)
        {
            if (startTraining)
            {
                int startX;
                int startY;

                getStartingLocation(out startX, out startY);
                createTile(startX, startY, startTile);
                movePlayer(startX, startY);

                for (int i = 0; i < episodes; i++)
                {
                    episodesCount = i;
                    int x = startX;
                    int y = startY;

                    while (!isTerminalState(x, y))
                    {
                        int actionIndex = getNextAction(x, y, epsilon);

                        int oldX = x;
                        int oldY = y;

                        getNextLocation(oldX, oldY, actionIndex, out x, out y);

                        /********************************************************************/
                        //paintTile(x, y, visitedTile);
                        movePlayer(x, y);
                        /********************************************************************/

                        int reward = gridPosMatrix[x, y].Reward;

                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        float oldQValue = gridPosMatrix[oldX, oldY].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x, y].qValues.Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);
                        gridPosMatrix[oldX, oldY].qValues[actionIndex] = newQValue;
                        yield return new WaitForSeconds(delay);
                    }

                }
                startTraining = false;
                fileManager.writeQValuesCSV(gridPosMatrix);
            }
            yield return null;
        }
    }

    private void readStoredDataForQvalues()
    {
        List<float[]> qvalues = fileManager.readQValuesCSV();

        int pos = 0;

        for (int i = 0; i < gridPosMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < gridPosMatrix.GetLength(1); j++)
            {
                gridPosMatrix[i, j].qValues = qvalues[pos];
                pos++;
            }
        }

        Debug.Log("Data File Read Successfully");
    }
}
