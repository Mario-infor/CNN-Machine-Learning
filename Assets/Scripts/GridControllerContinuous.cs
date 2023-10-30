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
    [SerializeField] private GameObject GaussTile;
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
    [SerializeField] private bool startRandomEachEpisode = true;

    private TileBase[] allTiles;
    private GaussianSurfaceClass[] gaussArray;
    private List<List<GameObject>> listCentersTileList = new List<List<GameObject>>();
    private BoundsInt bounds;
    private Vector3 goalPos = new Vector3(-100, -100, 0);
    private List<float[]> centers = new List<float[]>();
    //private float[,] actionsDiscreet = { { 0f, -1f }, { 0f, 1f }, { -1f, 0f }, { 1f, 0f } };
    private float[,] actionsDiscreet;
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

        actionsDiscreet = FillMovements();

        int xCenterFlag = 0;
        int yCenterFlag = 0;

        // Loop through all tilemap positions and create red tiles and gaussian centers where needed.
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
                    float[] temp = { x, y };
                    centers.Add(temp);
                }

                yCenterFlag++;
            }

            xCenterFlag++;
        }


        gaussCount = centers.Count;
        gaussArray = new GaussianSurfaceClass[actionsDiscreet.GetLength(0)];

        // Initialize guassian surfaces and its corresponding centers list.
        for (int i = 0; i < actionsDiscreet.GetLength(0); i++)
        {
            gaussArray[i] = new GaussianSurfaceClass(gaussCount, tranningIte);
            listCentersTileList.Add(new List<GameObject>());
        }

        // Check if gaussian centers shuold be display at runtime or not.
        if (showGaussiansUI)
        {
            for (int i = 0; i < listCentersTileList.Count; i++)
            {
                for (int j = 0; j < centers.Count; j++)
                {
                    listCentersTileList[i].Add(createGaussCenterCube(centers[j][0], centers[j][1], gaussArray[i].WList[j], GaussTile));
                }
            }
        }

        // Initial setup (goal tile position, move player to goal tile).
        getStartingLocation(out randomGoalX, out randomGoalY);
        createTile(randomGoalX, randomGoalY, GoalTile);
        movePlayer(randomGoalX, randomGoalY);
        goalPos.x = randomGoalX;
        goalPos.y = randomGoalY;

        // Check if player should start on the same tile every epoch or if it should be random positions.
        if (startRandomEachEpisode)
            StartCoroutine(TrainQLearningStartRandom());
        else
            StartCoroutine(TrainQLearningStartFix());
    }

    void Update()
    {
        // Update info on canvas.
        textWins.text = $"Wins: {winsCount}";
        textEpisodes.text = $"Episode: {episodesCount}";
        textWinsPercentage.text = $"Wins %: {winsPercentageCount} %";

        // Show representation of gaussian surfaces.
        if (showGaussiansUI)
        {
            for (int i = 0; i < listCentersTileList.Count; i++)
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

    // All posible movements the character can do.
    private float[,] FillMovements()
    {
        float[,] actionsDiscreet =
        {
            { 0f, -1f },
            { 0f, 1f },
            { -1f, 0f },
            { 1f, 0f },

            { -1f, -1f },
            { -1f, 1f },
            { 1f, -1f },
            { 1f, 1f },

            { 0f, -2f },
            { 0f, 2f },
            { -2f, 0f },
            { 2f, 0f },

            { -2f, -2f },
            { -2f, 2f },
            { 2f, -2f },
            { 2f, 2f }
        };
        return actionsDiscreet;
    }

    // Start trainning.
    public void play()
    {
        startTraining = true;
    }

    // Check if player went off the terrain or if it reached the goal.
    private bool isTerminalState(float x, float y)
    {
        Vector3Int position = tilemap.WorldToCell(new Vector3(x, y, 0));
        return !tilemap.GetTile(position) || (x == goalPos.x && y == goalPos.y);
    }

    // Get a random starting point inside the terrain and different to the goal position.
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

    // Choose the next move the player should do. If a random value between 0 and 1 es greater than
    // epsilon then choose the best move, else choose a random move.
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

    // Get the next position of the player based on the current position plus the selected move.
    private void getNextLocation(float x, float y, int action, out float newX, out float newY)
    {
        newX = x + actionsDiscreet[action, 0];
        newY = y + actionsDiscreet[action, 1];
    }

    // Instantiate a given tile prefab on a given location.
    private GameObject createTile(float x, float y, GameObject tile)
    {
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        return Instantiate(tile, pos, Quaternion.identity);
    }

    // Instatiate a gaussian center to be display on runtime.
    private GameObject createGaussCenterCube(float x, float y, float z, GameObject cube)
    {
        Vector3 pos = new Vector3(x, y, z);
        return Instantiate(cube, pos, Quaternion.identity);
    }

    // Move the player to a given location.
    private void movePlayer(float x, float y)
    {
        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f);
        player.transform.position = pos;
    }

    // Check the position of the player and get the corresponding reward.
    // Outside the map get -100.
    // On goal tile get +100.
    // Any other case get -1.
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

    // Calculate the value of evaluating a coordenate in a gaussian surface.
    private float GetQValue(float x, float y, int actionIndex)
    {
        return gaussArray[actionIndex].calculateH(x, y, sigma, centers);
    }

    // Get the best evaluation of a coordenate in all gaussian surfaces.
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

    // Trainning process starting always on the same tile each epoch.
    IEnumerator TrainQLearningStartFix()
    {
        while (true)
        {
            // When play btton is pressed.
            if (startTraining)
            {
                float startX;
                float startY;

                // Create starting tile and move player to it.
                getStartingLocation(out startX, out startY);
                createTile(startX, startY, startTile);
                movePlayer(startX, startY);

                // Loop through all epochs.
                for (int i = 0; i < episodes; i++)
                {
                    episodesCount = i;
                    float x = startX;
                    float y = startY;

                    // Keep going until we hit a terminal state (goal or out of map).
                    while (!isTerminalState(x, y))
                    {
                        // Next action that the player should do.
                        int actionIndex = getNextAction(x, y, epsilon);

                        float oldX = x;
                        float oldY = y;

                        getNextLocation(oldX, oldY, actionIndex, out x, out y);
                        movePlayer(x, y);

                        // Reward for moving to that location.
                        int reward = GetReward();

                        acumaltedReward += reward;

                        Debug.Log(acumaltedReward);

                        // We reached the goal.
                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        // Get the new q value for the action we did.
                        float oldQValue = GetQValue(oldX, oldY, actionIndex);

                        float temporalDifference = reward + (discountFactor * GetQValueMax(x, y)) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);

                        // Train the gaussian surface that corresponds to the action we did. 
                        gaussArray[actionIndex].trainGaussSurface(oldX, oldY, alpha, sigma, centers, newQValue);

                        yield return new WaitForSeconds(delay);
                    }

                }
                startTraining = false;
            }
            yield return null;
        }
    }

    // Trainning process starting on random tiles each epoch.
    IEnumerator TrainQLearningStartRandom()
    {
        while (true)
        {
            // When play btton is pressed.
            if (startTraining)
            {
                // Loop through all epochs.
                for (int i = 0; i < episodes; i++)
                {
                    float x;
                    float y;

                    // Get random starting location and move player to it.
                    getStartingLocation(out x, out y);
                    movePlayer(x, y);

                    episodesCount = i;

                    // Keep going until we hit a terminal state (goal or out of map).
                    while (!isTerminalState(x, y))
                    {
                        // Next action that the player should do.
                        int actionIndex = getNextAction(x, y, epsilon);

                        float oldX = x;
                        float oldY = y;

                        getNextLocation(oldX, oldY, actionIndex, out x, out y);
                        movePlayer(x, y);

                        // Reward for moving to that location.
                        int reward = GetReward();

                        acumaltedReward += reward;

                        Debug.Log(acumaltedReward);

                        // We reached the goal.
                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        // Get the new q value for the action we did.
                        float oldQValue = GetQValue(oldX, oldY, actionIndex);

                        float temporalDifference = reward + (discountFactor * GetQValueMax(x, y)) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);

                        // Train the gaussian surface that corresponds to the action we did. 
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
