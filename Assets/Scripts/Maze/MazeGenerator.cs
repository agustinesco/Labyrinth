using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public class MazeGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _corridorWidth;
        private readonly float _branchingFactor;
        private MazeGrid _grid;
        private System.Random _random;

        // Step size is corridorWidth + 1 (corridor + wall between corridors)
        private (int dx, int dy)[] _directions;
        private int _step;

        // Key room properties
        private const int RoomSize = 9;
        private int _roomCenterX;
        private int _roomCenterY;
        private List<(int x, int y, int dx, int dy)> _roomEntrances;

        /// <summary>
        /// Creates a new maze generator.
        /// </summary>
        /// <param name="width">Maze width in cells</param>
        /// <param name="height">Maze height in cells</param>
        /// <param name="seed">Random seed for reproducible mazes</param>
        /// <param name="corridorWidth">Width of corridors (will be made odd)</param>
        /// <param name="branchingFactor">
        /// Controls maze branching: 0.0 = long winding corridors (DFS),
        /// 1.0 = maximum bifurcations (Prim's-like). Default 0.0.
        /// </param>
        public MazeGenerator(int width, int height, int? seed = null, int corridorWidth = 1, float branchingFactor = 0f)
        {
            _width = width;
            _height = height;

            // Ensure corridor width is odd (for symmetrical corridors) and at least 1
            _corridorWidth = Mathf.Max(1, corridorWidth);
            if (_corridorWidth % 2 == 0)
                _corridorWidth += 1; // Make it odd

            // Clamp branching factor between 0 and 1
            _branchingFactor = Mathf.Clamp01(branchingFactor);

            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            // Wall thickness scales with corridor width (minimum 1)
            int wallThickness = Mathf.Max(1, _corridorWidth / 2);

            // Step size: corridor width + wall thickness
            _step = _corridorWidth + wallThickness;
            _directions = new (int, int)[]
            {
                (0, _step), (_step, 0), (0, -_step), (-_step, 0)
            };
        }

        public MazeGrid Generate()
        {
            _grid = new MazeGrid(_width, _height);

            // Start position needs to account for corridor width
            int halfWidth = _corridorWidth / 2;
            int startX = 1 + halfWidth;
            int startY = 1 + halfWidth;

            // Calculate and carve the key room in a quadrant opposite the player
            CarveKeyRoom(startX, startY);

            // Use Growing Tree algorithm with configurable branching
            GrowingTreeGenerate(startX, startY);

            // Connect room entrances to the maze
            ConnectRoomToMaze();

            _grid.GetCell(startX, startY).IsStart = true;

            // Key room center is the exit (where key spawns)
            var exitCell = _grid.GetCell(_roomCenterX, _roomCenterY);
            exitCell.IsExit = true;

            // Validate that key room is reachable from start
            if (!ValidateKeyRoomReachability(startX, startY))
            {
                Debug.LogWarning("[MazeGenerator] Key room not reachable! Forcing connection...");
                ForceConnectionToKeyRoom(startX, startY);
            }

            return _grid;
        }

        /// <summary>
        /// Removes grass (floor) tiles that are adjacent to exactly 3 other grass tiles.
        /// This cleans up certain unwanted configurations in the maze.
        /// </summary>
        private void RemoveTripleAdjacentGrass()
        {
            // Collect tiles to convert (don't modify while iterating)
            var tilesToConvert = new List<(int x, int y)>();

            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    var cell = _grid.GetCell(x, y);

                    // Skip walls, start, exit, and key room tiles
                    if (cell.IsWall || cell.IsStart || cell.IsExit || cell.IsKeyRoom)
                        continue;

                    // Count adjacent grass tiles (cardinal directions)
                    int adjacentGrassCount = CountAdjacentGrass(x, y);

                    if (adjacentGrassCount == 3)
                    {
                        tilesToConvert.Add((x, y));
                    }
                }
            }

            // Convert collected tiles to walls
            foreach (var (x, y) in tilesToConvert)
            {
                _grid.GetCell(x, y).IsWall = true;
            }
        }

        /// <summary>
        /// Counts how many adjacent tiles (cardinal directions) are grass (non-wall).
        /// </summary>
        private int CountAdjacentGrass(int x, int y)
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

        /// <summary>
        /// Growing Tree algorithm - a hybrid between DFS and Prim's algorithm.
        /// The branchingFactor controls the selection strategy:
        /// - 0.0: Always pick the newest cell (pure DFS = long corridors)
        /// - 1.0: Always pick a random cell (Prim's-like = maximum branching)
        /// - 0.5: 50% chance of either (balanced)
        /// </summary>
        private void GrowingTreeGenerate(int startX, int startY)
        {
            var activeCells = new List<(int x, int y)>();

            // Start with the initial cell
            CarveArea(startX, startY);
            _grid.GetCell(startX, startY).IsVisited = true;
            activeCells.Add((startX, startY));

            while (activeCells.Count > 0)
            {
                // Select cell based on branching factor
                int index = SelectCellIndex(activeCells.Count);
                var (x, y) = activeCells[index];

                // Find valid neighbors
                var validNeighbors = GetValidNeighbors(x, y);

                if (validNeighbors.Count > 0)
                {
                    // Pick a random valid neighbor
                    var (nx, ny) = validNeighbors[_random.Next(validNeighbors.Count)];

                    // Carve corridor to the neighbor
                    CarveCorridorBetween(x, y, nx, ny);
                    CarveArea(nx, ny);
                    _grid.GetCell(nx, ny).IsVisited = true;

                    // Add the new cell to active list
                    activeCells.Add((nx, ny));
                }
                else
                {
                    // No valid neighbors, remove this cell from active list
                    activeCells.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Selects which cell index to use based on branching factor.
        /// - branchingFactor = 0: Always return last index (newest = DFS)
        /// - branchingFactor = 1: Always return random index (Prim's)
        /// - branchingFactor = 0.5: 50% chance of either
        /// </summary>
        private int SelectCellIndex(int count)
        {
            if (count == 1)
                return 0;

            // Roll to determine selection method
            if (_random.NextDouble() < _branchingFactor)
            {
                // Random selection (more branching)
                return _random.Next(count);
            }
            else
            {
                // Newest selection (DFS, long corridors)
                return count - 1;
            }
        }

        /// <summary>
        /// Gets all valid unvisited neighbors for a cell.
        /// </summary>
        private List<(int x, int y)> GetValidNeighbors(int x, int y)
        {
            var neighbors = new List<(int x, int y)>();

            foreach (var (dx, dy) in _directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (IsValidCarveTarget(nx, ny))
                {
                    neighbors.Add((nx, ny));
                }
            }

            return neighbors;
        }

        private void CarveArea(int centerX, int centerY)
        {
            int halfWidth = _corridorWidth / 2;
            for (int ox = -halfWidth; ox <= halfWidth; ox++)
            {
                for (int oy = -halfWidth; oy <= halfWidth; oy++)
                {
                    int cx = centerX + ox;
                    int cy = centerY + oy;
                    if (_grid.IsInBounds(cx, cy) && cx > 0 && cx < _width - 1 && cy > 0 && cy < _height - 1)
                    {
                        _grid.GetCell(cx, cy).IsWall = false;
                    }
                }
            }
        }

        private void CarveCorridorBetween(int x1, int y1, int x2, int y2)
        {
            int dx = x2 > x1 ? 1 : (x2 < x1 ? -1 : 0);
            int dy = y2 > y1 ? 1 : (y2 < y1 ? -1 : 0);

            int cx = x1;
            int cy = y1;

            while (cx != x2 || cy != y2)
            {
                cx += dx;
                cy += dy;
                CarveArea(cx, cy);
            }
        }

        private bool IsValidCarveTarget(int x, int y)
        {
            int halfWidth = _corridorWidth / 2;
            // Check if the target area is within bounds and hasn't been carved yet
            if (!_grid.IsInBounds(x, y))
                return false;

            // Ensure we stay away from edges
            if (x - halfWidth <= 0 || x + halfWidth >= _width - 1 ||
                y - halfWidth <= 0 || y + halfWidth >= _height - 1)
                return false;

            // Skip cells that overlap with the key room area (with buffer)
            int roomHalf = RoomSize / 2 + 1; // +1 buffer for wall around room
            if (x >= _roomCenterX - roomHalf && x <= _roomCenterX + roomHalf &&
                y >= _roomCenterY - roomHalf && y <= _roomCenterY + roomHalf)
                return false;

            // Check if center cell is still a wall (unvisited)
            return _grid.GetCell(x, y).IsWall;
        }

        private void CarveKeyRoom(int startX, int startY)
        {
            int roomHalf = RoomSize / 2;
            int margin = roomHalf + 1; // keep room away from maze edges

            // Determine which quadrant the player starts in
            int midX = _width / 2;
            int midY = _height / 2;
            bool playerIsLeft = startX < midX;
            bool playerIsBottom = startY < midY;

            // Collect the 3 quadrants opposite to the player's quadrant
            // Each quadrant center is at (1/4 or 3/4) of the maze dimensions
            var candidates = new List<(int cx, int cy)>();
            int q1X = _width / 4;
            int q3X = 3 * _width / 4;
            int q1Y = _height / 4;
            int q3Y = 3 * _height / 4;

            // Add all 4 quadrant centers, then remove the player's quadrant
            var allQuadrants = new List<(int cx, int cy, bool left, bool bottom)>
            {
                (q1X, q1Y, true, true),    // bottom-left
                (q3X, q1Y, false, true),   // bottom-right
                (q1X, q3Y, true, false),   // top-left
                (q3X, q3Y, false, false),  // top-right
            };

            foreach (var (cx, cy, left, bottom) in allQuadrants)
            {
                if (left == playerIsLeft && bottom == playerIsBottom)
                    continue; // skip the player's own quadrant
                candidates.Add((cx, cy));
            }

            // Pick a random candidate quadrant
            var chosen = candidates[_random.Next(candidates.Count)];

            // Clamp room center so the 9x9 room stays within maze bounds
            _roomCenterX = Mathf.Clamp(chosen.cx, margin, _width - 1 - margin);
            _roomCenterY = Mathf.Clamp(chosen.cy, margin, _height - 1 - margin);

            // Snap to nearest valid grid position (odd coordinate) so room aligns with maze grid
            if (_roomCenterX % 2 == 0) _roomCenterX++;
            if (_roomCenterY % 2 == 0) _roomCenterY++;

            // Carve the 9x9 room area and mark as key room
            for (int x = _roomCenterX - roomHalf; x <= _roomCenterX + roomHalf; x++)
            {
                for (int y = _roomCenterY - roomHalf; y <= _roomCenterY + roomHalf; y++)
                {
                    if (_grid.IsInBounds(x, y))
                    {
                        var cell = _grid.GetCell(x, y);
                        cell.IsWall = false;
                        cell.IsKeyRoom = true;
                    }
                }
            }

            // Select 2-4 random entrances from the four cardinal directions
            var possibleEntrances = new List<(int x, int y, int dx, int dy)>
            {
                (_roomCenterX, _roomCenterY + roomHalf, 0, 1),   // North
                (_roomCenterX, _roomCenterY - roomHalf, 0, -1), // South
                (_roomCenterX + roomHalf, _roomCenterY, 1, 0),  // East
                (_roomCenterX - roomHalf, _roomCenterY, -1, 0)  // West
            };

            // Shuffle and select 2-4 entrances
            ShuffleList(possibleEntrances);
            int entranceCount = _random.Next(2, 5); // 2 to 4 entrances
            _roomEntrances = possibleEntrances.GetRange(0, entranceCount);
        }

        private void ConnectRoomToMaze()
        {
            foreach (var (entranceX, entranceY, dx, dy) in _roomEntrances)
            {
                // Carve outward from the entrance until we hit a maze corridor
                int x = entranceX + dx;
                int y = entranceY + dy;

                while (_grid.IsInBounds(x, y))
                {
                    var cell = _grid.GetCell(x, y);

                    // If we found an existing corridor (non-wall, non-room), we're connected
                    if (!cell.IsWall && !cell.IsKeyRoom)
                        break;

                    // Carve this cell and surrounding area based on corridor width
                    CarveArea(x, y);

                    x += dx;
                    y += dy;
                }
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private (int, int)[] ShuffleDirections()
        {
            var shuffled = new List<(int, int)>(_directions);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            return shuffled.ToArray();
        }

        private MazeCell FindFurthestCell(int startX, int startY)
        {
            var distances = new int[_width, _height];
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    distances[x, y] = -1;

            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            distances[startX, startY] = 0;

            MazeCell furthest = _grid.GetCell(startX, startY);
            int maxDistance = 0;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                int currentDist = distances[cx, cy];

                if (currentDist > maxDistance)
                {
                    maxDistance = currentDist;
                    furthest = _grid.GetCell(cx, cy);
                }

                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (_grid.IsInBounds(nx, ny) &&
                        !_grid.GetCell(nx, ny).IsWall &&
                        distances[nx, ny] == -1)
                    {
                        distances[nx, ny] = currentDist + 1;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return furthest;
        }

        /// <summary>
        /// Validates that the key room is reachable from the start position using BFS.
        /// </summary>
        /// <param name="startX">Start X position</param>
        /// <param name="startY">Start Y position</param>
        /// <returns>True if the key room is reachable, false otherwise</returns>
        private bool ValidateKeyRoomReachability(int startX, int startY)
        {
            var visited = new bool[_width, _height];
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                // Check if we reached any part of the key room
                if (_grid.GetCell(cx, cy).IsKeyRoom)
                {
                    return true;
                }

                // Explore neighbors
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (_grid.IsInBounds(nx, ny) &&
                        !visited[nx, ny] &&
                        !_grid.GetCell(nx, ny).IsWall)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Forces a connection from the start position to the key room by carving a direct path.
        /// Uses BFS to find the closest reachable cell to the key room, then carves to it.
        /// </summary>
        /// <param name="startX">Start X position</param>
        /// <param name="startY">Start Y position</param>
        private void ForceConnectionToKeyRoom(int startX, int startY)
        {
            // Find the closest point from the maze to the key room using BFS
            var visited = new bool[_width, _height];
            var parent = new (int x, int y)?[_width, _height];
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            (int x, int y) closestToRoom = (startX, startY);
            float minDistanceToRoom = float.MaxValue;

            // Find the closest walkable cell to the key room
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                // Calculate distance to room center
                float distToRoom = Mathf.Sqrt(
                    Mathf.Pow(cx - _roomCenterX, 2) +
                    Mathf.Pow(cy - _roomCenterY, 2)
                );

                if (distToRoom < minDistanceToRoom)
                {
                    minDistanceToRoom = distToRoom;
                    closestToRoom = (cx, cy);
                }

                // Explore neighbors
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (_grid.IsInBounds(nx, ny) &&
                        !visited[nx, ny] &&
                        !_grid.GetCell(nx, ny).IsWall)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            // Carve a direct path from closest point to the room
            int x = closestToRoom.x;
            int y = closestToRoom.y;

            // Find the nearest room entrance point
            int roomHalf = RoomSize / 2;
            int targetX, targetY;

            // Determine which side of the room to connect to
            if (Mathf.Abs(x - _roomCenterX) > Mathf.Abs(y - _roomCenterY))
            {
                // Connect horizontally
                targetX = x < _roomCenterX ? _roomCenterX - roomHalf : _roomCenterX + roomHalf;
                targetY = _roomCenterY;
            }
            else
            {
                // Connect vertically
                targetX = _roomCenterX;
                targetY = y < _roomCenterY ? _roomCenterY - roomHalf : _roomCenterY + roomHalf;
            }

            // Carve L-shaped path: first horizontal, then vertical
            int currentX = x;
            int currentY = y;

            // Move horizontally first
            int stepX = targetX > currentX ? 1 : -1;
            while (currentX != targetX)
            {
                currentX += stepX;
                if (_grid.IsInBounds(currentX, currentY))
                {
                    CarveArea(currentX, currentY);
                }
            }

            // Then move vertically
            int stepY = targetY > currentY ? 1 : -1;
            while (currentY != targetY)
            {
                currentY += stepY;
                if (_grid.IsInBounds(currentX, currentY))
                {
                    CarveArea(currentX, currentY);
                }
            }

            Debug.Log($"[MazeGenerator] Forced connection carved from ({closestToRoom.x}, {closestToRoom.y}) to room at ({targetX}, {targetY})");
        }
    }
}
