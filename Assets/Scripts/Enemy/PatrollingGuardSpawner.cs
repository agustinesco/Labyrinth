using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    public class PatrollingGuardSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject patrollingGuardPrefab;
        [SerializeField] private int minCorridorLength = 15;
        [SerializeField] private float spawnChance = 0.5f;
        [SerializeField] private int maxGuards = 3;
        [SerializeField] private float bufferFromStartExit = 5f;
        [SerializeField] private float wallOffset = 0.6f; // How close to walls the guard walks

        private struct CorridorRegion
        {
            public int MinX, MaxX, MinY, MaxY;
            public bool IsHorizontal;
            public List<Vector2> WallHuggingPath;
        }

        private List<GameObject> _spawnedGuards = new List<GameObject>();

        /// <summary>
        /// Configures the spawner settings from an EnemySpawnConfig.
        /// </summary>
        public void Configure(int maxGuards, float spawnChance, int minCorridorLength, float bufferFromStartExit)
        {
            this.maxGuards = maxGuards;
            this.spawnChance = spawnChance;
            this.minCorridorLength = minCorridorLength;
            this.bufferFromStartExit = bufferFromStartExit;
        }

        public void SpawnGuards(MazeGrid grid, Vector2 startPos, Vector2 exitPos, Transform player)
        {
            ClearGuards();
            Debug.Log($"[PatrolGuard] SpawnGuards called. Grid: {grid.Width}x{grid.Height}");
            var corridors = FindValidCorridors(grid, startPos, exitPos);

            Debug.Log($"[PatrolGuard] Found {corridors.Count} valid corridors");
            if (corridors.Count == 0)
                return;

            ShuffleList(corridors);

            int guardsSpawned = 0;
            foreach (var corridor in corridors)
            {
                if (guardsSpawned >= maxGuards)
                    break;

                if (Random.value > spawnChance)
                    continue;

                SpawnGuard(grid, corridor, player);
                guardsSpawned++;
                Debug.Log($"[PatrolGuard] Spawned guard #{guardsSpawned} with {corridor.WallHuggingPath.Count} waypoints");
            }
            Debug.Log($"[PatrolGuard] Total guards spawned: {guardsSpawned}");
        }

        private List<CorridorRegion> FindValidCorridors(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            var corridors = new List<CorridorRegion>();
            var usedCells = new HashSet<Vector2Int>();

            Debug.Log($"[PatrolGuard] Scanning for rectangular corridor regions in {grid.Width}x{grid.Height} grid");

            // Find rectangular floor regions that can support patrol paths
            // We need regions that are at least 3x3 to have a rectangular patrol path
            int minWidth = 3;

            for (int startY = 0; startY < grid.Height; startY++)
            {
                for (int startX = 0; startX < grid.Width; startX++)
                {
                    if (usedCells.Contains(new Vector2Int(startX, startY)))
                        continue;

                    if (grid.GetCell(startX, startY).IsWall)
                        continue;

                    // Try to find a rectangular region starting here
                    var region = FindRectangularRegion(grid, startX, startY, usedCells);
                    if (region.HasValue)
                    {
                        var r = region.Value;
                        int width = r.MaxX - r.MinX + 1;
                        int height = r.MaxY - r.MinY + 1;

                        // Must have minimum dimensions for rectangular patrol
                        // At least minCorridorLength in one dimension and minWidth in the other
                        bool validHorizontal = width >= minCorridorLength && height >= minWidth;
                        bool validVertical = height >= minCorridorLength && width >= minWidth;

                        if (validHorizontal || validVertical)
                        {
                            if (IsValidCorridorLocation(r, startPos, exitPos))
                            {
                                r.WallHuggingPath = GenerateRectangularPath(r);
                                r.IsHorizontal = width > height;

                                if (r.WallHuggingPath != null && r.WallHuggingPath.Count == 4)
                                {
                                    corridors.Add(r);
                                    MarkCellsUsed(usedCells, r);
                                    Debug.Log($"[PatrolGuard] Found corridor region at ({r.MinX},{r.MinY}) size={width}x{height}");
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log($"[PatrolGuard] Total corridors found: {corridors.Count}");
            return corridors;
        }

        private CorridorRegion? FindRectangularRegion(MazeGrid grid, int startX, int startY, HashSet<Vector2Int> usedCells)
        {
            // Expand from start position to find the largest rectangular floor region
            int minX = startX;
            int maxX = startX;
            int minY = startY;
            int maxY = startY;

            // Expand right
            while (maxX + 1 < grid.Width && !grid.GetCell(maxX + 1, startY).IsWall && !usedCells.Contains(new Vector2Int(maxX + 1, startY)))
            {
                maxX++;
            }

            // Expand left
            while (minX - 1 >= 0 && !grid.GetCell(minX - 1, startY).IsWall && !usedCells.Contains(new Vector2Int(minX - 1, startY)))
            {
                minX--;
            }

            // Expand up - check entire row
            while (maxY + 1 < grid.Height)
            {
                bool rowClear = true;
                for (int x = minX; x <= maxX; x++)
                {
                    if (grid.GetCell(x, maxY + 1).IsWall || usedCells.Contains(new Vector2Int(x, maxY + 1)))
                    {
                        rowClear = false;
                        break;
                    }
                }
                if (rowClear)
                    maxY++;
                else
                    break;
            }

            // Expand down - check entire row
            while (minY - 1 >= 0)
            {
                bool rowClear = true;
                for (int x = minX; x <= maxX; x++)
                {
                    if (grid.GetCell(x, minY - 1).IsWall || usedCells.Contains(new Vector2Int(x, minY - 1)))
                    {
                        rowClear = false;
                        break;
                    }
                }
                if (rowClear)
                    minY--;
                else
                    break;
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Need minimum size for a patrol region
            if (width < 3 || height < 3)
                return null;

            return new CorridorRegion
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY
            };
        }

        private bool IsValidCorridorLocation(CorridorRegion region, Vector2 startPos, Vector2 exitPos)
        {
            Vector2 center = new Vector2(
                (region.MinX + region.MaxX) / 2f + 0.5f,
                (region.MinY + region.MaxY) / 2f + 0.5f
            );

            return Vector2.Distance(center, startPos) > bufferFromStartExit &&
                   Vector2.Distance(center, exitPos) > bufferFromStartExit;
        }

        private void MarkCellsUsed(HashSet<Vector2Int> usedCells, CorridorRegion region)
        {
            for (int x = region.MinX; x <= region.MaxX; x++)
            {
                for (int y = region.MinY; y <= region.MaxY; y++)
                {
                    usedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        private List<Vector2> GenerateRectangularPath(CorridorRegion region)
        {
            var waypoints = new List<Vector2>();

            // Calculate corner positions with offset from edges
            float innerMinX = region.MinX + wallOffset;
            float innerMaxX = region.MaxX + 1 - wallOffset;
            float innerMinY = region.MinY + wallOffset;
            float innerMaxY = region.MaxY + 1 - wallOffset;

            // Generate rectangular patrol path (clockwise)
            waypoints.Add(new Vector2(innerMinX, innerMinY)); // Bottom-left
            waypoints.Add(new Vector2(innerMaxX, innerMinY)); // Bottom-right
            waypoints.Add(new Vector2(innerMaxX, innerMaxY)); // Top-right
            waypoints.Add(new Vector2(innerMinX, innerMaxY)); // Top-left

            return waypoints;
        }

        private void SpawnGuard(MazeGrid grid, CorridorRegion corridor, Transform player)
        {
            if (corridor.WallHuggingPath == null || corridor.WallHuggingPath.Count < 2)
                return;

            // Spawn at first waypoint
            Vector2 spawnPos = corridor.WallHuggingPath[0];

            GameObject guardObj;
            if (patrollingGuardPrefab != null)
            {
                guardObj = Instantiate(patrollingGuardPrefab,
                    new Vector3(spawnPos.x, spawnPos.y, 0),
                    Quaternion.identity);
            }
            else
            {
                guardObj = CreateGuardDynamically(spawnPos);
            }

            _spawnedGuards.Add(guardObj);

            var controller = guardObj.GetComponent<PatrollingGuardController>();
            if (controller != null)
            {
                controller.Initialize(grid, player, corridor.WallHuggingPath);
            }
        }

        /// <summary>
        /// Clears all spawned guards.
        /// </summary>
        public void ClearGuards()
        {
            foreach (var guard in _spawnedGuards)
            {
                if (guard != null)
                {
                    Destroy(guard);
                }
            }
            _spawnedGuards.Clear();
        }

        private GameObject CreateGuardDynamically(Vector2 position)
        {
            GameObject guardObj = new GameObject("PatrollingGuard");
            guardObj.transform.position = new Vector3(position.x, position.y, 0);
            guardObj.tag = "Enemy";

            var spriteRenderer = guardObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSimpleSprite();
            spriteRenderer.color = new Color(0.8f, 0.5f, 0.2f);
            spriteRenderer.sortingOrder = 10;

            var collider = guardObj.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            guardObj.AddComponent<PatrollingGuardController>();
            guardObj.AddComponent<Labyrinth.Visibility.VisibilityAwareEntity>();

            // Add bestiary discoverable
            var discoverable = guardObj.AddComponent<Labyrinth.UI.Bestiary.BestiaryDiscoverable>();
            discoverable.SetEnemyId("patrolling_guard");

            return guardObj;
        }

        private Sprite CreateSimpleSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
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
