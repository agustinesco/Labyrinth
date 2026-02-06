using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Spawns Blind Mole enemies at 4-way intersections in the maze.
    /// </summary>
    public class BlindMoleSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GameObject molePrefab;
        [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.5f;
        [SerializeField] private int maxMoles = 5;

        [Header("Exclusion Zones")]
        [SerializeField] private float startExclusionRadius = 8f;
        [SerializeField] private float exitExclusionRadius = 5f;

        private MazeGrid _grid;
        private List<BlindMoleController> _spawnedMoles = new List<BlindMoleController>();
        private Transform _container;

        /// <summary>
        /// Sets the container transform for spawned enemies.
        /// </summary>
        public void SetContainer(Transform container)
        {
            _container = container;
        }

        /// <summary>
        /// Configures the spawner settings from an EnemySpawnConfig.
        /// </summary>
        public void Configure(int maxMoles, float spawnChance, float startExclusionRadius, float exitExclusionRadius, GameObject prefab = null)
        {
            this.maxMoles = maxMoles;
            this.spawnChance = spawnChance;
            this.startExclusionRadius = startExclusionRadius;
            this.exitExclusionRadius = exitExclusionRadius;
            if (prefab != null)
                this.molePrefab = prefab;
        }

        /// <summary>
        /// Spawns moles based on the maze grid.
        /// </summary>
        public void SpawnMoles(MazeGrid grid)
        {
            _grid = grid;
            ClearExistingMoles();

            var intersections = FindFourWayIntersections();
            Debug.Log($"BlindMoleSpawner: Found {intersections.Count} 4-way intersections");

            // Shuffle intersections for randomness
            ShuffleList(intersections);

            int molesSpawned = 0;
            foreach (var pos in intersections)
            {
                if (molesSpawned >= maxMoles)
                    break;

                // Random chance to spawn
                if (Random.value > spawnChance)
                    continue;

                SpawnMoleAt(pos);
                molesSpawned++;
            }

            Debug.Log($"BlindMoleSpawner: Spawned {molesSpawned} blind moles");
        }

        private List<Vector2Int> FindFourWayIntersections()
        {
            var intersections = new List<Vector2Int>();

            if (_grid == null)
                return intersections;

            Vector2Int? startPos = null;
            Vector2Int? exitPos = null;

            // Find start and exit positions
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    var cell = _grid.GetCell(x, y);
                    if (cell.IsStart) startPos = new Vector2Int(x, y);
                    if (cell.IsExit) exitPos = new Vector2Int(x, y);
                }
            }

            // Scan for 4-way intersections
            for (int x = 1; x < _grid.Width - 1; x++)
            {
                for (int y = 1; y < _grid.Height - 1; y++)
                {
                    var cell = _grid.GetCell(x, y);

                    // Skip walls, start, exit, key room
                    if (cell.IsWall || cell.IsStart || cell.IsExit || cell.IsKeyRoom)
                        continue;

                    // Count adjacent floor tiles (cardinal directions)
                    int adjacentFloors = CountAdjacentFloors(x, y);

                    // 4-way intersection has 4 adjacent floor tiles
                    if (adjacentFloors == 4)
                    {
                        var pos = new Vector2Int(x, y);

                        // Check exclusion zones
                        if (startPos.HasValue &&
                            Vector2Int.Distance(pos, startPos.Value) < startExclusionRadius)
                            continue;

                        if (exitPos.HasValue &&
                            Vector2Int.Distance(pos, exitPos.Value) < exitExclusionRadius)
                            continue;

                        intersections.Add(pos);
                    }
                }
            }

            return intersections;
        }

        private int CountAdjacentFloors(int x, int y)
        {
            int count = 0;
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (_grid.IsInBounds(nx, ny) && !_grid.GetCell(nx, ny).IsWall)
                {
                    count++;
                }
            }

            return count;
        }

        private void SpawnMoleAt(Vector2Int gridPos)
        {
            Vector3 worldPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);

            GameObject moleObj;
            if (molePrefab != null)
            {
                moleObj = Instantiate(molePrefab, worldPos, Quaternion.identity);

                // Ensure sprite is assigned (prefab may have empty SpriteRenderer)
                var sr = moleObj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CreateMoleSprite();
                    sr.color = new Color(0.5f, 0.35f, 0.25f);
                }
            }
            else
            {
                moleObj = CreateMoleDynamically(worldPos);
            }

            if (_container != null)
                moleObj.transform.SetParent(_container);

            var moleController = moleObj.GetComponent<BlindMoleController>();
            if (moleController != null)
            {
                _spawnedMoles.Add(moleController);
            }
        }

        private GameObject CreateMoleDynamically(Vector3 position)
        {
            var moleObj = new GameObject("BlindMole");
            moleObj.transform.position = position;
            moleObj.tag = "Enemy";

            // Add sprite renderer
            var sr = moleObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateMoleSprite();
            sr.color = new Color(0.5f, 0.35f, 0.25f);
            sr.sortingOrder = 5;

            // Add collider (non-trigger for blocking player)
            var collider = moleObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = false;

            // Add rigidbody to make collision work
            var rb = moleObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            // Add controller
            var controller = moleObj.AddComponent<BlindMoleController>();

            // Add bestiary discoverable
            var discoverable = moleObj.AddComponent<Labyrinth.UI.Bestiary.BestiaryDiscoverable>();
            discoverable.SetEnemyId("blind_mole");

            return moleObj;
        }

        private Sprite CreateMoleSprite()
        {
            // Create a simple mole-like shape (oval)
            int width = 24;
            int height = 20;
            var texture = new Texture2D(width, height);
            var center = new Vector2(width / 2f, height / 2f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Ellipse equation
                    float dx = (x - center.x) / (width / 2f - 2);
                    float dy = (y - center.y) / (height / 2f - 2);
                    float dist = dx * dx + dy * dy;
                    texture.SetPixel(x, y, dist <= 1 ? Color.white : Color.clear);
                }
            }
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16);
        }

        /// <summary>
        /// Clears all spawned moles.
        /// </summary>
        public void ClearMoles()
        {
            foreach (var mole in _spawnedMoles)
            {
                if (mole != null)
                {
                    Destroy(mole.gameObject);
                }
            }
            _spawnedMoles.Clear();
        }

        private void ClearExistingMoles()
        {
            ClearMoles();
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
