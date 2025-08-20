using System;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;

    private GameObject playerInstance = null;

    private GameState currentState;
    [SerializeField] TextMeshProUGUI gameStateText;
    public LevelGeneratorLogic levelGenerator;



    public enum GameState //simple state machine to control game flow
    {
        Init, // we have to start somewhere
        GeneratingLevel,
        SpawningPlayer,
        SpawningEnemies,
        Playing,
        PlayerDied,
        LevelComplete
    }

    void Start()
    {
        TransitionToState(GameState.Init);
    }

    private void OnEnable()
    {
        if (levelGenerator == null) return;

        // Subscribe to each event
        levelGenerator.OnLevelGenerated += HandleLevelGenerated;
        PlayerHealthLogic.OnPlayerDeath += HandlePlayerDeath;
        levelGenerator.OnAllEnemiesKilled += HandleAllEnemiesKilled;
        levelGenerator.LevelCleared += HandleLevelCleared;
    }

    private void OnDisable()
    {
        if (levelGenerator == null) return;

        // Unsubscribe from each event
        levelGenerator.OnLevelGenerated -= HandleLevelGenerated;
        PlayerHealthLogic.OnPlayerDeath -= HandlePlayerDeath;
        levelGenerator.OnAllEnemiesKilled -= HandleAllEnemiesKilled;
        levelGenerator.LevelCleared -= HandleLevelCleared;
    }

// Event handlers:
  
    private void HandleLevelGenerated()
    {
        Debug.Log("Level generation complete. Spawning player...");

        if (spawnPoint != null && levelGenerator != null)
        {
            spawnPoint.position = levelGenerator.GetFirstRoomCenter();

            // Move existing player if already spawned
            if (playerInstance != null)
            {
                playerInstance.transform.position = spawnPoint.position;

                // Optional: reset camera follow
                CameraFollowLogic camFollow = Camera.main.GetComponent<CameraFollowLogic>();
                if (camFollow != null)
                {
                    camFollow.SetTarget(playerInstance.transform);
                }
            }
        }

        TransitionToState(GameState.SpawningPlayer);
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player has died.");
        TransitionToState(GameState.PlayerDied);
    }

    private void HandleAllEnemiesKilled()
    {
        Debug.Log("All enemies have been killed.");
        TransitionToState(GameState.LevelComplete);
    }

    private void HandleLevelCleared()
    {
        Debug.Log("Level has been cleared.");
        TransitionToState(GameState.Init);
    }

    void TransitionToState(GameState newState)
    {
        currentState = newState;
        gameStateText.text = currentState.ToString();

        switch (newState)
        {
            case GameState.Init:
                TransitionToState(GameState.GeneratingLevel);
                break;

            case GameState.GeneratingLevel:
                levelGenerator.GenerateLevel();
                break;

            case GameState.SpawningPlayer:
                if (!playerInstance) { SpawnPlayer(); } 
                TransitionToState(GameState.SpawningEnemies);
                break;

            case GameState.SpawningEnemies:
                levelGenerator.SpawnEnemiesInRooms();
                TransitionToState(GameState.Playing);
                break;

            case GameState.Playing:
                // Game is running
                break;

            case GameState.PlayerDied:
                levelGenerator.ResetLevelDifficulty();
                levelGenerator.ClearLevel();
                break;

            case GameState.LevelComplete:
                levelGenerator.IncreaseLevelDifficulty();
                levelGenerator.ClearLevel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab != null && spawnPoint != null)
        {
            playerInstance = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

            CameraFollowLogic camFollow = Camera.main.GetComponent<CameraFollowLogic>();
            if (camFollow != null)
            {
                camFollow.SetTarget(playerInstance.transform);
            }
            else
            {
                Debug.LogWarning("CameraFollowLogic script not found on Main Camera.");
            }
        }
        else
        {
            Debug.LogError("No player prefab or spawn point");
        }
    }
}