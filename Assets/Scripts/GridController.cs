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
    [Tooltip("Tilemap on which the player will train.")]
    [SerializeField] private Tilemap tilemap;
    [Tooltip("Prefab object that represents visited tiles by the player.")]
    [SerializeField] private GameObject visitedTile;
    [Tooltip("Prefab object that represents where the player will start on each epoch.")]
    [SerializeField] private GameObject startTile;
    [Tooltip("Prefab object that represents the goal tile the player is looking for.")]
    [SerializeField] private GameObject GoalTile;
    [Tooltip("Prefab object that represents the player.")]
    [SerializeField] private GameObject player;
    [Tooltip("Text on cavas that shows the amount of times the player has found the goal.")]
    [SerializeField] private TMP_Text textWins;
    [Tooltip("Text on cavas that shows the current episode of the training process.")]
    [SerializeField] private TMP_Text textEpisodes;
    [Tooltip("Text on cavas that shows porcentage of times the player has reached the goal vs the number of epochs.")]
    [SerializeField] private TMP_Text textWinsPercentage;
    [Tooltip("Value that decides the frecuency at which the best move is selected (higher means best move is selected more).")]
    [SerializeField] private float epsilon = 0.9f;
    [SerializeField] private float discountFactor = 1f;
    [Tooltip("Value that represents a delay between an epoch ending and the next one starting.")]
    [SerializeField] private float delay = 0.05f;
    [Tooltip("Value that controls the rate al which the gaussian surfaces update their weights.")]
    [SerializeField] private float learningRate = 0.9f;
    [Tooltip("Number of trainning epoch before stopping the program.")]
    [SerializeField] private int episodes = 1000;
    [Tooltip("Player will start at a random location on each epoch.")]
    [SerializeField] private bool startRandomEachEpisode = true;
    // [SerializeField] private string qValuesFileName;
    // [SerializeField] private string qValuesFileExtension;

    private TileState[,] gridPosMatrix;
    private FileManager fileManager;
    private string[] actions = { "up", "right", "down", "left" };
    private bool startTraining = false;
    private float winsCount = 0f;
    private float episodesCount = 0f;
    private float winsPercentageCount = 0f;

    void Start()
    {
        // fileManager = new FileManager(qValuesFileName, qValuesFileExtension);
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
        gridPosMatrix = new TileState[bounds.size.x, bounds.size.y];

        // Loop through all tilemap positions and create red tiles and a State instance where needed.
        for (int x = bounds.x; x < bounds.x + bounds.size.x; x++)
        {
            for (int y = bounds.y; y < bounds.y + bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                TileBase tile = allTiles[x - bounds.x + (y - bounds.y) * bounds.size.x];

                gridPosMatrix[x - bounds.x, y - bounds.y] = new TileState(cellPosition.x, cellPosition.y, (tile != null) ? -1 : -100, null);
                createTile(x - bounds.x, y - bounds.y, visitedTile);
            }
        }

        int randomGoalX;
        int randomGoalY;

        // Initial setup (goal tile position, move player to goal tile, set reward as 100 at goal position).
        getStartingLocation(out randomGoalX, out randomGoalY);
        gridPosMatrix[randomGoalX, randomGoalY].Reward = 100;
        createTile(randomGoalX, randomGoalY, GoalTile);
        movePlayer(randomGoalX, randomGoalY);

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
    }

    // Start trainning.
    public void play() 
    {
        startTraining = true;
    }

    // Check if player went off the terrain or if it reached the goal.
    private bool isTerminalState(int x, int y)
    {
        return (gridPosMatrix[x, y].Reward != -1);
    }

    // Get a random starting point inside the terrain and different to the goal position.
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

    // Choose the next move the player should do. If a random value between 0 and 1 es greater than
    // epsilon then choose the best move, else choose a random move.
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

    // Get the next position of the player based on the current position plus the selected move.
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

    // Instantiate a given tile prefab on a given location.
    private GameObject createTile(int x, int y, GameObject tile) 
    {
        Vector3 pos = new Vector3(gridPosMatrix[x, y].X + 0.5f, gridPosMatrix[x, y].Y + 0.5f);
        return Instantiate(tile, pos, Quaternion.identity);
    }

    // Move the player to a given location.
    private void movePlayer(int x, int y)
    {
        Vector3 pos = new Vector3(gridPosMatrix[x, y].X + 0.5f, gridPosMatrix[x, y].Y + 0.5f);
        player.transform.position = pos;
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
                    episodesCount = i;
                    int x;
                    int y;

                    // Get random starting location and move player to it.
                    getStartingLocation(out x, out y);
                    movePlayer(x, y);

                    // Keep going until we hit a terminal state (goal or out of map).
                    while (!isTerminalState(x, y))
                    {
                        // Next action that the player should do.
                        int actionIndex = getNextAction(x, y, epsilon);
                        int oldX = x;
                        int oldY = y;
                        getNextLocation(oldX, oldY, actionIndex, out x, out y);
                        movePlayer(x, y);

                        // Reward for moving to that location.
                        int reward = gridPosMatrix[x, y].Reward;

                        // We reached the goal.
                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        // Get the new q value for the action we did.
                        float oldQValue = gridPosMatrix[oldX, oldY].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x, y].qValues.Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);

                        // Update qValue at the old position. 
                        gridPosMatrix[oldX, oldY].qValues[actionIndex] = newQValue;
                        yield return new WaitForSeconds(delay);
                    }
                    
                }
                startTraining = false;
            }
            yield return null;
        }
    }

    // Trainning process starting always on the same tile each epoch.
    IEnumerator TrainQLearningStartFix()
    {
        while (true)
        {
            // When play btton is pressed.
            if (startTraining)
            {
                int startX;
                int startY;

                // Create starting tile and move player to it.
                getStartingLocation(out startX, out startY);
                createTile(startX, startY, startTile);
                movePlayer(startX, startY);

                // Loop through all epochs.
                for (int i = 0; i < episodes; i++)
                {
                    episodesCount = i;
                    int x = startX;
                    int y = startY;

                    // Keep going until we hit a terminal state (goal or out of map).
                    while (!isTerminalState(x, y))
                    {
                        // Next action that the player should do.
                        int actionIndex = getNextAction(x, y, epsilon);
                        int oldX = x;
                        int oldY = y;
                        getNextLocation(oldX, oldY, actionIndex, out x, out y);
                        movePlayer(x, y);

                        // Reward for moving to that location.
                        int reward = gridPosMatrix[x, y].Reward;

                        // We reached the goal.
                        if (reward == 100)
                        {
                            winsCount += 1;
                            winsPercentageCount = (float)Math.Round((winsCount / episodesCount) * 100, 2);
                        }

                        // Get the new q value for the action we did.
                        float oldQValue = gridPosMatrix[oldX, oldY].qValues[actionIndex];

                        float temporalDifference = reward + (discountFactor * gridPosMatrix[x, y].qValues.Max()) - oldQValue;

                        float newQValue = oldQValue + (learningRate * temporalDifference);

                        // Update qValue at the old position.
                        gridPosMatrix[oldX, oldY].qValues[actionIndex] = newQValue;
                        yield return new WaitForSeconds(delay);
                    }


                }
                startTraining = false;
                //fileManager.writeQValuesCSV(gridPosMatrix);
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
