using UnityEngine;
using UnityEngine.Tilemaps;

namespace Labyrinth.Maze
{
    [RequireComponent(typeof(Grid))]
    public class MazeRenderer : MonoBehaviour
    {
        [Header("Tilemaps (Auto-created if null)")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap floorTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase[] wallTiles;
        [SerializeField] private TileBase[] floorTiles;

        [Header("Fallback Colors (if no tiles assigned)")]
        [SerializeField] private Color wallColor = new Color(0.35f, 0.24f, 0.12f);
        [SerializeField] private Color floorColor = new Color(0.4f, 0.3f, 0.2f);

        [Header("Markers (Optional)")]
        [SerializeField] private GameObject startMarkerPrefab;
        [SerializeField] private GameObject exitMarkerPrefab;

        [Header("Layer Settings")]
        [SerializeField] private int wallLayer = 8;

        private MazeGrid _grid;
        private Transform _markersParent;
        private Tile _fallbackWallTile;
        private Tile _fallbackFloorTile;

        public Vector2 StartPosition { get; private set; }
        public Vector2 ExitPosition { get; private set; }
        public Tilemap WallTilemap => wallTilemap;
        public Tilemap FloorTilemap => floorTilemap;

        public void RenderMaze(MazeGrid grid)
        {
            _grid = grid;

            EnsureTilemapsExist();
            ClearExistingMaze();

            // Create parent for markers only
            _markersParent = new GameObject("MazeMarkers").transform;
            _markersParent.SetParent(transform);

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    var tilePosition = new Vector3Int(x, y, 0);

                    if (cell.IsWall)
                    {
                        // Pick a random wall tile for variety
                        TileBase wallTile = GetRandomTile(wallTiles, _fallbackWallTile);
                        wallTilemap.SetTile(tilePosition, wallTile);
                    }
                    else
                    {
                        // Pick a random floor tile for variety
                        TileBase floorTile = GetRandomTile(floorTiles, _fallbackFloorTile);
                        floorTilemap.SetTile(tilePosition, floorTile);

                        if (cell.IsStart)
                        {
                            // Store position at tile center
                            StartPosition = new Vector2(x + 0.5f, y + 0.5f);
                            if (startMarkerPrefab != null)
                            {
                                var position = new Vector3(x + 0.5f, y + 0.5f, 0);
                                Instantiate(startMarkerPrefab, position, Quaternion.identity, _markersParent);
                            }
                        }

                        if (cell.IsExit)
                        {
                            // Store position at tile center
                            ExitPosition = new Vector2(x + 0.5f, y + 0.5f);
                            if (exitMarkerPrefab != null)
                            {
                                var position = new Vector3(x + 0.5f, y + 0.5f, 0);
                                Instantiate(exitMarkerPrefab, position, Quaternion.identity, _markersParent);
                            }
                        }
                    }
                }
            }
        }

        private void EnsureTilemapsExist()
        {
            // Ensure Grid component exists
            Grid grid = GetComponent<Grid>();
            if (grid == null)
            {
                grid = gameObject.AddComponent<Grid>();
            }
            grid.cellSize = Vector3.one;

            // Create Floor Tilemap if not assigned
            if (floorTilemap == null)
            {
                Transform existingFloor = transform.Find("FloorTilemap");
                if (existingFloor != null)
                {
                    floorTilemap = existingFloor.GetComponent<Tilemap>();
                }
                else
                {
                    GameObject floorObj = new GameObject("FloorTilemap");
                    floorObj.transform.SetParent(transform);
                    floorObj.transform.localPosition = Vector3.zero;

                    floorTilemap = floorObj.AddComponent<Tilemap>();
                    TilemapRenderer floorRenderer = floorObj.AddComponent<TilemapRenderer>();
                    floorRenderer.sortingOrder = 0;
                }
            }

            // Create Wall Tilemap if not assigned
            if (wallTilemap == null)
            {
                Transform existingWall = transform.Find("WallTilemap");
                if (existingWall != null)
                {
                    wallTilemap = existingWall.GetComponent<Tilemap>();
                }
                else
                {
                    GameObject wallObj = new GameObject("WallTilemap");
                    wallObj.transform.SetParent(transform);
                    wallObj.transform.localPosition = Vector3.zero;
                    wallObj.layer = wallLayer;

                    wallTilemap = wallObj.AddComponent<Tilemap>();
                    TilemapRenderer wallRenderer = wallObj.AddComponent<TilemapRenderer>();
                    wallRenderer.sortingOrder = 1;

                    // Add collider for walls
                    wallObj.AddComponent<TilemapCollider2D>();
                }
            }

            // Ensure wall tilemap has collider
            if (wallTilemap.GetComponent<TilemapCollider2D>() == null)
            {
                wallTilemap.gameObject.AddComponent<TilemapCollider2D>();
            }

            // Ensure wall tilemap is on correct layer
            wallTilemap.gameObject.layer = wallLayer;

            // Create fallback tiles if no tiles assigned
            CreateFallbackTiles();
        }

        private void CreateFallbackTiles()
        {
            if ((wallTiles == null || wallTiles.Length == 0) && _fallbackWallTile == null)
            {
                _fallbackWallTile = ScriptableObject.CreateInstance<Tile>();
                _fallbackWallTile.color = wallColor;
                // Create a simple white sprite for the tile
                Texture2D tex = new Texture2D(32, 32);
                Color[] pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                _fallbackWallTile.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
                _fallbackWallTile.colliderType = Tile.ColliderType.Grid;
            }

            if ((floorTiles == null || floorTiles.Length == 0) && _fallbackFloorTile == null)
            {
                _fallbackFloorTile = ScriptableObject.CreateInstance<Tile>();
                _fallbackFloorTile.color = floorColor;
                Texture2D tex = new Texture2D(32, 32);
                Color[] pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                _fallbackFloorTile.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
                _fallbackFloorTile.colliderType = Tile.ColliderType.None;
            }
        }

        private TileBase GetRandomTile(TileBase[] tiles, Tile fallback)
        {
            if (tiles != null && tiles.Length > 0)
                return tiles[Random.Range(0, tiles.Length)];
            return fallback;
        }

        private TileBase GetRandomTile(TileBase[] tiles)
        {
            if (tiles == null || tiles.Length == 0)
                return null;
            return tiles[Random.Range(0, tiles.Length)];
        }

        private void ClearExistingMaze()
        {
            if (wallTilemap != null)
                wallTilemap.ClearAllTiles();

            if (floorTilemap != null)
                floorTilemap.ClearAllTiles();

            if (_markersParent != null)
            {
                Destroy(_markersParent.gameObject);
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

            var tilePosition = new Vector3Int(x, y, 0);

            // Remove wall tile
            wallTilemap.SetTile(tilePosition, null);

            // Add floor tile
            TileBase floorTile = GetRandomTile(floorTiles, _fallbackFloorTile);
            floorTilemap.SetTile(tilePosition, floorTile);

            // Update grid data
            cell.IsWall = false;

            return true;
        }
    }
}