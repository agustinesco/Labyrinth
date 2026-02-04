using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Items;
using Labyrinth.Enemy;

namespace Labyrinth.Maze
{
    /// <summary>
    /// Simplified maze initializer for testing maze generation.
    /// Generates the maze with optional items and enemies, no fog of war or player.
    /// </summary>
    public class MazeTestInitializer : MonoBehaviour
    {
        [Header("Maze Configuration")]
        [SerializeField, Tooltip("Maze generation configuration asset")]
        private MazeGeneratorConfig mazeConfig;

        [Header("Item Configuration")]
        [SerializeField, Tooltip("Item spawn configuration asset (optional)")]
        private ItemSpawnConfig itemConfig;

        [SerializeField, Tooltip("Item spawner component")]
        private ItemSpawner itemSpawner;

        [Header("Enemy Configuration")]
        [SerializeField, Tooltip("Enemy spawn configuration asset (optional)")]
        private EnemySpawnConfig enemyConfig;

        [SerializeField, Tooltip("Enemy spawner manager component")]
        private EnemySpawnerManager enemySpawnerManager;

        [Header("References")]
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private Camera mainCamera;

        [Header("UI Buttons")]
        [SerializeField] private Button regenerateMazeButton;
        [SerializeField] private Button regenerateItemsButton;
        [SerializeField] private Button regenerateEnemiesButton;
        [SerializeField] private Button deleteEnemiesButton;

        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI infoText;

        public MazeGrid Grid { get; private set; }

        private int _currentSeed;
        private Vector2 _startPos;
        private Vector2 _exitPos;

        private void Start()
        {
            // Setup button listeners
            if (regenerateMazeButton != null)
            {
                regenerateMazeButton.onClick.AddListener(RegenerateMaze);
            }

            if (regenerateItemsButton != null)
            {
                regenerateItemsButton.onClick.AddListener(RegenerateItems);
            }

            if (regenerateEnemiesButton != null)
            {
                regenerateEnemiesButton.onClick.AddListener(RegenerateEnemies);
            }

            if (deleteEnemiesButton != null)
            {
                deleteEnemiesButton.onClick.AddListener(DeleteEnemies);
            }

            // Setup item spawner config if provided
            if (itemSpawner != null && itemConfig != null)
            {
                itemSpawner.SpawnConfig = itemConfig;
            }

            // Setup enemy spawner config if provided
            if (enemySpawnerManager != null && enemyConfig != null)
            {
                enemySpawnerManager.SpawnConfig = enemyConfig;
            }

            GenerateMaze();
        }

        private void GenerateMaze()
        {
            if (mazeConfig == null)
            {
                Debug.LogError("[MazeTestInitializer] No MazeGeneratorConfig assigned!");
                return;
            }

            // Clear existing enemies before regenerating maze
            DeleteEnemies();

            // Generate a random seed (override config's seed setting for testing)
            _currentSeed = Random.Range(0, int.MaxValue);

            // Generate maze using config with random seed override
            var generator = mazeConfig.CreateGenerator(_currentSeed);
            Grid = generator.Generate();

            // Render the maze
            if (mazeRenderer != null)
            {
                mazeRenderer.RenderMaze(Grid);

                // Store start and exit positions from renderer
                _startPos = mazeRenderer.StartPosition;
                _exitPos = mazeRenderer.ExitPosition;
            }

            // Spawn items if configured
            SpawnItems();

            // Spawn enemies
            SpawnEnemies();

            // Position camera to see full maze
            SetupCamera();

            // Update info display
            UpdateInfoText();
        }

        private void SpawnItems()
        {
            if (itemSpawner != null && itemConfig != null)
            {
                itemSpawner.SpawnItems(Grid, _startPos, _exitPos);
            }
        }

        private void SpawnEnemies()
        {
            if (enemySpawnerManager != null && enemyConfig != null)
            {
                enemySpawnerManager.SpawnEnemies(Grid, _startPos, _exitPos, null);
            }
        }

        public void RegenerateMaze()
        {
            GenerateMaze();
        }

        public void RegenerateItems()
        {
            if (itemSpawner == null)
            {
                Debug.LogWarning("[MazeTestInitializer] No ItemSpawner assigned!");
                return;
            }

            if (itemConfig == null)
            {
                Debug.LogWarning("[MazeTestInitializer] No ItemSpawnConfig assigned!");
                return;
            }

            if (Grid == null)
            {
                Debug.LogWarning("[MazeTestInitializer] No maze generated yet!");
                return;
            }

            itemSpawner.ClearSpawnedItems();
            itemSpawner.SpawnItems(Grid, _startPos, _exitPos);
            Debug.Log("[MazeTestInitializer] Items regenerated");
        }

        public void RegenerateEnemies()
        {
            if (Grid == null)
            {
                Debug.LogWarning("[MazeTestInitializer] No maze generated yet!");
                return;
            }

            if (enemySpawnerManager != null && enemyConfig != null)
            {
                enemySpawnerManager.RegenerateEnemies(Grid, _startPos, _exitPos, null);
                Debug.Log("[MazeTestInitializer] Enemies regenerated");
            }
        }

        public void DeleteEnemies()
        {
            if (enemySpawnerManager != null)
            {
                enemySpawnerManager.ClearAllEnemies();
                Debug.Log("[MazeTestInitializer] Enemies deleted");
            }
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                // Center camera on maze
                mainCamera.transform.position = new Vector3(
                    mazeConfig.Width / 2f,
                    mazeConfig.Height / 2f,
                    -10f
                );

                // Set orthographic size to fit maze
                float verticalSize = mazeConfig.Height / 2f + 1f;
                float horizontalSize = (mazeConfig.Width / 2f + 1f) / mainCamera.aspect;
                mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            }
        }

        private void UpdateInfoText()
        {
            if (infoText != null)
            {
                string itemInfo = itemConfig != null ? $" | XP: {itemConfig.XpItemCount} | Items: {itemConfig.GeneralItemCount}" : "";
                string enemyInfo = enemyConfig != null ? $" | Guards: {enemyConfig.MaxPatrollingGuards} | Moles: {enemyConfig.MaxBlindMoles}" : "";
                infoText.text = $"Size: {mazeConfig.Width}x{mazeConfig.Height} | Corridor: {mazeConfig.GetValidatedCorridorWidth()} | Seed: {_currentSeed}{itemInfo}{enemyInfo}";
            }
        }

        private void OnDestroy()
        {
            if (regenerateMazeButton != null)
            {
                regenerateMazeButton.onClick.RemoveListener(RegenerateMaze);
            }

            if (regenerateItemsButton != null)
            {
                regenerateItemsButton.onClick.RemoveListener(RegenerateItems);
            }

            if (regenerateEnemiesButton != null)
            {
                regenerateEnemiesButton.onClick.RemoveListener(RegenerateEnemies);
            }

            if (deleteEnemiesButton != null)
            {
                deleteEnemiesButton.onClick.RemoveListener(DeleteEnemies);
            }
        }
    }
}
