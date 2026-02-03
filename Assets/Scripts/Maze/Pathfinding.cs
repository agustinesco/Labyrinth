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

        public Pathfinding(MazeGrid grid)
        {
            _grid = grid;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            var gScore = new Dictionary<Vector2Int, float>();
            gScore[start] = 0;

            var fScore = new Dictionary<Vector2Int, float>();
            fScore[start] = Heuristic(start, goal);

            while (openSet.Count > 0)
            {
                var current = GetLowestFScore(openSet, fScore);

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                // Process cardinal directions (cost = 1)
                foreach (var dir in CardinalNeighbors)
                {
                    var neighbor = current + dir;

                    if (!_grid.IsInBounds(neighbor.x, neighbor.y))
                        continue;

                    if (_grid.GetCell(neighbor.x, neighbor.y).IsWall)
                        continue;

                    float tentativeG = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }

                // Process diagonal directions (cost = sqrt(2))
                foreach (var dir in DiagonalNeighbors)
                {
                    var neighbor = current + dir;

                    if (!_grid.IsInBounds(neighbor.x, neighbor.y))
                        continue;

                    if (_grid.GetCell(neighbor.x, neighbor.y).IsWall)
                        continue;

                    // Prevent corner cutting - both adjacent cardinal cells must be passable
                    var adj1 = new Vector2Int(current.x + dir.x, current.y);
                    var adj2 = new Vector2Int(current.x, current.y + dir.y);

                    if (!_grid.IsInBounds(adj1.x, adj1.y) || _grid.GetCell(adj1.x, adj1.y).IsWall)
                        continue;
                    if (!_grid.IsInBounds(adj2.x, adj2.y) || _grid.GetCell(adj2.x, adj2.y).IsWall)
                        continue;

                    float tentativeG = gScore[current] + DiagonalCost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null;
        }

        // Octile distance heuristic - optimal for 8-directional movement
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            // Octile: min(dx,dy) diagonal moves + |dx-dy| cardinal moves
            return Mathf.Min(dx, dy) * DiagonalCost + Mathf.Abs(dx - dy);
        }

        private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
        {
            Vector2Int lowest = openSet[0];
            float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;

            foreach (var node in openSet)
            {
                float score = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                if (score < lowestScore)
                {
                    lowestScore = score;
                    lowest = node;
                }
            }

            return lowest;
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }
    }
}
