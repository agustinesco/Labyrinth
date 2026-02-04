using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public class Pathfinding
    {
        private readonly MazeGrid _grid;

        // Cardinal directions (cost = 1)
        private static readonly Vector2Int[] CardinalNeighbors =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        // Diagonal directions (cost = sqrt(2) ≈ 1.414)
        private static readonly Vector2Int[] DiagonalNeighbors =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        private const float DiagonalCost = 1.414f;

        // Path caching
        private Vector2Int _cachedStart;
        private Vector2Int _cachedGoal;
        private List<Vector2Int> _cachedPath;

        // Reusable data structures to reduce GC allocations
        private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        private readonly Dictionary<Vector2Int, float> _gScore = new Dictionary<Vector2Int, float>();
        private readonly MinHeap _openSet = new MinHeap();
        private readonly HashSet<Vector2Int> _closedSet = new HashSet<Vector2Int>();

        public Pathfinding(MazeGrid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// Finds a path from start to goal using optimized A* with caching.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            // Return cached path if start/goal haven't changed significantly
            if (_cachedPath != null && _cachedStart == start && _cachedGoal == goal)
            {
                return _cachedPath;
            }

            // Early exit: if start equals goal
            if (start == goal)
            {
                return new List<Vector2Int> { start };
            }

            // Early exit: if goal is a wall
            if (!_grid.IsInBounds(goal.x, goal.y) || _grid.GetCell(goal.x, goal.y).IsWall)
            {
                return null;
            }

            // Clear reusable structures
            _cameFrom.Clear();
            _gScore.Clear();
            _openSet.Clear();
            _closedSet.Clear();

            _gScore[start] = 0;
            _openSet.Insert(start, Heuristic(start, goal));

            while (_openSet.Count > 0)
            {
                var current = _openSet.ExtractMin();

                if (current == goal)
                {
                    var path = ReconstructPath(current);
                    // Cache the result
                    _cachedStart = start;
                    _cachedGoal = goal;
                    _cachedPath = path;
                    return path;
                }

                _closedSet.Add(current);

                // Process cardinal directions (cost = 1)
                ProcessNeighbors(current, goal, CardinalNeighbors, 1f, false);

                // Process diagonal directions (cost = sqrt(2))
                ProcessNeighbors(current, goal, DiagonalNeighbors, DiagonalCost, true);
            }

            return null;
        }

        /// <summary>
        /// Finds a path, but only if the cached path is invalid or endpoints changed.
        /// Returns the cached path if still valid.
        /// </summary>
        public List<Vector2Int> FindPathCached(Vector2Int start, Vector2Int goal, int cacheToleranceDistance = 2)
        {
            // Check if cached path is still usable
            if (_cachedPath != null && _cachedPath.Count > 0)
            {
                int startDist = Mathf.Abs(start.x - _cachedStart.x) + Mathf.Abs(start.y - _cachedStart.y);
                int goalDist = Mathf.Abs(goal.x - _cachedGoal.x) + Mathf.Abs(goal.y - _cachedGoal.y);

                if (startDist <= cacheToleranceDistance && goalDist <= cacheToleranceDistance)
                {
                    return _cachedPath;
                }
            }

            return FindPath(start, goal);
        }

        /// <summary>
        /// Clears the path cache, forcing a fresh calculation on next FindPath call.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedPath = null;
        }

        private void ProcessNeighbors(Vector2Int current, Vector2Int goal, Vector2Int[] directions, float cost, bool checkCornerCutting)
        {
            float currentG = _gScore[current];

            foreach (var dir in directions)
            {
                var neighbor = current + dir;

                if (_closedSet.Contains(neighbor))
                    continue;

                if (!_grid.IsInBounds(neighbor.x, neighbor.y))
                    continue;

                if (_grid.GetCell(neighbor.x, neighbor.y).IsWall)
                    continue;

                // Prevent corner cutting for diagonal movement
                if (checkCornerCutting)
                {
                    var adj1 = new Vector2Int(current.x + dir.x, current.y);
                    var adj2 = new Vector2Int(current.x, current.y + dir.y);

                    if (!_grid.IsInBounds(adj1.x, adj1.y) || _grid.GetCell(adj1.x, adj1.y).IsWall)
                        continue;
                    if (!_grid.IsInBounds(adj2.x, adj2.y) || _grid.GetCell(adj2.x, adj2.y).IsWall)
                        continue;
                }

                float tentativeG = currentG + cost;

                if (!_gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                {
                    _cameFrom[neighbor] = current;
                    _gScore[neighbor] = tentativeG;
                    float fScore = tentativeG + Heuristic(neighbor, goal);

                    _openSet.InsertOrUpdate(neighbor, fScore);
                }
            }
        }

        // Octile distance heuristic - optimal for 8-directional movement
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            // Octile: min(dx,dy) diagonal moves + |dx-dy| cardinal moves
            return Mathf.Min(dx, dy) * DiagonalCost + Mathf.Abs(dx - dy);
        }

        private List<Vector2Int> ReconstructPath(Vector2Int current)
        {
            var path = new List<Vector2Int>();
            path.Add(current);

            while (_cameFrom.TryGetValue(current, out var parent))
            {
                current = parent;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Min-heap (priority queue) implementation for efficient A* open set operations.
        /// </summary>
        private class MinHeap
        {
            private readonly List<(Vector2Int node, float priority)> _heap = new List<(Vector2Int, float)>();
            private readonly Dictionary<Vector2Int, int> _indices = new Dictionary<Vector2Int, int>();

            public int Count => _heap.Count;

            public void Clear()
            {
                _heap.Clear();
                _indices.Clear();
            }

            public void Insert(Vector2Int node, float priority)
            {
                _heap.Add((node, priority));
                int index = _heap.Count - 1;
                _indices[node] = index;
                BubbleUp(index);
            }

            public void InsertOrUpdate(Vector2Int node, float priority)
            {
                if (_indices.TryGetValue(node, out int existingIndex))
                {
                    float oldPriority = _heap[existingIndex].priority;
                    _heap[existingIndex] = (node, priority);

                    if (priority < oldPriority)
                        BubbleUp(existingIndex);
                    else
                        BubbleDown(existingIndex);
                }
                else
                {
                    Insert(node, priority);
                }
            }

            public Vector2Int ExtractMin()
            {
                var min = _heap[0].node;
                _indices.Remove(min);

                int lastIndex = _heap.Count - 1;
                if (lastIndex > 0)
                {
                    _heap[0] = _heap[lastIndex];
                    _indices[_heap[0].node] = 0;
                    _heap.RemoveAt(lastIndex);
                    BubbleDown(0);
                }
                else
                {
                    _heap.RemoveAt(0);
                }

                return min;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_heap[index].priority >= _heap[parent].priority)
                        break;

                    Swap(index, parent);
                    index = parent;
                }
            }

            private void BubbleDown(int index)
            {
                int count = _heap.Count;
                while (true)
                {
                    int smallest = index;
                    int left = 2 * index + 1;
                    int right = 2 * index + 2;

                    if (left < count && _heap[left].priority < _heap[smallest].priority)
                        smallest = left;
                    if (right < count && _heap[right].priority < _heap[smallest].priority)
                        smallest = right;

                    if (smallest == index)
                        break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int i, int j)
            {
                var temp = _heap[i];
                _heap[i] = _heap[j];
                _heap[j] = temp;

                _indices[_heap[i].node] = i;
                _indices[_heap[j].node] = j;
            }
        }
    }
}
