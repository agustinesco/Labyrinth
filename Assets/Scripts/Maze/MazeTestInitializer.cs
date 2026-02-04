using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Labyrinth.Maze
{
    /// <summary>
    /// Simplified maze initializer for testing maze generation.
    /// Only generates the maze with no items, enemies, fog of war, or player.
    /// </summary>
    public class MazeTestInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField, Tooltip("Maze generation configuration asset")]
        private MazeGeneratorConfig mazeConfig;

        [Header("References")]
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Button regenerateButton;

        [Header("UI Display")]
        [SerializeField] private TextMeshProUGUI infoText;

        public MazeGrid Grid { get; private set; }

        private int _currentSeed;

        private void Start()
        {
            if (regenerateButton != null)
            {
                regenerateButton.onClick.AddListener(RegenerateMaze);
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

            // Generate a random seed (override config's seed setting for testing)
            _currentSeed = Random.Range(0, int.MaxValue);

            // Generate maze using config with random seed override
            var generator = mazeConfig.CreateGenerator(_currentSeed);
            Grid = generator.Generate();

            // Render the maze
            if (mazeRenderer != null)
            {
                mazeRenderer.RenderMaze(Grid);
            }

            // Position camera to see full maze
            SetupCamera();

            // Update info display
            UpdateInfoText();
        }

        public void RegenerateMaze()
        {
            GenerateMaze();
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
                infoText.text = $"Size: {mazeConfig.Width}x{mazeConfig.Height} | Corridor: {mazeConfig.GetValidatedCorridorWidth()} | Branching: {mazeConfig.BranchingFactor:F2} | Seed: {_currentSeed}";
            }
        }

        private void OnDestroy()
        {
            if (regenerateButton != null)
            {
                regenerateButton.onClick.RemoveListener(RegenerateMaze);
            }
        }
    }
}
