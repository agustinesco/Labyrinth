using UnityEngine;

namespace Labyrinth.Maze
{
    public class MazeRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject startMarkerPrefab;
        [SerializeField] private GameObject exitMarkerPrefab;

        private MazeGrid _grid;
        private Transform _tilesParent;

        public Vector2 StartPosition { get; private set; }
        public Vector2 ExitPosition { get; private set; }

        public void RenderMaze(MazeGrid grid)
        {
            _grid = grid;
            ClearExistingMaze();

            _tilesParent = new GameObject("MazeTiles").transform;
            _tilesParent.SetParent(transform);

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    var position = new Vector3(x, y, 0);

                    if (cell.IsWall)
                    {
                        Instantiate(wallPrefab, position, Quaternion.identity, _tilesParent);
                    }
                    else
                    {
                        Instantiate(floorPrefab, position, Quaternion.identity, _tilesParent);

                        if (cell.IsStart)
                        {
                            StartPosition = new Vector2(x, y);
                            if (startMarkerPrefab != null)
                            {
                                Instantiate(startMarkerPrefab, position, Quaternion.identity, _tilesParent);
                            }
                        }

                        if (cell.IsExit)
                        {
                            ExitPosition = new Vector2(x, y);
                            if (exitMarkerPrefab != null)
                            {
                                Instantiate(exitMarkerPrefab, position, Quaternion.identity, _tilesParent);
                            }
                        }
                    }
                }
            }
        }

        private void ClearExistingMaze()
        {
            if (_tilesParent != null)
            {
                Destroy(_tilesParent.gameObject);
            }
        }

        public MazeGrid GetGrid()
        {
            return _grid;
        }

        /// <summary>
        /// Destroys a wall at the specified position and replaces it with a floor.
        /// Returns true if a wall was destroyed.
        /// </summary>
        public bool DestroyWallAt(int x, int y)
        {
            if (_grid == null || !_grid.IsInBounds(x, y))
                return false;

            var cell = _grid.GetCell(x, y);
            if (!cell.IsWall)
                return false;

            // Find and destroy the wall object at this position
            var position = new Vector3(x, y, 0);
            foreach (Transform child in _tilesParent)
            {
                if (Vector3.Distance(child.position, position) < 0.1f)
                {
                    // Check if it's a wall (has collider on wall layer)
                    var collider = child.GetComponent<Collider2D>();
                    if (collider != null && child.gameObject.layer == 8) // Layer 8 = Wall
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }

            // Update grid data
            cell.IsWall = false;

            // Spawn floor tile
            Instantiate(floorPrefab, position, Quaternion.identity, _tilesParent);

            return true;
        }
    }
}