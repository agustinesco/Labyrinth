using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Spawns Shadow Stalker enemies at dead ends and corners of the maze.
    /// These locations are ideal because the player will often have their
    /// back turned, allowing the stalker to approach unseen.
    /// </summary>
    public class ShadowStalkerSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private GameObject stalkerPrefab;
        [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.4f;
        [SerializeField] private int maxStalkers = 3;

        [Header("Exclusion Zones")]
        [SerializeField] private float startExclusionRadius = 10f;
        [SerializeField] private float exitExclusionRadius = 8f;

        [Header("Spawn Location Preferences")]
        [SerializeField] private bool preferDeadEnds = true;
        [SerializeField] private bool preferCorners = true;
        [SerializeField] private float minDistanceBetweenStalkers = 10f;

        private MazeGrid _grid;
        private Transform _player;
        private List<ShadowStalkerController> _spawnedStalkers = new List<ShadowStalkerController>();
        private List<Vector2Int> _spawnedPositions = new List<Vector2Int>();
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
        public void Configure(int maxStalkers, float spawnChance, float startExclusionRadius, float exitExclusionRadius, GameObject prefab = null)
        {
            this.maxStalkers = maxStalkers;
            this.spawnChance = spawnChance;
            this.startExclusionRadius = startExclusionRadius;
            this.exitExclusionRadius = exitExclusionRadius;
            if (prefab != null)
                this.stalkerPrefab = prefab;
        }

        /// <summary>
        /// Spawns stalkers based on the maze grid.
        /// </summary>
        public void SpawnStalkers(MazeGrid grid, Transform player)
        {
            _grid = grid;
            _player = player;
            ClearStalkers();

            var spawnLocations = FindValidSpawnLocations();
            Debug.Log($"[ShadowStalkerSpawner] Found {spawnLocations.Count} valid spawn locations");

            // Shuffle for randomness
            ShuffleList(spawnLocations);

            int stalkersSpawned = 0;
            foreach (var pos in spawnLocations)
            {
                if (stalkersSpawned >= maxStalkers)
                    break;

                // Random chance to spawn
                if (Random.value > spawnChance)
                    continue;

                // Check minimum distance from other stalkers
                if (!IsValidDistanceFromOtherStalkers(pos))
                    continue;

                SpawnStalkerAt(pos);
                stalkersSpawned++;
            }

            Debug.Log($"[ShadowStalkerSpawner] Spawned {stalkersSpawned} shadow stalkers");
        }

        private List<Vector2Int> FindValidSpawnLocations()
        {
            var locations = new List<Vector2Int>();

            if (_grid == null)
                return locations;

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

            // Scan for valid spawn locations
            for (int x = 1; x < _grid.Width - 1; x++)
            {
                for (int y = 1; y < _grid.Height - 1; y++)
                {
                    var cell = _grid.GetCell(x, y);

                    // Skip walls, start, exit, key room
                    if (cell.IsWall || cell.IsStart || cell.IsExit || cell.IsKeyRoom)
                        continue;

                    var pos = new Vector2Int(x, y);

                    // Check exclusion zones
                    if (startPos.HasValue &&
                        Vector2Int.Distance(pos, startPos.Value) < startExclusionRadius)
                        continue;

                    if (exitPos.HasValue &&
                        Vector2Int.Distance(pos, exitPos.Value) < exitExclusionRadius)
                        continue;

                    // Check if this is a preferred location type
                    int adjacentFloors = CountAdjacentFloors(x, y);
                    bool isDeadEnd = adjacentFloors == 1;
                    bool isCorner = adjacentFloors == 2 && IsCorner(x, y);

                    // Prioritize dead ends and corners
                    if (preferDeadEnds && isDeadEnd)
                    {
                        // Dead ends are highest priority - add multiple times to weight them
                        locations.Insert(0, pos);
                    }
                    else if (preferCorners && isCorner)
                    {
                        // Corners are second priority
                        locations.Insert(locations.Count / 2, pos);
                    }
                    else if (adjacentFloors >= 2 && adjacentFloors <= 3)
                    {
                        // Regular corridor spots as fallback
                        locations.Add(pos);
                    }
                }
            }

            return locations;
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

        private bool IsCorner(int x, int y)
        {
            // Check if this is an L-shaped corner (two adjacent non-opposite floors)
            bool north = _grid.IsInBounds(x, y + 1) && !_grid.GetCell(x, y + 1).IsWall;
            bool south = _grid.IsInBounds(x, y - 1) && !_grid.GetCell(x, y - 1).IsWall;
            bool east = _grid.IsInBounds(x + 1, y) && !_grid.GetCell(x + 1, y).IsWall;
            bool west = _grid.IsInBounds(x - 1, y) && !_grid.GetCell(x - 1, y).IsWall;

            // It's a corner if we have exactly 2 adjacent floors and they're not opposite
            bool isNorthSouth = north && south && !east && !west;
            bool isEastWest = east && west && !north && !south;

            // If it's a straight corridor, it's not a corner
            return !isNorthSouth && !isEastWest;
        }

        private bool IsValidDistanceFromOtherStalkers(Vector2Int pos)
        {
            foreach (var existingPos in _spawnedPositions)
            {
                if (Vector2Int.Distance(pos, existingPos) < minDistanceBetweenStalkers)
                {
                    return false;
                }
            }
            return true;
        }

        private void SpawnStalkerAt(Vector2Int gridPos)
        {
            Vector3 worldPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);

            GameObject stalkerObj;
            if (stalkerPrefab != null)
            {
                stalkerObj = Instantiate(stalkerPrefab, worldPos, Quaternion.identity);

                // Ensure sprite is assigned (prefab may have empty SpriteRenderer)
                var sr = stalkerObj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CreateStalkerSprite();
                    sr.color = new Color(0.2f, 0.1f, 0.3f, 1f);
                }
            }
            else
            {
                stalkerObj = CreateStalkerDynamically(worldPos);
            }

            if (_container != null)
                stalkerObj.transform.SetParent(_container);
            _spawnedPositions.Add(gridPos);

            var controller = stalkerObj.GetComponent<ShadowStalkerController>();
            if (controller != null)
            {
                controller.Initialize(_grid, _player);
                _spawnedStalkers.Add(controller);
            }
        }

        private GameObject CreateStalkerDynamically(Vector3 position)
        {
            var stalkerObj = new GameObject("ShadowStalker");
            stalkerObj.transform.position = position;
            stalkerObj.tag = "Enemy";

            // Add sprite renderer - dark, shadowy appearance
            var sr = stalkerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateStalkerSprite();
            sr.color = new Color(0.2f, 0.1f, 0.3f, 1f);
            sr.sortingOrder = 5;

            // Add collider (trigger for damage)
            var collider = stalkerObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true;

            // Add rigidbody
            var rb = stalkerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;

            // Add controller
            stalkerObj.AddComponent<ShadowStalkerController>();

            // Add visibility aware entity
            stalkerObj.AddComponent<Labyrinth.Visibility.VisibilityAwareEntity>();

            // Add bestiary discoverable
            var discoverable = stalkerObj.AddComponent<Labyrinth.UI.Bestiary.BestiaryDiscoverable>();
            discoverable.SetEnemyId("shadow_stalker");

            return stalkerObj;
        }

        private Sprite CreateStalkerSprite()
        {
            // Create a shadowy, ghostly shape
            int size = 32;
            var texture = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // Create a slightly irregular circle with wispy edges
                    float dx = (x - center.x) / (size / 2f - 4);
                    float dy = (y - center.y) / (size / 2f - 4);
                    float dist = dx * dx + dy * dy;

                    // Add some noise for a more organic look
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.3f;
                    float threshold = 1f + noise;

                    if (dist <= threshold)
                    {
                        // Fade edges for ghostly appearance
                        float alpha = Mathf.Clamp01(1f - (dist / threshold) * 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        /// <summary>
        /// Clears all spawned stalkers.
        /// </summary>
        public void ClearStalkers()
        {
            foreach (var stalker in _spawnedStalkers)
            {
                if (stalker != null)
                {
                    Destroy(stalker.gameObject);
                }
            }
            _spawnedStalkers.Clear();
            _spawnedPositions.Clear();
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
