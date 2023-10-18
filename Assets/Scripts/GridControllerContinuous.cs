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
using UnityEngine.UIElements;

public class GridControllerContinuos : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject visitedTile;
    [SerializeField] private GameObject startTile;
    [SerializeField] private GameObject GoalTile;
    [SerializeField] private GameObject player;
    [SerializeField] private TMP_Text textWins;
    [SerializeField] private TMP_Text textEpisodes;
    [SerializeField] private TMP_Text textWinsPercentage;
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [SerializeField] private float delay = 0.05f;
    [SerializeField] private float learningRate = 0.9f;
    [SerializeField] private int episodes = 1000;

    private TileState[,] gridPosMatrix;
    private string[] actions = { "up", "right", "down", "left" };
    private bool startTraining = false;
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;
    private int randomGoalX;
    private int randomGoalY;
    private BoundsInt bounds;
    private TileBase[] allTiles;
    private Vector3Int goalPos = new Vector3Int(-100, -100, 0);

    void Start()
    {
        bounds = tilemap.cellBounds;
        allTiles = tilemap.GetTilesBlock(bounds);
        gridPosMatrix = new TileState[bounds.size.x, bounds.size.y];

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new TileState(cellPosition.x, cellPosition.y, (tile != null) ? -1 : -100, null);
                if (tile != null)
                {
                    createTile(x, y, visitedTile);
                }
            }
        }

        getStartingLocation(out randomGoalX, out randomGoalY);
        createTile(randomGoalX, randomGoalY, GoalTile);
        movePlayer(randomGoalX, randomGoalY);
        goalPos.x = randomGoalX;
        goalPos.y = randomGoalY;
        StartCoroutine(TrainQLearningStartFix());
    }

    void Update()
    {
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
        /*Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        location = tilemap.WorldToCell(mp);

        if (tilemap.GetTile(location))
        {
            Vector3 moveTo = new Vector3(location.x + 0.5f, location.y + 0.5f);
            player.transform.position = moveTo;
            Instantiate(visitedTile, moveTo, Quaternion.identity);
        }*/
    }

    private bool isTerminalState(int x, int y)
    {
        return (!tilemap.GetTile(new Vector3Int(x, y)) || (x == goalPos.x && y == goalPos.y));
    }

    private void getStartingLocation(out int X, out int Y)
    {
        X = UnityEngine.Random.Range(bounds.xMin, bounds.xMax);
        Y = UnityEngine.Random.Range(bounds.yMin, bounds.yMax);

        while (isTerminalState(X, Y))
        {
            X = UnityEngine.Random.Range(bounds.xMin, bounds.xMax);
            Y = UnityEngine.Random.Range(bounds.yMin, bounds.yMax);
        }
    }

    private int getNextAction(int x, int y, float epsilon)
    {
        System.Random random = new System.Random();
        if (UnityEngine.Random.Range(0f, 1f) < epsilon)
        {
            return Array.IndexOf(gridPosMatrix[x - bounds.x, y - bounds.y].qValues, gridPosMatrix[x - bounds.x, y - bounds.y].qValues.Max());
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
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        return Instantiate(tile, pos, Quaternion.identity);
    }

    private void movePlayer(int x, int y)
    {
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        player.transform.position = pos;
    }

    private int GetReward()
    {
        int reward = -1;

        Vector3Int position = tilemap.WorldToCell(player.transform.position);

        if (!tilemap.GetTile(position))
        {
            reward = -100;
        }
        else if (position.x == goalPos.x && position.y == goalPos.y)
        { 
            reward = 100;
        }


        return reward;
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

                        int reward = GetReward();

                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        float oldQValue = gridPosMatrix[oldX - bounds.x, oldY - bounds.y].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x - bounds.x, y - bounds.y].qValues.Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);
                        gridPosMatrix[oldX - bounds.x, oldY - bounds.y].qValues[actionIndex] = newQValue;
                        yield return new WaitForSeconds(delay);
                    }

                }
                startTraining = false;
            }
            yield return null;
        }
    }
}
