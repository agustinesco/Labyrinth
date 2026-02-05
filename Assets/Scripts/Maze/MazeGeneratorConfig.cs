using UnityEngine;

namespace Labyrinth.Maze
{
    /// <summary>
    /// ScriptableObject containing maze generation configuration.
    /// Create via Assets > Create > Labyrinth > Maze Generator Config
    /// </summary>
    [CreateAssetMenu(fileName = "MazeConfig", menuName = "Labyrinth/Maze Generator Config", order = 1)]
    public class MazeGeneratorConfig : ScriptableObject
    {
        [Header("Maze Dimensions")]
        [SerializeField, Min(11), Tooltip("Width of the maze in cells")]
        private int width = 51;
        
        [SerializeField, Min(11), Tooltip("Height of the maze in cells")]
        private int height = 51;

        [Header("Corridor Settings")]
        [SerializeField, Min(1), Tooltip("Width of corridors (will be made odd for symmetry)")]
        private int corridorWidth = 3;

        [Header("Generation Algorithm")]
        [SerializeField, Range(0f, 1f), Tooltip("0 = long winding corridors (DFS), 1 = maximum bifurcations (Prim's-like)")]
        private float branchingFactor = 0.3f;

        [Header("Seed")]
        [SerializeField, Tooltip("Use a fixed seed for reproducible mazes")]
        private bool useFixedSeed = false;
        
        [SerializeField, Tooltip("Fixed seed value (only used if Use Fixed Seed is enabled)")]
        private int fixedSeed = 12345;

        // Public properties
        public int Width => width;
        public int Height => height;
        public int CorridorWidth => corridorWidth;
        public float BranchingFactor => branchingFactor;
        public bool UseFixedSeed => useFixedSeed;
        public int FixedSeed => fixedSeed;

        /// <summary>
        /// Sets the maze configuration values programmatically.
        /// Used by LevelDefinition to create configs on-the-fly.
        /// </summary>
        public void SetValues(int width, int height, int corridorWidth, float branchingFactor)
        {
            this.width = width;
            this.height = height;
            this.corridorWidth = corridorWidth;
            this.branchingFactor = branchingFactor;
        }

        /// <summary>
        /// Gets a validated corridor width (clamped to valid range for maze size)
        /// </summary>
        public int GetValidatedCorridorWidth()
        {
            int maxCorridorWidth = Mathf.Min(width, height) / 4;
            return Mathf.Clamp(corridorWidth, 1, maxCorridorWidth);
        }

        /// <summary>
        /// Gets the seed to use for generation (random or fixed based on settings)
        /// </summary>
        public int? GetSeed()
        {
            return useFixedSeed ? fixedSeed : (int?)null;
        }

        /// <summary>
        /// Sets the configuration values programmatically
        /// </summary>
        public void SetValues(int mazeWidth, int mazeHeight, int corridorW, float branching)
        {
            width = mazeWidth;
            height = mazeHeight;
            corridorWidth = corridorW;
            branchingFactor = branching;
        }

        /// <summary>
        /// Creates a MazeGenerator instance using this configuration
        /// </summary>
        public MazeGenerator CreateGenerator()
        {
            return new MazeGenerator(
                width,
                height,
                seed: GetSeed(),
                corridorWidth: GetValidatedCorridorWidth(),
                branchingFactor: branchingFactor
            );
        }

        /// <summary>
        /// Creates a MazeGenerator instance with a specific seed override
        /// </summary>
        public MazeGenerator CreateGenerator(int seedOverride)
        {
            return new MazeGenerator(
                width,
                height,
                seed: seedOverride,
                corridorWidth: GetValidatedCorridorWidth(),
                branchingFactor: branchingFactor
            );
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure minimum dimensions
            width = Mathf.Max(11, width);
            height = Mathf.Max(11, height);
            corridorWidth = Mathf.Max(1, corridorWidth);
            
            // Warn if corridor width will be clamped
            int maxCorridorWidth = Mathf.Min(width, height) / 4;
            if (corridorWidth > maxCorridorWidth)
            {
                Debug.LogWarning($"[MazeConfig] Corridor width {corridorWidth} exceeds maximum {maxCorridorWidth} for maze size {width}x{height}. It will be clamped during generation.");
            }
        }
#endif
    }
}
