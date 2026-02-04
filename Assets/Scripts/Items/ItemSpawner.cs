using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Item Prefabs")]
        [SerializeField] private GameObject keyItemPrefab;
        [SerializeField] private GameObject speedItemPrefab;
        [SerializeField] private GameObject lightSourcePrefab;
        [SerializeField] private GameObject healItemPrefab;
        [SerializeField] private GameObject explosiveItemPrefab;
        [SerializeField] private GameObject xpItemPrefab;
        [SerializeField] private GameObject pebblesItemPrefab;
        [SerializeField] private GameObject invisibilityItemPrefab;
        [SerializeField] private GameObject wispItemPrefab;

        [Header("Spawn Counts")]
        [SerializeField] private int speedItemCount = 6;
        [SerializeField] private int lightSourceCount = 6;
        [SerializeField] private int healItemCount = 4;
        [SerializeField] private int explosiveItemCount = 4;
        [SerializeField] private int xpItemCount = 45;
        [SerializeField] private int pebblesItemCount = 4;
        [SerializeField] private int invisibilityItemCount = 2;
        [SerializeField] private int wispItemCount = 2;

        [Header("Active Items (controls spawning)")]
        [SerializeField] private bool speedItemActive = true;
        [SerializeField] private bool lightSourceActive = true;
        [SerializeField] private bool healItemActive = true;
        [SerializeField] private bool explosiveItemActive = false;
        [SerializeField] private bool xpItemActive = true;
        [SerializeField] private bool pebblesItemActive = false;
        [SerializeField] private bool invisibilityItemActive = true;
        [SerializeField] private bool wispItemActive = true;

        public void SpawnItems(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            // Spawn key at exit
            SpawnItem(keyItemPrefab, exitPos);

            // Find dead-end positions (corridor endings with only one connection)
            var deadEndPositions = FindDeadEnds(grid, startPos, exitPos);

            // Calculate total items needed (only count active items)
            int totalItemsNeeded = 0;
            if (speedItemActive) totalItemsNeeded += speedItemCount;
            if (lightSourceActive) totalItemsNeeded += lightSourceCount;
            if (healItemActive) totalItemsNeeded += healItemCount;
            if (explosiveItemActive) totalItemsNeeded += explosiveItemCount;
            if (xpItemActive) totalItemsNeeded += xpItemCount;
            if (pebblesItemActive) totalItemsNeeded += pebblesItemCount;
            if (invisibilityItemActive) totalItemsNeeded += invisibilityItemCount;
            if (wispItemActive) totalItemsNeeded += wispItemCount;

            // If not enough dead ends, add regular floor positions as fallback
            if (deadEndPositions.Count < totalItemsNeeded)
            {
                var floorPositions = FindFloorPositions(grid, startPos, exitPos, deadEndPositions);
                ShuffleList(floorPositions);

                // Add floor positions until we have enough
                int needed = totalItemsNeeded - deadEndPositions.Count;
                for (int i = 0; i < floorPositions.Count && i < needed; i++)
                {
                    deadEndPositions.Add(floorPositions[i]);
                }
            }

            // Shuffle positions
            ShuffleList(deadEndPositions);

            int posIndex = 0;

            // Spawn speed items
            if (speedItemActive)
            {
                for (int i = 0; i < speedItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(speedItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn light sources
            if (lightSourceActive)
            {
                for (int i = 0; i < lightSourceCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(lightSourcePrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn heal items
            if (healItemActive)
            {
                for (int i = 0; i < healItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(healItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn explosive items
            if (explosiveItemActive)
            {
                for (int i = 0; i < explosiveItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(explosiveItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn XP items
            if (xpItemActive)
            {
                for (int i = 0; i < xpItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(xpItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn pebbles items
            if (pebblesItemActive)
            {
                for (int i = 0; i < pebblesItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(pebblesItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn invisibility items
            if (invisibilityItemActive)
            {
                for (int i = 0; i < invisibilityItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(invisibilityItemPrefab, deadEndPositions[posIndex]);
                }
            }

            // Spawn wisp items
            if (wispItemActive)
            {
                for (int i = 0; i < wispItemCount && posIndex < deadEndPositions.Count; i++, posIndex++)
                {
                    SpawnItem(wispItemPrefab, deadEndPositions[posIndex]);
                }
            }

            Debug.Log($"ItemSpawner: Found {deadEndPositions.Count} dead ends, spawned items at {posIndex} locations");
        }

        private void SpawnItem(GameObject prefab, Vector2 position)
        {
            if (prefab == null) return;
            Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        }

        /// <summary>
        /// Finds all dead-end positions in the maze.
        /// A dead-end is the center of a 3x3 floor area that has only one exit direction.
        /// </summary>
        private List<Vector2> FindDeadEnds(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            var deadEnds = new List<Vector2>();

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Height - 1; y++)
                {
                    // Skip if center is a wall
                    if (grid.GetCell(x, y).IsWall)
                        continue;

                    // Skip start and exit positions
                    if (IsNearPosition(x, y, startPos, 2) || IsNearPosition(x, y, exitPos, 2))
                        continue;

                    // Check if this is a dead-end (corridor ending)
                    if (IsDeadEnd(grid, x, y))
                    {
                        deadEnds.Add(new Vector2(x + 0.5f, y + 0.5f));
                    }
                }
            }

            return deadEnds;
        }

        /// <summary>
        /// Checks if a floor tile is a dead-end (has only one exit direction).
        /// </summary>
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

        /// <summary>
        /// Finds all valid floor positions (not start, not exit, not already in deadEnds list).
        /// </summary>
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
