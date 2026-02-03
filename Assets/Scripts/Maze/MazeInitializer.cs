using UnityEngine;
using UnityEngine.SceneManagement;
using Labyrinth.Items;
using Labyrinth.Enemy;
using Labyrinth.Player;
using Labyrinth.Core;
using Labyrinth.UI;
using Labyrinth.Traps;

namespace Labyrinth.Maze
{
    public class MazeInitializer : MonoBehaviour
    {
        [SerializeField] private int mazeWidth = 25;
        [SerializeField] private int mazeHeight = 25;
        [SerializeField] private int corridorWidth = 3;
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private TrapSpawner trapSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private HealthDisplay healthDisplay;
        [SerializeField] private VirtualJoystick virtualJoystick;

        public MazeGrid Grid { get; private set; }

        private void Start()
        {
            GenerateMaze();
        }

        private void GenerateMaze()
        {
            // Validate and clamp corridor width
            // Max corridor width is 1/4 of the smallest maze dimension to ensure proper maze generation
            int maxCorridorWidth = Mathf.Min(mazeWidth, mazeHeight) / 4;
            int validCorridorWidth = Mathf.Clamp(corridorWidth, 1, maxCorridorWidth);

            if (validCorridorWidth != corridorWidth)
            {
                Debug.LogWarning($"Corridor width {corridorWidth} clamped to {validCorridorWidth} for maze size {mazeWidth}x{mazeHeight}");
            }

            // Generate maze with specified corridor width
            var generator = new MazeGenerator(mazeWidth, mazeHeight, corridorWidth: validCorridorWidth);
            Grid = generator.Generate();
            mazeRenderer.RenderMaze(Grid);

            // Spawn player
            var playerObj = Instantiate(playerPrefab,
                new Vector3(mazeRenderer.StartPosition.x, mazeRenderer.StartPosition.y, 0),
                Quaternion.identity);
            playerObj.tag = "Player";

            // Set up camera
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerObj.transform);
                cameraFollow.SetBounds(mazeWidth, mazeHeight);
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
                trapSpawner.SpawnTraps(Grid, mazeRenderer.StartPosition, mazeRenderer.ExitPosition, validCorridorWidth);
            }

            // Initialize enemy spawner
            if (enemySpawner != null)
            {
                enemySpawner.Initialize(Grid, mazeRenderer.StartPosition, playerObj.transform);
            }
        }

        public void ResetGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
