using UnityEngine;
using UnityEngine.SceneManagement;
using Labyrinth.Items;
using Labyrinth.Enemy;
using Labyrinth.Player;
using Labyrinth.Core;
using Labyrinth.UI;
using Labyrinth.Traps;
using Labyrinth.Visibility;
using Labyrinth.Progression;
using Labyrinth.Leveling;

namespace Labyrinth.Maze
{
    public class MazeInitializer : MonoBehaviour
    {
        [SerializeField, Tooltip("Maze generation configuration asset")]
        private MazeGeneratorConfig mazeConfig;

        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private TrapSpawner trapSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private EnemySpawnerManager enemySpawnerManager;
        [SerializeField] private EnemySpawnConfig enemySpawnConfig;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private HealthDisplay healthDisplay;
        [SerializeField] private VirtualJoystick virtualJoystick;
        [SerializeField] private FogOfWarManager fogOfWarManager;

        public MazeGrid Grid { get; private set; }

        private void Start()
        {
            GenerateMaze();
        }

        private void GenerateMaze()
        {
            MazeGeneratorConfig effectiveConfig = mazeConfig;

            // Check if we have an active level from progression
            var levelManager = LevelProgressionManager.Instance;
            if (levelManager != null && levelManager.CurrentLevel != null)
            {
                var level = levelManager.CurrentLevel;
                effectiveConfig = level.CreateMazeConfig();

                // Reset player systems for fresh start
                if (PlayerLevelSystem.Instance != null)
                {
                    PlayerLevelSystem.Instance.ResetLevel();
                }
            }

            if (effectiveConfig == null)
            {
                Debug.LogError("[MazeInitializer] No MazeGeneratorConfig assigned!");
                return;
            }

            // Generate maze using config
            var generator = effectiveConfig.CreateGenerator();
            Grid = generator.Generate();
            mazeRenderer.RenderMaze(Grid);

            // Update fog of war to match maze size
            if (fogOfWarManager != null)
            {
                fogOfWarManager.SetMazeDimensions(effectiveConfig.Width, effectiveConfig.Height);
            }

            // Spawn player
            var playerObj = Instantiate(playerPrefab,
                new Vector3(mazeRenderer.StartPosition.x, mazeRenderer.StartPosition.y, 0),
                Quaternion.identity);
            playerObj.tag = "Player";

            // Spawn ObjectiveTracker if not present
            if (FindFirstObjectByType<ObjectiveTracker>() == null)
            {
                var trackerObj = new GameObject("ObjectiveTracker");
                trackerObj.AddComponent<ObjectiveTracker>();
            }

            // Set up camera
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerObj.transform);
                cameraFollow.SetBounds(effectiveConfig.Width, effectiveConfig.Height);
            }

            // Set up health display
            if (healthDisplay != null)
            {
                var playerHealth = playerObj.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    healthDisplay.SetPlayerHealth(playerHealth);
                }
            }

            // Connect joystick to player input handler
            if (virtualJoystick != null)
            {
                var inputHandler = playerObj.GetComponent<PlayerInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.SetJoystick(virtualJoystick);
                }
            }

            // Spawn items
            if (itemSpawner != null)
            {
                itemSpawner.SpawnItems(Grid, mazeRenderer.StartPosition, mazeRenderer.ExitPosition);
            }

            // Spawn traps
            if (trapSpawner != null)
            {
                trapSpawner.SpawnTraps(Grid, mazeRenderer.StartPosition, mazeRenderer.ExitPosition, effectiveConfig.GetValidatedCorridorWidth());
            }

            // Initialize enemy spawner
            if (enemySpawner != null)
            {
                enemySpawner.Initialize(Grid, mazeRenderer.StartPosition, playerObj.transform);
            }

            // Spawn enemies using manager
            if (enemySpawnerManager != null)
            {
                if (enemySpawnConfig != null)
                {
                    enemySpawnerManager.SpawnConfig = enemySpawnConfig;
                }
                enemySpawnerManager.SpawnEnemies(Grid, mazeRenderer.StartPosition, mazeRenderer.ExitPosition, playerObj.transform);
            }
        }

        public void ResetGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
