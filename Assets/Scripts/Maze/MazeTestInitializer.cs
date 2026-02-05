using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Items;
using Labyrinth.Enemy;
using Labyrinth.Progression;

namespace Labyrinth.Maze
{
    /// <summary>
    /// Simplified maze initializer for testing maze generation.
    /// Generates the maze with optional items and enemies, no fog of war or player.
    /// Supports testing with LevelDefinition or manual configuration.
    /// </summary>
    public class MazeTestInitializer : MonoBehaviour
    {
        [Header("Level Configuration (Priority)")]
        [SerializeField, Tooltip("Level definition to test. If set, overrides manual maze config.")]
        private LevelDefinition levelDefinition;

        [SerializeField, Tooltip("Available levels to choose from via buttons")]
        private List<LevelDefinition> availableLevels = new();

        [Header("Manual Maze Configuration")]
        [SerializeField, Tooltip("Maze generation configuration asset (used if no LevelDefinition)")]
        private MazeGeneratorConfig mazeConfig;

        [Header("Item Configuration")]
        [SerializeField, Tooltip("Item spawn configuration asset (optional, used alongside any maze config)")]
        private ItemSpawnConfig itemConfig;

        [SerializeField, Tooltip("Item spawner component")]
        private ItemSpawner itemSpawner;

        [Header("Enemy Configuration")]
        [SerializeField, Tooltip("Enemy spawn configuration asset (optional, used alongside any maze config)")]
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

        [Header("Level Selection UI")]
        [SerializeField, Tooltip("Container for level selection buttons")]
        private Transform levelButtonContainer;
        [SerializeField, Tooltip("Prefab for level selection button (needs Button and TMP_Text child)")]
        private GameObject levelButtonPrefab;
        [SerializeField, Tooltip("Button to clear level selection and use manual config")]
        private Button clearLevelButton;

        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI infoText;

        public MazeGrid Grid { get; private set; }

        private int _currentSeed;
        private Vector2 _startPos;
        private Vector2 _exitPos;
        private MazeGeneratorConfig _activeMazeConfig;
        private List<Button> _createdLevelButtons = new();

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

            if (clearLevelButton != null)
            {
                clearLevelButton.onClick.AddListener(ClearLevelSelection);
            }

            // Create level selection buttons
            CreateLevelButtons();

            // Setup configs based on level definition or manual config
            SetupConfigs();

            GenerateMaze();
        }

        private void SetupConfigs()
        {
            if (levelDefinition != null)
            {
                // Use LevelDefinition's maze config
                _activeMazeConfig = levelDefinition.CreateMazeConfig();
                Debug.Log($"[MazeTestInitializer] Using LevelDefinition: {levelDefinition.DisplayName}");

                // Use LevelDefinition's item config (respects XP count and general item count)
                if (itemSpawner != null)
                {
                    var levelItemConfig = levelDefinition.CreateItemSpawnConfig();
                    itemSpawner.SpawnConfig = levelItemConfig;
                    Debug.Log($"[MazeTestInitializer] Using level item config - XP: {levelItemConfig.XpItemCount}, Items: {levelItemConfig.GeneralItemCount}");
                }
            }
            else if (mazeConfig != null)
            {
                // Use manual maze config
                _activeMazeConfig = mazeConfig;
                Debug.Log("[MazeTestInitializer] Using manual MazeGeneratorConfig");

                // Use manual item config when no LevelDefinition
                if (itemSpawner != null && itemConfig != null)
                {
                    itemSpawner.SpawnConfig = itemConfig;
                }
            }

            // Setup enemy spawner config (works with both LevelDefinition and manual config)
            if (enemySpawnerManager != null && enemyConfig != null)
            {
                enemySpawnerManager.SpawnConfig = enemyConfig;
            }
        }

        private void GenerateMaze()
        {
            if (_activeMazeConfig == null)
            {
                Debug.LogError("[MazeTestInitializer] No MazeGeneratorConfig assigned!");
                return;
            }

            // Clear existing enemies before regenerating maze
            DeleteEnemies();

            // Generate a random seed (override config's seed setting for testing)
            _currentSeed = Random.Range(0, int.MaxValue);

            // Generate maze using config with random seed override
            var generator = _activeMazeConfig.CreateGenerator(_currentSeed);
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

            if (mainCamera != null && _activeMazeConfig != null)
            {
                // Center camera on maze
                mainCamera.transform.position = new Vector3(
                    _activeMazeConfig.Width / 2f,
                    _activeMazeConfig.Height / 2f,
                    -10f
                );

                // Set orthographic size to fit maze
                float verticalSize = _activeMazeConfig.Height / 2f + 1f;
                float horizontalSize = (_activeMazeConfig.Width / 2f + 1f) / mainCamera.aspect;
                mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            }
        }

        private void UpdateInfoText()
        {
            if (infoText != null && _activeMazeConfig != null)
            {
                string levelInfo = levelDefinition != null ? $"Level: {levelDefinition.DisplayName} | " : "";
                string itemInfo;
                if (levelDefinition != null)
                {
                    itemInfo = $" | XP: {levelDefinition.XpItemCount} | Items: {levelDefinition.GeneralItemCount}";
                }
                else if (itemConfig != null)
                {
                    itemInfo = $" | XP: {itemConfig.XpItemCount} | Items: {itemConfig.GeneralItemCount}";
                }
                else
                {
                    itemInfo = "";
                }
                string enemyInfo = enemyConfig != null ? $" | Guards: {enemyConfig.MaxPatrollingGuards} | Moles: {enemyConfig.MaxBlindMoles}" : "";
                infoText.text = $"{levelInfo}Size: {_activeMazeConfig.Width}x{_activeMazeConfig.Height} | Corridor: {_activeMazeConfig.GetValidatedCorridorWidth()} | Seed: {_currentSeed}{itemInfo}{enemyInfo}";
            }
        }

        private void CreateLevelButtons()
        {
            if (levelButtonContainer == null || levelButtonPrefab == null)
            {
                return;
            }

            // Clear existing buttons
            foreach (var btn in _createdLevelButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            _createdLevelButtons.Clear();

            // Create button for each available level
            foreach (var level in availableLevels)
            {
                if (level == null) continue;

                var buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
                var button = buttonObj.GetComponent<Button>();
                var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = level.DisplayName;
                }

                if (button != null)
                {
                    // Capture level in closure
                    var capturedLevel = level;
                    button.onClick.AddListener(() => SelectLevel(capturedLevel));
                    _createdLevelButtons.Add(button);
                }
            }
        }

        /// <summary>
        /// Selects a level and regenerates the maze with its configuration.
        /// </summary>
        public void SelectLevel(LevelDefinition level)
        {
            levelDefinition = level;
            SetupConfigs();
            GenerateMaze();
            Debug.Log($"[MazeTestInitializer] Selected level: {level.DisplayName}");
        }

        /// <summary>
        /// Clears level selection and uses manual maze config.
        /// </summary>
        public void ClearLevelSelection()
        {
            levelDefinition = null;
            SetupConfigs();
            GenerateMaze();
            Debug.Log("[MazeTestInitializer] Cleared level selection, using manual config");
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

            if (clearLevelButton != null)
            {
                clearLevelButton.onClick.RemoveListener(ClearLevelSelection);
            }

            // Clean up dynamically created buttons
            foreach (var btn in _createdLevelButtons)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }
            }
            _createdLevelButtons.Clear();
        }
    }
}
