using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField, Tooltip("Item spawn configuration asset")]
        private ItemSpawnConfig spawnConfig;

        [Header("Containers")]
        [SerializeField, Tooltip("Parent transform for spawned items (optional)")]
        private Transform itemContainer;
        [SerializeField, Tooltip("Parent transform for spawned XP pickups (optional)")]
        private Transform xpContainer;

        private List<GameObject> _spawnedItems = new List<GameObject>();

        /// <summary>
        /// Gets or sets the spawn configuration.
        /// </summary>
        public ItemSpawnConfig SpawnConfig
        {
            get => spawnConfig;
            set => spawnConfig = value;
        }

        public void SpawnItems(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            if (spawnConfig == null)
            {
                Debug.LogError("[ItemSpawner] No ItemSpawnConfig assigned!");
                return;
            }

            // Clear any previously spawned items
            ClearSpawnedItems();

            // Always spawn key at exit (independent of counts)
            if (spawnConfig.KeyItemPrefab != null)
            {
                SpawnItem(spawnConfig.KeyItemPrefab, exitPos, itemContainer);
            }

            // Find spawn positions
            var deadEndPositions = FindDeadEnds(grid, startPos, exitPos);

            // Calculate total items needed (XP + general)
            int totalItemsNeeded = spawnConfig.GetTotalItemCount();

            // If not enough dead ends, add regular floor positions as fallback
            if (deadEndPositions.Count < totalItemsNeeded)
            {
                var floorPositions = FindFloorPositions(grid, startPos, exitPos, deadEndPositions);
                ShuffleList(floorPositions);

                int needed = totalItemsNeeded - deadEndPositions.Count;
                for (int i = 0; i < floorPositions.Count && i < needed; i++)
                {
                    deadEndPositions.Add(floorPositions[i]);
                }
            }

            // Shuffle positions for random placement
            ShuffleList(deadEndPositions);

            int posIndex = 0;

            // Spawn XP items
            if (spawnConfig.XpItemPrefab != null)
            {
                for (int i = 0; i < spawnConfig.XpItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(spawnConfig.XpItemPrefab, deadEndPositions[posIndex], xpContainer);
                }
            }

            // Spawn general items (randomly picked from pool)
            for (int i = 0; i < spawnConfig.GeneralItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
            {
                var randomItem = spawnConfig.GetRandomGeneralItem();
                if (randomItem != null)
                {
                    SpawnItem(randomItem, deadEndPositions[posIndex], itemContainer);
                }
            }

            Debug.Log($"ItemSpawner: Spawned {posIndex} items ({spawnConfig.XpItemCount} XP + {spawnConfig.GeneralItemCount} general)");
        }

        /// <summary>
        /// Clears all spawned items from the scene.
        /// </summary>
        public void ClearSpawnedItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _spawnedItems.Clear();
        }

        private void SpawnItem(GameObject prefab, Vector2 position, Transform container = null)
        {
            if (prefab == null) return;
            var item = Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            if (container != null)
                item.transform.SetParent(container);
            _spawnedItems.Add(item);
        }

        /// <summary>
        /// Finds all dead-end positions in the maze.
        /// </summary>
        private List<Vector2> FindDeadEnds(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            var deadEnds = new List<Vector2>();

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Height - 1; y++)
                {
                    if (grid.GetCell(x, y).IsWall)
                        continue;

                    if (IsNearPosition(x, y, startPos, 2) || IsNearPosition(x, y, exitPos, 2))
                        continue;

                    if (IsDeadEnd(grid, x, y))
                    {
                        deadEnds.Add(new Vector2(x + 0.5f, y + 0.5f));
                    }
                }
            }

            return deadEnds;
        }

        private bool IsDeadEnd(MazeGrid grid, int x, int y)
        {
            int connections = 0;

            if (!IsWallAt(grid, x - 1, y)) connections++;
            if (!IsWallAt(grid, x + 1, y)) connections++;
            if (!IsWallAt(grid, x, y - 1)) connections++;
            if (!IsWallAt(grid, x, y + 1)) connections++;

            return connections == 1;
        }

        private bool IsWallAt(MazeGrid grid, int x, int y)
        {
            if (!grid.IsInBounds(x, y))
                return true;
            return grid.GetCell(x, y).IsWall;
        }

        private bool IsNearPosition(int x, int y, Vector2 pos, int distance)
        {
            return Mathf.Abs(x - pos.x) <= distance && Mathf.Abs(y - pos.y) <= distance;
        }

        private List<Vector2> FindFloorPositions(MazeGrid grid, Vector2 startPos, Vector2 exitPos, List<Vector2> excludePositions)
        {
            var floorPositions = new List<Vector2>();

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Height - 1; y++)
                {
                    if (grid.GetCell(x, y).IsWall)
                        continue;

                    if (IsNearPosition(x, y, startPos, 3) || IsNearPosition(x, y, exitPos, 2))
                        continue;

                    var pos = new Vector2(x + 0.5f, y + 0.5f);

                    bool excluded = false;
                    foreach (var excludePos in excludePositions)
                    {
                        if (Vector2.Distance(pos, excludePos) < 1f)
                        {
                            excluded = true;
                            break;
                        }
                    }

                    if (!excluded)
                    {
                        floorPositions.Add(pos);
                    }
                }
            }

            return floorPositions;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
