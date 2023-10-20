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
    [SerializeField] private GameObject visitedTile;
    [SerializeField] private GameObject startTile;
    [SerializeField] private GameObject GoalTile;
    [SerializeField] private GameObject GaussTile1;
    [SerializeField] private GameObject GaussTile2;
    [SerializeField] private GameObject GaussTile3;
    [SerializeField] private GameObject GaussTile4;
    [SerializeField] private GameObject player;
    [SerializeField] private TMP_Text textWins;
    [SerializeField] private TMP_Text textEpisodes;
    [SerializeField] private TMP_Text textWinsPercentage;
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [SerializeField] private float delay = 0.05f;
    [SerializeField] private float learningRate = 0.1f;
    [SerializeField] private int episodes = 1000;
    [SerializeField] private float sigma = 0.6f;

    private TileState[,] gridPosMatrix;
    //private float[,] actionsDiscreet = {{-1f, 0f}, {0f, 1f}, {1f, 0f}, {0f, -1f}};
    //private float[,] actionsDiscreet = { { 1f, 0f }, { 0f, -1f }, { 0f, 1f }, { -1f, 0f }};
    private float[,] actionsDiscreet = { { 0f, -1f }, { 0f, 1f }, { -1f, 0f }, { 1f, 0f } };
    private bool startTraining = false;
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;
    private float randomGoalX;
    private float randomGoalY;
    private BoundsInt bounds;
    private TileBase[] allTiles;
    private Vector3 goalPos = new Vector3(-100, -100, 0);
    private List<float[]> centers = new List<float[]>();
    
    private float alpha = 0.1f;
    private int tranningIte = 100;
    private int stepSize = 1;
    private int gaussCount;
    private GaussianSurfaceClass[] gaussArray;
    private int acumaltedReward = 0;
    private List<GameObject> centersTileList1 = new List<GameObject>();
    private List<GameObject> centersTileList2 = new List<GameObject>();
    private List<GameObject> centersTileList3 = new List<GameObject>();
    private List<GameObject> centersTileList4 = new List<GameObject>();
    private bool showGaussiansUI = true;
    void Start()
    {
        bounds = tilemap.cellBounds;
        allTiles = tilemap.GetTilesBlock(bounds);
        gridPosMatrix = new TileState[bounds.size.x, bounds.size.y];

        int xCenterFlag = 0;
        int yCenterFlag = 0;

        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            yCenterFlag = 0;
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new TileState(cellPosition.x, cellPosition.y, (tile != null) ? -1 : -100, null);
                
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
        if(showGaussiansUI) 
        {
            gaussArray = new GaussianSurfaceClass[actionsDiscreet.GetLength(0)];
            for (int i = 0; i < actionsDiscreet.GetLength(0); i++)
            {
                gaussArray[i] = new GaussianSurfaceClass(gaussCount, tranningIte);
            }

            for (int i = 0; i < gaussArray[0].WList.Length; i++)
            {
                centersTileList1.Add(createGaussCenterTile(centers[i][0], centers[i][1], gaussArray[0].WList[i], GaussTile1));
            }

            for (int i = 0; i < gaussArray[1].WList.Length; i++)
            {
                centersTileList2.Add(createGaussCenterTile(centers[i][0], centers[i][1], gaussArray[0].WList[i], GaussTile2));
            }

            for (int i = 0; i < gaussArray[2].WList.Length; i++)
            {
                centersTileList3.Add(createGaussCenterTile(centers[i][0], centers[i][1], gaussArray[0].WList[i], GaussTile3));
            }

            for (int i = 0; i < gaussArray[3].WList.Length; i++)
            {
                centersTileList4.Add(createGaussCenterTile(centers[i][0], centers[i][1], gaussArray[0].WList[i], GaussTile4));
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
            for (int i = 0; i < gaussArray[0].WList.Length; i++)
            {
                float x = centersTileList1[i].transform.position.x;
                float y = centersTileList1[i].transform.position.y;
                float z = gaussArray[0].WList[i];
                float circleSize = sigma * 2;
                centersTileList1[i].transform.position = new Vector3(x, y, z);
                centersTileList1[i].transform.GetChild(0).gameObject.transform.localScale = new Vector3(circleSize, circleSize, circleSize);
            }

            for (int i = 0; i < gaussArray[1].WList.Length; i++)
            {
                float x = centersTileList2[i].transform.position.x;
                float y = centersTileList2[i].transform.position.y;
                float z = gaussArray[1].WList[i];
                float circleSize = sigma * 2;
                centersTileList2[i].transform.position = new Vector3(x, y, z);
                centersTileList2[i].transform.GetChild(0).gameObject.transform.localScale = new Vector3(circleSize, circleSize, circleSize);
            }

            for (int i = 0; i < gaussArray[2].WList.Length; i++)
            {
                float x = centersTileList3[i].transform.position.x;
                float y = centersTileList3[i].transform.position.y;
                float z = gaussArray[2].WList[i];
                float circleSize = sigma * 2;
                centersTileList3[i].transform.position = new Vector3(x, y, z);
                centersTileList3[i].transform.GetChild(0).gameObject.transform.localScale = new Vector3(circleSize, circleSize, circleSize);
            }

            for (int i = 0; i < gaussArray[3].WList.Length; i++)
            {
                float x = centersTileList4[i].transform.position.x;
                float y = centersTileList4[i].transform.position.y;
                float z = gaussArray[3].WList[i];
                float circleSize = sigma * 2;
                centersTileList4[i].transform.position = new Vector3(x, y, z);
                centersTileList4[i].transform.GetChild(0).gameObject.transform.localScale = new Vector3(circleSize, circleSize, circleSize);
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

    /*private float GetQValue(float x, float y, int actionIndex)
    {
        return gridPosMatrix[(int)x - bounds.x, (int)y - bounds.y].qValues[actionIndex];
    }*/

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

    /*private void SetQValue(float x, float y, int actionIndex, float newQValue)
    {
        gridPosMatrix[(int)x - bounds.x, (int)y - bounds.y].qValues[actionIndex] = newQValue;
    }*/

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
                        movePlayer(x, y);
                        /********************************************************************/

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
                        //SetQValue(oldX, oldY, actionIndex, newQValue);
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
