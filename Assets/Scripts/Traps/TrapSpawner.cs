using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Traps
{
    public class TrapSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject tripwirePrefab;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private int minTraps = 3;
        [SerializeField] private int maxTraps = 5;
        [SerializeField] private int bufferFromStartExit = 3;

        [Header("Container")]
        [SerializeField, Tooltip("Parent transform for spawned traps (optional)")]
        private Transform trapContainer;

        private struct HallwayPosition
        {
            public Vector2 Position;
            public Vector2 ArrowDirection;
        }

        public void SpawnTraps(MazeGrid grid, Vector2 startPos, Vector2 exitPos, int corridorWidth)
        {
            var validPositions = FindHallwayPositions(grid, startPos, exitPos, corridorWidth);

            if (validPositions.Count == 0) return;

            // Shuffle positions
            ShuffleList(validPositions);

            // Determine trap count
            int trapCount = Random.Range(minTraps, maxTraps + 1);
            trapCount = Mathf.Min(trapCount, validPositions.Count);

            // Spawn traps
            for (int i = 0; i < trapCount; i++)
            {
                SpawnTripwire(validPositions[i], corridorWidth);
            }
        }

        private List<HallwayPosition> FindHallwayPositions(MazeGrid grid, Vector2 startPos, Vector2 exitPos, int corridorWidth)
        {
            var positions = new List<HallwayPosition>();
            int checkDistance = corridorWidth; // Distance to check for walls

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);

                    // Skip walls, start, and exit
                    if (cell.IsWall || cell.IsStart || cell.IsExit) continue;

                    // Skip positions too close to start or exit
                    Vector2 pos = new Vector2(x, y);
                    if (Vector2.Distance(pos, startPos) < bufferFromStartExit * corridorWidth) continue;
                    if (Vector2.Distance(pos, exitPos) < bufferFromStartExit * corridorWidth) continue;

                    // Check for hallway pattern with corridor width consideration
                    var hallwayInfo = GetHallwayInfo(grid, x, y, checkDistance);
                    if (hallwayInfo.HasValue)
                    {
                        positions.Add(new HallwayPosition
                        {
                            Position = pos,
                            ArrowDirection = hallwayInfo.Value
                        });
                    }
                }
            }

            return positions;
        }

        private Vector2? GetHallwayInfo(MazeGrid grid, int x, int y, int checkDistance)
        {
            // Find walls by checking in each direction up to checkDistance
            bool hasNorthWall = HasWallInDirection(grid, x, y, 0, 1, checkDistance);
            bool hasSouthWall = HasWallInDirection(grid, x, y, 0, -1, checkDistance);
            bool hasEastWall = HasWallInDirection(grid, x, y, 1, 0, checkDistance);
            bool hasWestWall = HasWallInDirection(grid, x, y, -1, 0, checkDistance);

            var validDirections = new List<Vector2>();

            // If there's a wall to the north, arrow can shoot from north
            if (hasNorthWall) validDirections.Add(Vector2.down);
            if (hasSouthWall) validDirections.Add(Vector2.up);
            if (hasEastWall) validDirections.Add(Vector2.left);
            if (hasWestWall) validDirections.Add(Vector2.right);

            if (validDirections.Count == 0) return null;

            // Pick a random valid direction
            return validDirections[Random.Range(0, validDirections.Count)];
        }

        private bool HasWallInDirection(MazeGrid grid, int startX, int startY, int dx, int dy, int maxDistance)
        {
            for (int i = 1; i <= maxDistance; i++)
            {
                int checkX = startX + dx * i;
                int checkY = startY + dy * i;

                if (!grid.IsInBounds(checkX, checkY))
                    return true; // Out of bounds counts as wall

                if (grid.GetCell(checkX, checkY).IsWall)
                    return true;
            }
            return false;
        }

        private void SpawnTripwire(HallwayPosition hallway, int corridorWidth)
        {
            GameObject trapObj;

            if (tripwirePrefab != null)
            {
                trapObj = Instantiate(tripwirePrefab, 
                    new Vector3(hallway.Position.x, hallway.Position.y, 0), 
                    Quaternion.identity);
            }
            else
            {
                // Create trap dynamically if no prefab
                trapObj = new GameObject("TripwireTrap");
                trapObj.transform.position = new Vector3(hallway.Position.x, hallway.Position.y, 0);
                trapObj.AddComponent<TripwireTrap>();
                
                // Add trigger collider
                var collider = trapObj.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(corridorWidth, corridorWidth);
            }

            if (trapContainer != null)
                trapObj.transform.SetParent(trapContainer);

            var tripwire = trapObj.GetComponent<TripwireTrap>();
            if (tripwire != null)
            {
                tripwire.SetArrowPrefab(arrowPrefab);
                tripwire.Initialize(hallway.ArrowDirection, corridorWidth);
            }
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
