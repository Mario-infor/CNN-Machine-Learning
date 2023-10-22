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
using System.Reflection;

public class GridControllerContinuos : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject visitedTile;
    [SerializeField] private GameObject startTile;
    [SerializeField] private GameObject GoalTile;
    [SerializeField] private List<GameObject> GaussTile;
    [SerializeField] private TMP_Text textWins;
    [SerializeField] private TMP_Text textEpisodes;
    [SerializeField] private TMP_Text textWinsPercentage;
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [SerializeField] private float delay = 0.05f;
    [SerializeField] private float learningRate = 0.1f;
    [SerializeField] private float sigma = 0.6f;
    [SerializeField] private int episodes = 1000;
    [SerializeField] private bool showGaussiansUI = true;

    private TileBase[] allTiles;
    private GaussianSurfaceClass[] gaussArray;
    private List<List<GameObject>> listCentersTileList = new List<List<GameObject>>();
    private BoundsInt bounds;
    private Vector3 goalPos = new Vector3(-100, -100, 0);
    private List<float[]> centers = new List<float[]>();
    private float[,] actionsDiscreet = { { 0f, -1f }, { 0f, 1f }, { -1f, 0f }, { 1f, 0f } };
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;
    private float randomGoalX;
    private float randomGoalY;
    private float alpha = 0.1f;
    private int tranningIte = 100;
    private int stepSize = 1;
    private int gaussCount;
    private int acumaltedReward = 0;
    private bool startTraining = false;
    void Start()
    {
        bounds = tilemap.cellBounds;
        allTiles = tilemap.GetTilesBlock(bounds);

        int xCenterFlag = 0;
        int yCenterFlag = 0;

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            yCenterFlag = 0;
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                if (tile != null)
                    createTile(x, y, visitedTile);
                
                if (yCenterFlag % stepSize == 0 && xCenterFlag % stepSize == 0)
                {
                    float[] temp = {x, y};
                    centers.Add(temp);
                }
                
                yCenterFlag++;
            }

            xCenterFlag++;
        }

        gaussCount = centers.Count;
        gaussArray = new GaussianSurfaceClass[actionsDiscreet.GetLength(0)];

        for (int i = 0; i < actionsDiscreet.GetLength(0); i++)
        {
            gaussArray[i] = new GaussianSurfaceClass(gaussCount, tranningIte);
            listCentersTileList.Add(new List<GameObject>());
        }

        if (showGaussiansUI) 
        {
            for (int i = 0; i < listCentersTileList.Count; i++)
            {
                for (int j = 0; j < centers.Count; j++)
                {
                    listCentersTileList[i].Add(createGaussCenterTile(centers[j][0], centers[j][1], gaussArray[i].WList[j], GaussTile[i]));
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

        if (showGaussiansUI)
        {
            for (int i = 0; i < gaussArray.Length; i++)
            {
                for (int j = 0; j < gaussArray[i].WList.Length; j++) 
                {
                    float x = listCentersTileList[i][j].transform.position.x;
                    float y = listCentersTileList[i][j].transform.position.y;
                    float z = gaussArray[i].WList[j];
                    listCentersTileList[i][j].transform.position = new Vector3(x, y, z);
                }
                    
            }
        }
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
        int index = -1;
        if (UnityEngine.Random.Range(0f, 1f) < epsilon)
        {
            float best = -100000;
            for (int i = 0; i < gaussArray.Length; i++)
            {
                float eval = gaussArray[i].calculateH(x, y, sigma, centers);
                if (eval > best)
                {
                    best = eval;
                    index = i;
                }
            }
            if (index == -1) 
            {
                index = UnityEngine.Random.Range(0, 4);
            }
        }
        else
        {
            index = UnityEngine.Random.Range(0, 4);
        }
        return index;
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

    private GameObject createGaussCenterTile(float x, float y, float z, GameObject tile)
    {
        Vector3 pos = new Vector3(x, y, z);
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
        return gaussArray[actionIndex].calculateH(x, y, sigma, centers);
    }

    private float GetQValueMax(float x, float y)
    {
        float best = -100f;
        for (int i = 0; i < gaussArray.Length; i++)
        {
            float eval = gaussArray[i].calculateH(x, y, sigma, centers);
            if (eval > best)
            {
                best = eval;
            }
        }
        return best;
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
                        movePlayer(x, y);

                        int reward = GetReward();

                        acumaltedReward += reward;

                        Debug.Log(acumaltedReward);

                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        float oldQValue = GetQValue(oldX, oldY, actionIndex);

                        float temporalDifference = reward + (discountFactor * GetQValueMax(x, y)) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);

                        gaussArray[actionIndex].trainGaussSurface(oldX, oldY, alpha, sigma, centers, newQValue);

                        yield return new WaitForSeconds(delay);
                    }

                }
                startTraining = false;
            }
            yield return null;
        }
    }
}
