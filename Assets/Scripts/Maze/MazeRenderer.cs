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
        [SerializeField] private Tilemap grassTilemap;

        [Header("Tiles")]
        [SerializeField] private TileBase[] wallTiles;
        [SerializeField] private TileBase[] floorTiles;

        [Header("Rule-Based Floor Tiles")]
        [SerializeField, Tooltip("Used for floor tiles not connected to any wall")]
        private TileBase floorTileCenter;
        [SerializeField, Tooltip("Used for floor tiles connected to wall on left or top")]
        private TileBase floorTileLeft;
        [SerializeField, Tooltip("Used for floor tiles connected to wall on right or bottom")]
        private TileBase floorTileRight;
        [SerializeField, Tooltip("Used for floor tiles connected to 2 adjacent continuous walls (corner)")]
        private TileBase floorTileCorner;

        [Header("Rule-Based Wall Tiles")]
        [SerializeField, Tooltip("Used for walls without any surrounding walls (isolated)")]
        private TileBase wallTileIsolated;
        [SerializeField, Tooltip("Used for walls with 2 adjacent continuous walls (corner), rotated to connect them")]
        private TileBase wallTileCorner;
        [SerializeField, Tooltip("Used for walls connected to only 1 other wall (end cap)")]
        private TileBase wallTileEndCap;
        [SerializeField, Tooltip("Used for walls connected to 4 adjacent walls (cross)")]
        private TileBase wallTileCross;
        [SerializeField, Tooltip("Used for walls connected to 3 adjacent walls (T-junction)")]
        private TileBase wallTileTJunction;

        [Header("Grass Tiles (Overlay)")]
        [SerializeField, Tooltip("Used for grass adjacent to 1 wall (edge)")]
        private TileBase grassTileEdge;
        [SerializeField, Tooltip("Used for grass adjacent to 2 walls (corner)")]
        private TileBase grassTileCorner;
        [SerializeField, Tooltip("Used for grass inner corners (adjacent to 2 grass tiles, not walls)")]
        private TileBase grassTileInnerCorner;
        [SerializeField] private int grassSortingOrder = 100;

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
        public Tilemap GrassTilemap => grassTilemap;

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
                        // Pick wall tile based on adjacent walls (rule-based) or random
                        var (wallTile, rotation) = GetWallTileForPosition(x, y);
                        wallTilemap.SetTile(tilePosition, wallTile);

                        // Apply rotation if needed
                        if (rotation != 0f)
                        {
                            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                            wallTilemap.SetTransformMatrix(tilePosition, matrix);
                        }
                    }
                    else
                    {
                        // Pick floor tile based on adjacent walls (rule-based) or random
                        var (floorTile, rotation) = GetFloorTileForPosition(x, y);
                        floorTilemap.SetTile(tilePosition, floorTile);

                        // Apply rotation if needed
                        if (rotation != 0f)
                        {
                            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                            floorTilemap.SetTransformMatrix(tilePosition, matrix);
                        }

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

            // Render grass overlay tiles
            RenderGrassTiles(grid);
        }

        private void RenderGrassTiles(MazeGrid grid)
        {
            if (grassTilemap == null || (grassTileEdge == null && grassTileCorner == null && grassTileInnerCorner == null))
                return;

            // Track which positions have grass tiles (for inner corner detection)
            bool[,] hasGrass = new bool[grid.Width, grid.Height];

            // First pass: Place grass tiles adjacent to walls
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);

                    // Grass only on floor tiles adjacent to walls
                    if (cell.IsWall)
                        continue;

                    var (grassTile, rotation) = GetGrassTileForPosition(x, y);
                    if (grassTile != null)
                    {
                        var tilePosition = new Vector3Int(x, y, 0);
                        grassTilemap.SetTile(tilePosition, grassTile);
                        hasGrass[x, y] = true;

                        if (rotation != 0f)
                        {
                            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                            grassTilemap.SetTransformMatrix(tilePosition, matrix);
                        }
                    }
                }
            }

            // Second pass: Place inner corner grass tiles
            if (grassTileInnerCorner != null)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    for (int y = 0; y < grid.Height; y++)
                    {
                        // Skip if already has grass or is a wall
                        if (hasGrass[x, y] || grid.GetCell(x, y).IsWall)
                            continue;

                        var (innerCornerTile, rotation) = GetGrassInnerCornerForPosition(x, y, hasGrass, grid);
                        if (innerCornerTile != null)
                        {
                            var tilePosition = new Vector3Int(x, y, 0);
                            grassTilemap.SetTile(tilePosition, innerCornerTile);

                            if (rotation != 0f)
                            {
                                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, rotation), Vector3.one);
                                grassTilemap.SetTransformMatrix(tilePosition, matrix);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines grass tile and rotation based on adjacent walls.
        /// - 1 adjacent wall → grassTileEdge with rotation
        /// - 2 adjacent continuous walls (corner) → grassTileCorner with rotation
        /// </summary>
        private (TileBase tile, float rotation) GetGrassTileForPosition(int x, int y)
        {
            bool leftIsWall = IsWallAt(x - 1, y);
            bool rightIsWall = IsWallAt(x + 1, y);
            bool topIsWall = IsWallAt(x, y + 1);
            bool bottomIsWall = IsWallAt(x, y - 1);

            int adjacentWallCount = (leftIsWall ? 1 : 0) + (rightIsWall ? 1 : 0) +
                                    (topIsWall ? 1 : 0) + (bottomIsWall ? 1 : 0);

            // Corner case: 2 adjacent continuous walls
            if (adjacentWallCount == 2 && grassTileCorner != null)
            {
                // Top + Left corner
                if (topIsWall && leftIsWall)
                {
                    return (grassTileCorner, 0f);
                }
                // Left + Bottom corner
                if (leftIsWall && bottomIsWall)
                {
                    return (grassTileCorner, 90f);
                }
                // Bottom + Right corner
                if (bottomIsWall && rightIsWall)
                {
                    return (grassTileCorner, 180f);
                }
                // Right + Top corner
                if (rightIsWall && topIsWall)
                {
                    return (grassTileCorner, -90f);
                }
            }

            // Edge case: 1 adjacent wall
            if (adjacentWallCount == 1 && grassTileEdge != null)
            {
                // Wall on right - no rotation
                if (rightIsWall)
                {
                    return (grassTileEdge, 0f);
                }
                // Wall on top - 90 rotation
                if (topIsWall)
                {
                    return (grassTileEdge, 90f);
                }
                // Wall on left - 180 rotation
                if (leftIsWall)
                {
                    return (grassTileEdge, 180f);
                }
                // Wall on bottom - -90 rotation
                if (bottomIsWall)
                {
                    return (grassTileEdge, -90f);
                }
            }

            return (null, 0f);
        }

        /// <summary>
        /// Determines inner corner grass tile for positions adjacent to 2 continuous grass tiles (not walls).
        /// </summary>
        private (TileBase tile, float rotation) GetGrassInnerCornerForPosition(int x, int y, bool[,] hasGrass, MazeGrid grid)
        {
            // Check adjacent grass tiles
            bool leftHasGrass = x > 0 && hasGrass[x - 1, y];
            bool rightHasGrass = x < grid.Width - 1 && hasGrass[x + 1, y];
            bool topHasGrass = y < grid.Height - 1 && hasGrass[x, y + 1];
            bool bottomHasGrass = y > 0 && hasGrass[x, y - 1];

            int adjacentGrassCount = (leftHasGrass ? 1 : 0) + (rightHasGrass ? 1 : 0) +
                                     (topHasGrass ? 1 : 0) + (bottomHasGrass ? 1 : 0);

            // Inner corner: 2 adjacent continuous grass tiles
            if (adjacentGrassCount == 2)
            {
                // Right + Bottom grass - original rotation
                if (rightHasGrass && bottomHasGrass)
                {
                    return (grassTileInnerCorner, 0f);
                }
                // Right + Top grass - 90 rotation
                if (rightHasGrass && topHasGrass)
                {
                    return (grassTileInnerCorner, 90f);
                }
                // Top + Left grass - 180 rotation
                if (topHasGrass && leftHasGrass)
                {
                    return (grassTileInnerCorner, 180f);
                }
                // Left + Bottom grass - -90 rotation
                if (leftHasGrass && bottomHasGrass)
                {
                    return (grassTileInnerCorner, -90f);
                }
            }

            return (null, 0f);
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

            // Create Grass Tilemap if not assigned (renders above player, no collision)
            if (grassTilemap == null)
            {
                Transform existingGrass = transform.Find("GrassTilemap");
                if (existingGrass != null)
                {
                    grassTilemap = existingGrass.GetComponent<Tilemap>();
                }
                else
                {
                    GameObject grassObj = new GameObject("GrassTilemap");
                    grassObj.transform.SetParent(transform);
                    grassObj.transform.localPosition = Vector3.zero;

                    grassTilemap = grassObj.AddComponent<Tilemap>();
                    TilemapRenderer grassRenderer = grassObj.AddComponent<TilemapRenderer>();
                    grassRenderer.sortingOrder = grassSortingOrder; // High sorting order to render above player
                    // No collider - player passes through
                }
            }
            else
            {
                // Ensure grass tilemap has correct sorting order
                var grassRenderer = grassTilemap.GetComponent<TilemapRenderer>();
                if (grassRenderer != null)
                {
                    grassRenderer.sortingOrder = grassSortingOrder;
                }
            }

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

        /// <summary>
        /// Determines the appropriate floor tile and rotation based on adjacent walls.
        /// - 2 adjacent continuous walls (corner) → floorTileCorner with rotation
        /// - Left wall → floorTileLeft (no rotation)
        /// - Top wall → floorTileLeft rotated 90° CW to face down
        /// - Right wall → floorTileRight (no rotation)
        /// - Bottom wall → floorTileRight rotated 90° CW to face up
        /// - No adjacent walls → floorTileCenter
        /// Falls back to random floor tile if rule tiles aren't assigned.
        /// </summary>
        private (TileBase tile, float rotation) GetFloorTileForPosition(int x, int y)
        {
            // Check if rule-based tiles are assigned
            bool hasRuleTiles = floorTileCenter != null || floorTileLeft != null || floorTileRight != null || floorTileCorner != null;

            if (!hasRuleTiles)
            {
                // Fall back to random floor tile
                return (GetRandomTile(floorTiles, _fallbackFloorTile), 0f);
            }

            // Check adjacent cells for walls
            bool leftIsWall = IsWallAt(x - 1, y);
            bool rightIsWall = IsWallAt(x + 1, y);
            bool topIsWall = IsWallAt(x, y + 1);
            bool bottomIsWall = IsWallAt(x, y - 1);

            // Count adjacent walls
            int adjacentWallCount = (leftIsWall ? 1 : 0) + (rightIsWall ? 1 : 0) +
                                    (topIsWall ? 1 : 0) + (bottomIsWall ? 1 : 0);

            // Check for corner cases (2 adjacent continuous walls) - highest priority
            if (adjacentWallCount == 2 && floorTileCorner != null)
            {
                // Bottom-Left corner (walls below and to the left)
                if (bottomIsWall && leftIsWall)
                {
                    return (floorTileCorner, -90f);
                }
                // Left-Top corner (walls to the left and above)
                if (leftIsWall && topIsWall)
                {
                    return (floorTileCorner, -180f);
                }
                // Top-Right corner (walls above and to the right)
                if (topIsWall && rightIsWall)
                {
                    return (floorTileCorner, 90f);
                }
                // Right-Bottom corner (walls to the right and below)
                if (rightIsWall && bottomIsWall)
                {
                    return (floorTileCorner, 0f);
                }
            }

            // Priority order: left, top, right, bottom
            // Left wall - use floorTileLeft without rotation
            if (leftIsWall)
            {
                return (floorTileLeft ?? GetRandomTile(floorTiles, _fallbackFloorTile), 0f);
            }

            // Top wall - use floorTileLeft rotated 90° clockwise (facing down)
            if (topIsWall)
            {
                return (floorTileLeft ?? GetRandomTile(floorTiles, _fallbackFloorTile), -90f);
            }

            // Right wall - use floorTileRight without rotation
            if (rightIsWall)
            {
                return (floorTileRight ?? GetRandomTile(floorTiles, _fallbackFloorTile), 0f);
            }

            // Bottom wall - use floorTileRight rotated 90° clockwise (facing up)
            if (bottomIsWall)
            {
                return (floorTileRight ?? GetRandomTile(floorTiles, _fallbackFloorTile), -90f);
            }

            // No adjacent walls - use center tile
            return (floorTileCenter ?? GetRandomTile(floorTiles, _fallbackFloorTile), 0f);
        }

        /// <summary>
        /// Determines the appropriate wall tile and rotation based on adjacent walls.
        /// - No adjacent walls → wallTileIsolated
        /// - 4 adjacent walls (cross) → wallTileCross
        /// - 3 adjacent walls (T-junction) → wallTileTJunction with rotation
        /// - 1 adjacent wall (end cap) → wallTileEndCap with rotation
        /// - 2 adjacent opposite walls (straight line) → wallTileIsolated with rotation
        /// - 2 adjacent continuous walls (corner) → wallTileCorner with rotation
        /// - Otherwise → random wall tile
        /// </summary>
        private (TileBase tile, float rotation) GetWallTileForPosition(int x, int y)
        {
            // Check if rule-based tiles are assigned
            bool hasRuleTiles = wallTileIsolated != null || wallTileCorner != null || wallTileEndCap != null || wallTileCross != null || wallTileTJunction != null;

            if (!hasRuleTiles)
            {
                // Fall back to random wall tile
                return (GetRandomTile(wallTiles, _fallbackWallTile), 0f);
            }

            // Check adjacent cells for walls (only cardinal directions)
            bool leftIsWall = IsWallAt(x - 1, y);
            bool rightIsWall = IsWallAt(x + 1, y);
            bool topIsWall = IsWallAt(x, y + 1);
            bool bottomIsWall = IsWallAt(x, y - 1);

            // Count adjacent walls
            int adjacentWallCount = (leftIsWall ? 1 : 0) + (rightIsWall ? 1 : 0) +
                                    (topIsWall ? 1 : 0) + (bottomIsWall ? 1 : 0);

            // No adjacent walls - use isolated tile
            if (adjacentWallCount == 0)
            {
                return (wallTileIsolated ?? GetRandomTile(wallTiles, _fallbackWallTile), 0f);
            }

            // 4 adjacent walls (cross) - use cross tile
            if (adjacentWallCount == 4 && wallTileCross != null)
            {
                return (wallTileCross, 0f);
            }

            // 3 adjacent walls (T-junction) - use T-junction tile with rotation
            if (adjacentWallCount == 3 && wallTileTJunction != null)
            {
                // Left + Bottom + Top (no right) - base rotation
                if (leftIsWall && bottomIsWall && topIsWall && !rightIsWall)
                {
                    return (wallTileTJunction, 0f);
                }
                // Bottom + Top + Right (no left)
                if (bottomIsWall && topIsWall && rightIsWall && !leftIsWall)
                {
                    return (wallTileTJunction, 180f);
                }
                // Left + Right + Top (no bottom)
                if (leftIsWall && rightIsWall && topIsWall && !bottomIsWall)
                {
                    return (wallTileTJunction, -90f);
                }
                // Left + Right + Bottom (no top)
                if (leftIsWall && rightIsWall && bottomIsWall && !topIsWall)
                {
                    return (wallTileTJunction, 90f);
                }
            }

            // 1 adjacent wall (end cap) - use end cap tile with rotation
            if (adjacentWallCount == 1 && wallTileEndCap != null)
            {
                // Bottom connection - no rotation
                if (bottomIsWall)
                {
                    return (wallTileEndCap, 0f);
                }
                // Top connection - rotate -180°
                if (topIsWall)
                {
                    return (wallTileEndCap, -180f);
                }
                // Left connection - rotate -90°
                if (leftIsWall)
                {
                    return (wallTileEndCap, -90f);
                }
                // Right connection - rotate 90°
                if (rightIsWall)
                {
                    return (wallTileEndCap, 90f);
                }
            }

            // Check for straight line walls (2 walls on opposite sides)
            if (adjacentWallCount == 2 && wallTileIsolated != null)
            {
                // Horizontal line (walls on left and right)
                if (leftIsWall && rightIsWall && !topIsWall && !bottomIsWall)
                {
                    return (wallTileIsolated, -90f);
                }
                // Vertical line (walls on top and bottom)
                if (topIsWall && bottomIsWall && !leftIsWall && !rightIsWall)
                {
                    return (wallTileIsolated, 0f);
                }
            }

            // Check for corner cases (2 adjacent continuous walls)
            if (adjacentWallCount == 2 && wallTileCorner != null)
            {
                // Bottom-Left corner (walls below and to the left)
                if (bottomIsWall && leftIsWall)
                {
                    return (wallTileCorner, -90f);
                }
                // Left-Top corner (walls to the left and above)
                if (leftIsWall && topIsWall)
                {
                    return (wallTileCorner, -180f);
                }
                // Top-Right corner (walls above and to the right)
                if (topIsWall && rightIsWall)
                {
                    return (wallTileCorner, 90f);
                }
                // Right-Bottom corner (walls to the right and below)
                if (rightIsWall && bottomIsWall)
                {
                    return (wallTileCorner, 0f);
                }
            }

            // For all other cases, use random wall tile
            return (GetRandomTile(wallTiles, _fallbackWallTile), 0f);
        }

        /// <summary>
        /// Checks if the cell at the given position is a wall or out of bounds.
        /// </summary>
        private bool IsWallAt(int x, int y)
        {
            if (_grid == null || !_grid.IsInBounds(x, y))
                return true; // Treat out of bounds as wall

            return _grid.GetCell(x, y).IsWall;
        }

        private void ClearExistingMaze()
        {
            if (wallTilemap != null)
                wallTilemap.ClearAllTiles();

            if (floorTilemap != null)
                floorTilemap.ClearAllTiles();

            if (grassTilemap != null)
                grassTilemap.ClearAllTiles();

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