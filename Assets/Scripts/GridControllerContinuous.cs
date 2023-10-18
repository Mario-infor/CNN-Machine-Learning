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
    private float[,] actionsDiscreet = {{-1f, 0f}, {0f, 1f}, {1f, 0f}, {0f, -1f}};
    private bool startTraining = false;
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;
    private float randomGoalX;
    private float randomGoalY;
    private BoundsInt bounds;
    private TileBase[] allTiles;
    private Vector3 goalPos = new Vector3(-100, -100, 0);


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

    private bool isTerminalState(float x, float y)
    {
        Vector3Int position = tilemap.WorldToCell(new Vector3(x, y, 0));
        return !tilemap.GetTile(position) || (x == goalPos.x && y == goalPos.y);
    }

    private void getStartingLocation(out float X, out float Y)
    {
        X = UnityEngine.Random.Range(bounds.xMin, bounds.xMax);
        Y = UnityEngine.Random.Range(bounds.yMin, bounds.yMax);

        while (isTerminalState(X, Y))
        {
            X = UnityEngine.Random.Range(bounds.xMin, bounds.xMax);
            Y = UnityEngine.Random.Range(bounds.yMin, bounds.yMax);
        }
    }

    private int getNextAction(float x, float y, float epsilon)
    {
        System.Random random = new System.Random();
        if (UnityEngine.Random.Range(0f, 1f) < epsilon)
        {
            return Array.IndexOf(GetQValueList(x, y), GetQValueList(x, y).Max());
        }
        else
        {
            return UnityEngine.Random.Range(0, 4);
        }
    }

    private void getNextLocation(float x, float y, int action, out float newX, out float newY)
    {
        newX = x + actionsDiscreet[action, 0];
        newY = y + actionsDiscreet[action, 1];
    }

    private GameObject createTile(float x, float y, GameObject tile)
    {
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        return Instantiate(tile, pos, Quaternion.identity);
    }

    private void movePlayer(float x, float y)
    {
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        player.transform.position = pos;
    }

    private int GetReward()
    {
        int reward = -1;
        Vector3Int position = tilemap.WorldToCell(player.transform.position);

        if (!tilemap.GetTile(position))
            reward = -100;
        else if (position.x == goalPos.x && position.y == goalPos.y)
            reward = 100;

        return reward;
    }

    private float GetQValue(float x, float y, int actionIndex)
    {
        return gridPosMatrix[(int)x - bounds.x, (int)y - bounds.y].qValues[actionIndex];
    }

    private float[] GetQValueList(float x, float y)
    {
        return gridPosMatrix[(int)x - bounds.x, (int)y - bounds.y].qValues;
    }

    private void SetQValue(float x, float y, int actionIndex, float newQValue)
    {
        gridPosMatrix[(int)x - bounds.x, (int)y - bounds.y].qValues[actionIndex] = newQValue;
    }

    IEnumerator TrainQLearningStartFix()
    {
        while (true)
        {
            if (startTraining)
            {
                float startX;
                float startY;

                getStartingLocation(out startX, out startY);
                createTile(startX, startY, startTile);
                movePlayer(startX, startY);

                for (int i = 0; i < episodes; i++)
                {
                    episodesCount = i;
                    float x = startX;
                    float y = startY;

                    while (!isTerminalState(x, y))
                    {
                        int actionIndex = getNextAction(x, y, epsilon);

                        float oldX = x;
                        float oldY = y;

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

                        //float oldQValue = gridPosMatrix[oldX - bounds.x, oldY - bounds.y].qValues[actionIndex];
                        float oldQValue = GetQValue(oldX, oldY, actionIndex);

                        float temporalDifference = reward + (discountFactor * GetQValueList(x, y).Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);
                        //gridPosMatrix[oldX - bounds.x, oldY - bounds.y].qValues[actionIndex] = newQValue;
                        SetQValue(oldX, oldY, actionIndex, newQValue);
                        yield return new WaitForSeconds(delay);
                    }

                }
                startTraining = false;
            }
            yield return null;
        }
    }
}
