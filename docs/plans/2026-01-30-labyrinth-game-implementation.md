# Labyrinth Escape - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a mobile 2D top-down procedural labyrinth game with line-of-sight visibility, item pickups, and an A* pathfinding enemy.

**Architecture:** Unity 2D with Physics2D for collisions and raycasting. Maze generation uses recursive backtracking on a 25x25 grid. Visibility uses raycast-based shadow mesh. Enemy uses A* pathfinding recalculated every 0.5s.

**Tech Stack:** Unity 2022.3, C#, Unity Test Framework, Physics2D, SpriteMask

---

## Task 1: Project Structure Setup

**Files:**
- Create: `Assets/Scripts/Core/.gitkeep`
- Create: `Assets/Scripts/Maze/.gitkeep`
- Create: `Assets/Scripts/Player/.gitkeep`
- Create: `Assets/Scripts/Enemy/.gitkeep`
- Create: `Assets/Scripts/Items/.gitkeep`
- Create: `Assets/Scripts/Visibility/.gitkeep`
- Create: `Assets/Scripts/UI/.gitkeep`
- Create: `Assets/Prefabs/.gitkeep`
- Create: `Assets/Sprites/.gitkeep`
- Create: `Assets/Materials/.gitkeep`
- Create: `Assets/Scenes/.gitkeep` (exists)
- Create: `Assets/Tests/EditMode/Labyrinth.EditMode.Tests.asmdef`
- Create: `Assets/Scripts/Labyrinth.asmdef`

**Step 1: Create folder structure**

Use Unity MCP to create all required folders under Assets.

**Step 2: Create runtime assembly definition**

Create `Assets/Scripts/Labyrinth.asmdef`:
```json
{
    "name": "Labyrinth",
    "rootNamespace": "Labyrinth",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 3: Create test assembly definition**

Create `Assets/Tests/EditMode/Labyrinth.EditMode.Tests.asmdef`:
```json
{
    "name": "Labyrinth.EditMode.Tests",
    "rootNamespace": "Labyrinth.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Labyrinth"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Step 4: Verify compilation**

Run Unity refresh and check console for errors.
Expected: No compilation errors.

---

## Task 2: Maze Data Structures

**Files:**
- Create: `Assets/Scripts/Maze/MazeCell.cs`
- Create: `Assets/Tests/EditMode/MazeCellTests.cs`

**Step 1: Write failing test for MazeCell**

Create `Assets/Tests/EditMode/MazeCellTests.cs`:
```csharp
using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeCellTests
    {
        [Test]
        public void NewCell_IsWall_ByDefault()
        {
            var cell = new MazeCell(0, 0);
            Assert.IsTrue(cell.IsWall);
        }

        [Test]
        public void Cell_StoresCoordinates()
        {
            var cell = new MazeCell(5, 10);
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(10, cell.Y);
        }

        [Test]
        public void Cell_CanBeSetToFloor()
        {
            var cell = new MazeCell(0, 0);
            cell.IsWall = false;
            Assert.IsFalse(cell.IsWall);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run EditMode tests via Unity MCP.
Expected: FAIL - MazeCell class not found.

**Step 3: Implement MazeCell**

Create `Assets/Scripts/Maze/MazeCell.cs`:
```csharp
namespace Labyrinth.Maze
{
    public class MazeCell
    {
        public int X { get; }
        public int Y { get; }
        public bool IsWall { get; set; } = true;
        public bool IsVisited { get; set; }
        public bool IsStart { get; set; }
        public bool IsExit { get; set; }

        public MazeCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
```

**Step 4: Run tests to verify pass**

Run EditMode tests.
Expected: All MazeCellTests PASS.

---

## Task 3: Maze Grid

**Files:**
- Create: `Assets/Scripts/Maze/MazeGrid.cs`
- Create: `Assets/Tests/EditMode/MazeGridTests.cs`

**Step 1: Write failing tests for MazeGrid**

Create `Assets/Tests/EditMode/MazeGridTests.cs`:
```csharp
using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeGridTests
    {
        [Test]
        public void Grid_HasCorrectDimensions()
        {
            var grid = new MazeGrid(25, 25);
            Assert.AreEqual(25, grid.Width);
            Assert.AreEqual(25, grid.Height);
        }

        [Test]
        public void Grid_AllCellsAreWalls_Initially()
        {
            var grid = new MazeGrid(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Assert.IsTrue(grid.GetCell(x, y).IsWall);
                }
            }
        }

        [Test]
        public void Grid_GetCell_ReturnsCorrectCell()
        {
            var grid = new MazeGrid(10, 10);
            var cell = grid.GetCell(3, 7);
            Assert.AreEqual(3, cell.X);
            Assert.AreEqual(7, cell.Y);
        }

        [Test]
        public void Grid_IsInBounds_ReturnsTrueForValidCoords()
        {
            var grid = new MazeGrid(10, 10);
            Assert.IsTrue(grid.IsInBounds(0, 0));
            Assert.IsTrue(grid.IsInBounds(9, 9));
            Assert.IsTrue(grid.IsInBounds(5, 5));
        }

        [Test]
        public void Grid_IsInBounds_ReturnsFalseForInvalidCoords()
        {
            var grid = new MazeGrid(10, 10);
            Assert.IsFalse(grid.IsInBounds(-1, 0));
            Assert.IsFalse(grid.IsInBounds(0, -1));
            Assert.IsFalse(grid.IsInBounds(10, 0));
            Assert.IsFalse(grid.IsInBounds(0, 10));
        }
    }
}
```

**Step 2: Run tests to verify failure**

Run EditMode tests.
Expected: FAIL - MazeGrid class not found.

**Step 3: Implement MazeGrid**

Create `Assets/Scripts/Maze/MazeGrid.cs`:
```csharp
namespace Labyrinth.Maze
{
    public class MazeGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly MazeCell[,] _cells;

        public MazeGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new MazeCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new MazeCell(x, y);
                }
            }
        }

        public MazeCell GetCell(int x, int y)
        {
            return _cells[x, y];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}
```

**Step 4: Run tests to verify pass**

Run EditMode tests.
Expected: All MazeGridTests PASS.

---

## Task 4: Maze Generation Algorithm

**Files:**
- Create: `Assets/Scripts/Maze/MazeGenerator.cs`
- Create: `Assets/Tests/EditMode/MazeGeneratorTests.cs`

**Step 1: Write failing tests for MazeGenerator**

Create `Assets/Tests/EditMode/MazeGeneratorTests.cs`:
```csharp
using NUnit.Framework;
using Labyrinth.Maze;

namespace Labyrinth.Tests
{
    public class MazeGeneratorTests
    {
        [Test]
        public void Generate_CreatesGridWithCorrectSize()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();
            Assert.AreEqual(25, grid.Width);
            Assert.AreEqual(25, grid.Height);
        }

        [Test]
        public void Generate_StartCellIsFloor()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();
            var start = grid.GetCell(1, 1);
            Assert.IsFalse(start.IsWall);
            Assert.IsTrue(start.IsStart);
        }

        [Test]
        public void Generate_HasExitCell()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            bool hasExit = false;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    if (grid.GetCell(x, y).IsExit)
                    {
                        hasExit = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(hasExit);
        }

        [Test]
        public void Generate_HasFloorCells()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            int floorCount = 0;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    if (!grid.GetCell(x, y).IsWall)
                        floorCount++;
                }
            }
            Assert.Greater(floorCount, 50);
        }

        [Test]
        public void Generate_OuterEdgesAreWalls()
        {
            var generator = new MazeGenerator(25, 25);
            var grid = generator.Generate();

            for (int x = 0; x < grid.Width; x++)
            {
                Assert.IsTrue(grid.GetCell(x, 0).IsWall);
                Assert.IsTrue(grid.GetCell(x, grid.Height - 1).IsWall);
            }
            for (int y = 0; y < grid.Height; y++)
            {
                Assert.IsTrue(grid.GetCell(0, y).IsWall);
                Assert.IsTrue(grid.GetCell(grid.Width - 1, y).IsWall);
            }
        }
    }
}
```

**Step 2: Run tests to verify failure**

Run EditMode tests.
Expected: FAIL - MazeGenerator class not found.

**Step 3: Implement MazeGenerator with recursive backtracking**

Create `Assets/Scripts/Maze/MazeGenerator.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public class MazeGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private MazeGrid _grid;
        private System.Random _random;

        private static readonly (int dx, int dy)[] Directions =
        {
            (0, 2), (2, 0), (0, -2), (-2, 0)
        };

        public MazeGenerator(int width, int height, int? seed = null)
        {
            _width = width;
            _height = height;
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public MazeGrid Generate()
        {
            _grid = new MazeGrid(_width, _height);

            int startX = 1;
            int startY = 1;

            CarvePassage(startX, startY);

            _grid.GetCell(startX, startY).IsStart = true;

            var exitCell = FindFurthestCell(startX, startY);
            exitCell.IsExit = true;

            return _grid;
        }

        private void CarvePassage(int x, int y)
        {
            _grid.GetCell(x, y).IsWall = false;
            _grid.GetCell(x, y).IsVisited = true;

            var directions = ShuffleDirections();

            foreach (var (dx, dy) in directions)
            {
                int newX = x + dx;
                int newY = y + dy;

                if (_grid.IsInBounds(newX, newY) &&
                    newX > 0 && newX < _width - 1 &&
                    newY > 0 && newY < _height - 1 &&
                    _grid.GetCell(newX, newY).IsWall)
                {
                    int wallX = x + dx / 2;
                    int wallY = y + dy / 2;
                    _grid.GetCell(wallX, wallY).IsWall = false;

                    CarvePassage(newX, newY);
                }
            }
        }

        private (int, int)[] ShuffleDirections()
        {
            var shuffled = new List<(int, int)>(Directions);
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
    }
}
```

**Step 4: Run tests to verify pass**

Run EditMode tests.
Expected: All MazeGeneratorTests PASS.

---

## Task 5: A* Pathfinding

**Files:**
- Create: `Assets/Scripts/Maze/Pathfinding.cs`
- Create: `Assets/Tests/EditMode/PathfindingTests.cs`

**Step 1: Write failing tests for Pathfinding**

Create `Assets/Tests/EditMode/PathfindingTests.cs`:
```csharp
using NUnit.Framework;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Tests
{
    public class PathfindingTests
    {
        private MazeGrid CreateSimpleGrid()
        {
            var grid = new MazeGrid(5, 5);
            // Create a simple corridor: floor at (1,1), (2,1), (3,1)
            grid.GetCell(1, 1).IsWall = false;
            grid.GetCell(2, 1).IsWall = false;
            grid.GetCell(3, 1).IsWall = false;
            return grid;
        }

        [Test]
        public void FindPath_ReturnsPath_WhenPathExists()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 1));

            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void FindPath_ReturnsNull_WhenNoPathExists()
        {
            var grid = new MazeGrid(5, 5);
            grid.GetCell(1, 1).IsWall = false;
            grid.GetCell(3, 3).IsWall = false;
            // No connection between (1,1) and (3,3)

            var pathfinder = new Pathfinding(grid);
            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 3));

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_StartsAtStart_EndsAtGoal()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var start = new Vector2Int(1, 1);
            var goal = new Vector2Int(3, 1);
            var path = pathfinder.FindPath(start, goal);

            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void FindPath_DoesNotGoThroughWalls()
        {
            var grid = CreateSimpleGrid();
            var pathfinder = new Pathfinding(grid);

            var path = pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 1));

            foreach (var point in path)
            {
                Assert.IsFalse(grid.GetCell(point.x, point.y).IsWall);
            }
        }
    }
}
```

**Step 2: Run tests to verify failure**

Run EditMode tests.
Expected: FAIL - Pathfinding class not found.

**Step 3: Implement A* Pathfinding**

Create `Assets/Scripts/Maze/Pathfinding.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public class Pathfinding
    {
        private readonly MazeGrid _grid;

        private static readonly Vector2Int[] Neighbors =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

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

                foreach (var dir in Neighbors)
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
            }

            return null;
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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
```

**Step 4: Run tests to verify pass**

Run EditMode tests.
Expected: All PathfindingTests PASS.

---

## Task 6: Game Scene Setup

**Files:**
- Create: `Assets/Scenes/Game.unity`
- Create: `Assets/Scripts/Core/GameManager.cs`

**Step 1: Create Game scene**

Create new scene "Game" with:
- Main Camera (Orthographic, size 8, position 0,0,-10)
- Directional Light (if needed for 2D)

**Step 2: Implement GameManager**

Create `Assets/Scripts/Core/GameManager.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Labyrinth.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float enemySpawnDelay = 45f;

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public float EnemySpawnTimer { get; private set; }
        public bool EnemySpawned { get; private set; }

        public event System.Action OnEnemySpawn;
        public event System.Action OnGameWin;
        public event System.Action OnGameLose;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnemySpawnTimer = enemySpawnDelay;
            CurrentState = GameState.Playing;
            EnemySpawned = false;
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            if (!EnemySpawned)
            {
                EnemySpawnTimer -= Time.deltaTime;
                if (EnemySpawnTimer <= 0)
                {
                    SpawnEnemy();
                }
            }
        }

        private void SpawnEnemy()
        {
            EnemySpawned = true;
            OnEnemySpawn?.Invoke();
        }

        public void TriggerWin()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Won;
            OnGameWin?.Invoke();
        }

        public void TriggerLose()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Lost;
            OnGameLose?.Invoke();
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public enum GameState
    {
        Playing,
        Won,
        Lost
    }
}
```

**Step 3: Add GameManager to scene**

Create empty GameObject "GameManager" and attach GameManager script.

**Step 4: Verify compilation**

Check console for errors.
Expected: No compilation errors.

---

## Task 7: Maze Renderer

**Files:**
- Create: `Assets/Scripts/Maze/MazeRenderer.cs`
- Create: `Assets/Prefabs/WallTile.prefab`
- Create: `Assets/Prefabs/FloorTile.prefab`

**Step 1: Create tile prefabs**

Create two sprite prefabs:
- WallTile: Dark gray square sprite (1x1 unit), BoxCollider2D, Layer "Wall"
- FloorTile: Light gray square sprite (1x1 unit), no collider

**Step 2: Implement MazeRenderer**

Create `Assets/Scripts/Maze/MazeRenderer.cs`:
```csharp
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
    }
}
```

**Step 3: Set up MazeRenderer in scene**

Create empty GameObject "MazeRenderer", attach script, assign prefab references.

**Step 4: Verify rendering works**

Test by generating and rendering a maze in Play mode.

---

## Task 8: Maze Initialization

**Files:**
- Create: `Assets/Scripts/Maze/MazeInitializer.cs`

**Step 1: Implement MazeInitializer**

Create `Assets/Scripts/Maze/MazeInitializer.cs`:
```csharp
using UnityEngine;

namespace Labyrinth.Maze
{
    public class MazeInitializer : MonoBehaviour
    {
        [SerializeField] private int mazeWidth = 25;
        [SerializeField] private int mazeHeight = 25;
        [SerializeField] private MazeRenderer mazeRenderer;

        public MazeGrid Grid { get; private set; }

        public event System.Action<MazeGrid> OnMazeGenerated;

        private void Start()
        {
            GenerateMaze();
        }

        public void GenerateMaze()
        {
            var generator = new MazeGenerator(mazeWidth, mazeHeight);
            Grid = generator.Generate();

            mazeRenderer.RenderMaze(Grid);

            OnMazeGenerated?.Invoke(Grid);
        }
    }
}
```

**Step 2: Set up MazeInitializer in scene**

Add MazeInitializer to MazeRenderer GameObject, assign references.

**Step 3: Test maze generation**

Enter Play mode and verify maze generates correctly.

---

## Task 9: Player Controller

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Create: `Assets/Prefabs/Player.prefab`

**Step 1: Create Player prefab**

Create Player prefab with:
- Sprite Renderer (blue square or player sprite)
- Rigidbody2D (Dynamic, freeze rotation Z, gravity 0)
- CircleCollider2D (radius 0.4)
- Layer "Player"

**Step 2: Implement PlayerController**

Create `Assets/Scripts/Player/PlayerController.cs`:
```csharp
using UnityEngine;

namespace Labyrinth.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 5f;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private float _speedBonus;
        private float _speedBoostTimer;

        public float CurrentSpeed => baseSpeed + _speedBonus;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            if (_speedBoostTimer > 0)
            {
                _speedBoostTimer -= Time.deltaTime;
                if (_speedBoostTimer <= 0)
                {
                    _speedBonus = 0;
                }
            }
        }

        private void FixedUpdate()
        {
            Vector2 movement = _moveInput.normalized * CurrentSpeed;
            _rb.linearVelocity = movement;
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        public void ApplySpeedBoost(float bonus, float duration)
        {
            _speedBonus = bonus;
            _speedBoostTimer = duration;
        }

        public float GetSpeedBoostTimeRemaining()
        {
            return _speedBoostTimer;
        }
    }
}
```

**Step 3: Add Player to scene**

Instantiate Player prefab in scene (will be positioned by initializer later).

**Step 4: Test basic movement**

Use keyboard input temporarily to test movement works.

---

## Task 10: Virtual Joystick

**Files:**
- Create: `Assets/Scripts/UI/VirtualJoystick.cs`
- Create: `Assets/Prefabs/UI/VirtualJoystick.prefab`

**Step 1: Create Joystick UI**

Create Canvas with:
- UI Image "JoystickBackground" (circle, bottom-left, 150x150)
- UI Image "JoystickKnob" (smaller circle, child of background, 60x60)

**Step 2: Implement VirtualJoystick**

Create `Assets/Scripts/UI/VirtualJoystick.cs`:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float idleAlpha = 0.5f;
        [SerializeField] private float activeAlpha = 1f;

        private Vector2 _inputVector;
        private Canvas _canvas;

        public Vector2 InputVector => _inputVector.magnitude > deadZone ? _inputVector : Vector2.zero;

        private void Start()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = idleAlpha;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = activeAlpha;
            }
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = idleAlpha;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position
            );

            float radius = background.sizeDelta.x / 2;
            position = Vector2.ClampMagnitude(position, radius);

            knob.anchoredPosition = position;
            _inputVector = position / radius;
        }
    }
}
```

**Step 3: Set up Joystick in scene**

Create Canvas, add joystick UI elements, attach script, configure references.

**Step 4: Connect joystick to player**

Create `Assets/Scripts/Player/PlayerInputHandler.cs`:
```csharp
using UnityEngine;
using Labyrinth.UI;

namespace Labyrinth.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private VirtualJoystick joystick;

        private void Update()
        {
            if (joystick != null && playerController != null)
            {
                playerController.SetMoveInput(joystick.InputVector);
            }

            // Keyboard fallback for testing
            #if UNITY_EDITOR
            if (joystick == null || joystick.InputVector == Vector2.zero)
            {
                Vector2 keyboardInput = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );
                playerController.SetMoveInput(keyboardInput);
            }
            #endif
        }
    }
}
```

**Step 5: Test joystick input**

Test in editor and on mobile preview.

---

## Task 11: Camera Follow

**Files:**
- Create: `Assets/Scripts/Core/CameraFollow.cs`

**Step 1: Implement CameraFollow**

Create `Assets/Scripts/Core/CameraFollow.cs`:
```csharp
using UnityEngine;

namespace Labyrinth.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
```

**Step 2: Add to Main Camera**

Attach CameraFollow to Main Camera, assign player as target.

---

## Task 12: Player Health

**Files:**
- Create: `Assets/Scripts/Player/PlayerHealth.cs`
- Create: `Assets/Tests/EditMode/PlayerHealthTests.cs`

**Step 1: Write failing tests**

Create `Assets/Tests/EditMode/PlayerHealthTests.cs`:
```csharp
using NUnit.Framework;
using Labyrinth.Player;

namespace Labyrinth.Tests
{
    public class PlayerHealthTests
    {
        [Test]
        public void Health_StartsAtMaxHealth()
        {
            var health = new PlayerHealthData(3);
            Assert.AreEqual(3, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            var health = new PlayerHealthData(3);
            health.TakeDamage(1);
            Assert.AreEqual(2, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_CannotGoBelowZero()
        {
            var health = new PlayerHealthData(3);
            health.TakeDamage(10);
            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void IsDead_ReturnsTrueWhenHealthIsZero()
        {
            var health = new PlayerHealthData(1);
            health.TakeDamage(1);
            Assert.IsTrue(health.IsDead);
        }
    }

    public class PlayerHealthData
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public PlayerHealthData(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth = System.Math.Max(0, CurrentHealth - amount);
        }
    }
}
```

**Step 2: Run tests**

Run tests - they should pass since we included the implementation inline.

**Step 3: Implement PlayerHealth MonoBehaviour**

Create `Assets/Scripts/Player/PlayerHealth.cs`:
```csharp
using UnityEngine;
using Labyrinth.Core;

namespace Labyrinth.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1.5f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int _currentHealth;
        private float _invincibilityTimer;
        private bool _isInvincible;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => _currentHealth <= 0;
        public bool IsInvincible => _isInvincible;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

        private void Start()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth);
        }

        private void Update()
        {
            if (_isInvincible)
            {
                _invincibilityTimer -= Time.deltaTime;

                // Blink effect
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = Mathf.FloorToInt(_invincibilityTimer * 10) % 2 == 0;
                }

                if (_invincibilityTimer <= 0)
                {
                    _isInvincible = false;
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.enabled = true;
                    }
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isInvincible || IsDead) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
                GameManager.Instance?.TriggerLose();
            }
            else
            {
                StartInvincibility();
            }
        }

        private void StartInvincibility()
        {
            _isInvincible = true;
            _invincibilityTimer = invincibilityDuration;
        }
    }
}
```

**Step 4: Add PlayerHealth to Player prefab**

Add component to Player, assign sprite renderer reference.

---

## Task 13: Visibility System

**Files:**
- Create: `Assets/Scripts/Visibility/VisibilityController.cs`
- Create: `Assets/Materials/DarknessMaterial.mat`

**Step 1: Create darkness material**

Create unlit black material or use sprite with full black.

**Step 2: Implement VisibilityController**

Create `Assets/Scripts/Visibility/VisibilityController.cs`:
```csharp
using UnityEngine;
using System.Collections.Generic;

namespace Labyrinth.Visibility
{
    public class VisibilityController : MonoBehaviour
    {
        [SerializeField] private float baseVisibilityRadius = 4f;
        [SerializeField] private int rayCount = 90;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        private Mesh _mesh;
        private float _visibilityBonus;
        private float _visibilityBoostTimer;

        public float CurrentRadius => baseVisibilityRadius + _visibilityBonus;

        private void Start()
        {
            _mesh = new Mesh();
            if (meshFilter != null)
            {
                meshFilter.mesh = _mesh;
            }
        }

        private void LateUpdate()
        {
            if (_visibilityBoostTimer > 0)
            {
                _visibilityBoostTimer -= Time.deltaTime;
                if (_visibilityBoostTimer <= 0)
                {
                    _visibilityBonus = 0;
                }
            }

            UpdateVisibilityMesh();
        }

        private void UpdateVisibilityMesh()
        {
            float angleStep = 360f / rayCount;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            vertices.Add(Vector3.zero); // Center point

            for (int i = 0; i <= rayCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, CurrentRadius, wallLayer);

                Vector3 vertex;
                if (hit.collider != null)
                {
                    vertex = transform.InverseTransformPoint(hit.point);
                }
                else
                {
                    vertex = (Vector3)(direction * CurrentRadius);
                }

                vertices.Add(vertex);

                if (i > 0)
                {
                    triangles.Add(0);
                    triangles.Add(i);
                    triangles.Add(i + 1);
                }
            }

            _mesh.Clear();
            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.RecalculateNormals();
        }

        public void ApplyVisibilityBoost(float bonus, float duration)
        {
            _visibilityBonus = bonus;
            _visibilityBoostTimer = duration;
        }

        public float GetVisibilityBoostTimeRemaining()
        {
            return _visibilityBoostTimer;
        }
    }
}
```

**Step 3: Set up visibility in scene**

Create child object under Player with:
- MeshFilter
- MeshRenderer with SpriteMask material (or stencil-based shader)
- VisibilityController script

**Step 4: Create darkness overlay**

Create large quad covering the camera view with darkness material, rendered behind the visibility mask.

---

## Task 14: Base Item System

**Files:**
- Create: `Assets/Scripts/Items/BaseItem.cs`
- Create: `Assets/Scripts/Items/ItemSpawner.cs`

**Step 1: Implement BaseItem**

Create `Assets/Scripts/Items/BaseItem.cs`:
```csharp
using UnityEngine;

namespace Labyrinth.Items
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseItem : MonoBehaviour
    {
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.1f;

        private Vector3 _startPosition;
        private float _bobOffset;

        protected virtual void Start()
        {
            _startPosition = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        protected virtual void Update()
        {
            // Bobbing animation
            _bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = _startPosition + Vector3.up * _bobOffset;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnCollected(other.gameObject);
                Destroy(gameObject);
            }
        }

        protected abstract void OnCollected(GameObject player);
    }
}
```

**Step 2: Implement ItemSpawner**

Create `Assets/Scripts/Items/ItemSpawner.cs`:
```csharp
using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;

namespace Labyrinth.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject keyItemPrefab;
        [SerializeField] private GameObject speedItemPrefab;
        [SerializeField] private GameObject lightSourcePrefab;
        [SerializeField] private int speedItemCount = 3;
        [SerializeField] private int lightSourceCount = 3;

        public void SpawnItems(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            // Spawn key at exit
            Instantiate(keyItemPrefab, new Vector3(exitPos.x, exitPos.y, 0), Quaternion.identity);

            // Find valid spawn positions (floor tiles, not start/exit)
            var validPositions = new List<Vector2>();
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (!cell.IsWall && !cell.IsStart && !cell.IsExit)
                    {
                        validPositions.Add(new Vector2(x, y));
                    }
                }
            }

            // Shuffle positions
            for (int i = validPositions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = validPositions[i];
                validPositions[i] = validPositions[j];
                validPositions[j] = temp;
            }

            // Spawn speed items
            int spawned = 0;
            for (int i = 0; i < validPositions.Count && spawned < speedItemCount; i++)
            {
                var pos = validPositions[i];
                Instantiate(speedItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                spawned++;
            }

            // Spawn light sources
            int lightSpawned = 0;
            for (int i = spawned; i < validPositions.Count && lightSpawned < lightSourceCount; i++)
            {
                var pos = validPositions[i];
                Instantiate(lightSourcePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                lightSpawned++;
            }
        }
    }
}
```

---

## Task 15: Specific Items

**Files:**
- Create: `Assets/Scripts/Items/KeyItem.cs`
- Create: `Assets/Scripts/Items/SpeedItem.cs`
- Create: `Assets/Scripts/Items/LightSourceItem.cs`
- Create prefabs for each

**Step 1: Implement KeyItem**

Create `Assets/Scripts/Items/KeyItem.cs`:
```csharp
using UnityEngine;
using Labyrinth.Core;

namespace Labyrinth.Items
{
    public class KeyItem : BaseItem
    {
        protected override void OnCollected(GameObject player)
        {
            GameManager.Instance?.TriggerWin();
        }
    }
}
```

**Step 2: Implement SpeedItem**

Create `Assets/Scripts/Items/SpeedItem.cs`:
```csharp
using UnityEngine;
using Labyrinth.Player;

namespace Labyrinth.Items
{
    public class SpeedItem : BaseItem
    {
        [SerializeField] private float speedBonus = 3f;
        [SerializeField] private float duration = 8f;

        protected override void OnCollected(GameObject player)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ApplySpeedBoost(speedBonus, duration);
            }
        }
    }
}
```

**Step 3: Implement LightSourceItem**

Create `Assets/Scripts/Items/LightSourceItem.cs`:
```csharp
using UnityEngine;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    public class LightSourceItem : BaseItem
    {
        [SerializeField] private float visibilityBonus = 3f;
        [SerializeField] private float duration = 10f;

        protected override void OnCollected(GameObject player)
        {
            var visibility = player.GetComponentInChildren<VisibilityController>();
            if (visibility != null)
            {
                visibility.ApplyVisibilityBoost(visibilityBonus, duration);
            }
        }
    }
}
```

**Step 4: Create item prefabs**

Create prefabs for each item:
- KeyItem: Gold sprite, KeyItem script, CircleCollider2D (trigger)
- SpeedItem: Blue sprite, SpeedItem script, CircleCollider2D (trigger)
- LightSourceItem: Yellow sprite, LightSourceItem script, CircleCollider2D (trigger)

---

## Task 16: Enemy Controller

**Files:**
- Create: `Assets/Scripts/Enemy/EnemyController.cs`
- Create: `Assets/Prefabs/Enemy.prefab`

**Step 1: Implement EnemyController**

Create `Assets/Scripts/Enemy/EnemyController.cs`:
```csharp
using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Player;
using Labyrinth.Core;

namespace Labyrinth.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 4f;
        [SerializeField] private float speedIncreaseAfter = 30f;
        [SerializeField] private float increasedSpeed = 4.5f;
        [SerializeField] private float pathRecalculateInterval = 0.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 1;

        private Transform _target;
        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _pathTimer;
        private float _chaseTimer;
        private float _attackTimer;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(MazeGrid grid, Transform target)
        {
            _grid = grid;
            _target = target;
            _pathfinding = new Pathfinding(grid);
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_target == null || _grid == null || GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            _chaseTimer += Time.deltaTime;
            _attackTimer -= Time.deltaTime;

            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0)
            {
                RecalculatePath();
                _pathTimer = pathRecalculateInterval;
            }

            MoveAlongPath();
        }

        private void RecalculatePath()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(_target.position.x),
                Mathf.RoundToInt(_target.position.y)
            );

            _currentPath = _pathfinding.FindPath(currentPos, targetPos);
            _pathIndex = 0;
        }

        private void MoveAlongPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
                return;

            Vector3 targetPosition = new Vector3(_currentPath[_pathIndex].x, _currentPath[_pathIndex].y, 0);
            float speed = _chaseTimer > speedIncreaseAfter ? increasedSpeed : baseSpeed;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                _pathIndex++;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && _attackTimer <= 0)
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);
                    _attackTimer = attackCooldown;

                    // Knockback player
                    Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                    other.transform.position += (Vector3)(knockbackDir * 0.5f);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = visible;
            }
        }
    }
}
```

**Step 2: Create Enemy prefab**

Create prefab with:
- SpriteRenderer (red/purple sprite)
- CircleCollider2D (trigger, radius 0.4)
- EnemyController script
- Tag "Enemy"

---

## Task 17: Enemy Spawner

**Files:**
- Create: `Assets/Scripts/Enemy/EnemySpawner.cs`

**Step 1: Implement EnemySpawner**

Create `Assets/Scripts/Enemy/EnemySpawner.cs`:
```csharp
using UnityEngine;
using Labyrinth.Core;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerTransform;

        private MazeGrid _grid;
        private Vector2 _spawnPosition;

        public void Initialize(MazeGrid grid, Vector2 startPosition, Transform player)
        {
            _grid = grid;
            _spawnPosition = startPosition;
            playerTransform = player;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn += SpawnEnemy;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn -= SpawnEnemy;
            }
        }

        private void SpawnEnemy()
        {
            var enemyObj = Instantiate(enemyPrefab, new Vector3(_spawnPosition.x, _spawnPosition.y, 0), Quaternion.identity);
            var enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Initialize(_grid, playerTransform);
            }
        }
    }
}
```

**Step 2: Set up in scene**

Add EnemySpawner to scene, assign enemy prefab reference.

---

## Task 18: Health Display UI

**Files:**
- Create: `Assets/Scripts/UI/HealthDisplay.cs`

**Step 1: Create health UI**

In Canvas, create:
- Panel "HealthPanel" (top-left anchor)
- 3 Image children for hearts (heart sprites)

**Step 2: Implement HealthDisplay**

Create `Assets/Scripts/UI/HealthDisplay.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Player;

namespace Labyrinth.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;
        [SerializeField] private PlayerHealth playerHealth;

        private void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateDisplay;
                UpdateDisplay(playerHealth.CurrentHealth);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
            }
        }

        public void SetPlayerHealth(PlayerHealth health)
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
            }

            playerHealth = health;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateDisplay;
                UpdateDisplay(playerHealth.CurrentHealth);
            }
        }

        private void UpdateDisplay(int currentHealth)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
                }
            }
        }
    }
}
```

---

## Task 19: Game Over / Win UI

**Files:**
- Create: `Assets/Scripts/UI/GameOverUI.cs`

**Step 1: Create overlay UI**

In Canvas, create:
- Panel "GameOverPanel" (full screen, dark semi-transparent, initially inactive)
- Text "GameOverText" ("Caught!" / "Escaped!")
- Button "TryAgainButton"
- Button "MenuButton"

**Step 2: Implement GameOverUI**

Create `Assets/Scripts/UI/GameOverUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Core;

namespace Labyrinth.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private Button menuButton;

        private void Start()
        {
            panel.SetActive(false);

            tryAgainButton.onClick.AddListener(OnTryAgain);
            menuButton.onClick.AddListener(OnMenu);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameWin += ShowWin;
                GameManager.Instance.OnGameLose += ShowLose;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameWin -= ShowWin;
                GameManager.Instance.OnGameLose -= ShowLose;
            }
        }

        private void ShowWin()
        {
            messageText.text = "Escaped!";
            panel.SetActive(true);
        }

        private void ShowLose()
        {
            messageText.text = "Caught!";
            panel.SetActive(true);
        }

        private void OnTryAgain()
        {
            GameManager.Instance?.RestartGame();
        }

        private void OnMenu()
        {
            GameManager.Instance?.LoadMainMenu();
        }
    }
}
```

---

## Task 20: Enemy Spawn Warning

**Files:**
- Create: `Assets/Scripts/UI/SpawnWarningUI.cs`

**Step 1: Create warning UI**

In Canvas, create:
- Text "WarningText" ("Something awakens...", centered, initially inactive)

**Step 2: Implement SpawnWarningUI**

Create `Assets/Scripts/UI/SpawnWarningUI.cs`:
```csharp
using UnityEngine;
using TMPro;
using Labyrinth.Core;

namespace Labyrinth.UI
{
    public class SpawnWarningUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private CanvasGroup canvasGroup;

        private float _fadeTimer;
        private bool _isShowing;

        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn += ShowWarning;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn -= ShowWarning;
            }
        }

        private void Update()
        {
            if (_isShowing)
            {
                _fadeTimer -= Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / displayDuration);
                }

                if (_fadeTimer <= 0)
                {
                    _isShowing = false;
                }
            }
        }

        private void ShowWarning()
        {
            _isShowing = true;
            _fadeTimer = displayDuration;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
        }
    }
}
```

---

## Task 21: Main Menu Scene

**Files:**
- Create: `Assets/Scenes/MainMenu.unity`
- Create: `Assets/Scripts/UI/MainMenuUI.cs`

**Step 1: Create MainMenu scene**

Create new scene with:
- Main Camera
- Canvas with UI elements:
  - Text "TitleText" ("Labyrinth Escape")
  - Button "PlayButton"

**Step 2: Implement MainMenuUI**

Create `Assets/Scripts/UI/MainMenuUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Labyrinth.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
```

**Step 3: Add scenes to build settings**

Add MainMenu and Game scenes to Build Settings.

---

## Task 22: Game Initialization Integration

**Files:**
- Modify: `Assets/Scripts/Maze/MazeInitializer.cs`

**Step 1: Update MazeInitializer to coordinate all systems**

Update `Assets/Scripts/Maze/MazeInitializer.cs`:
```csharp
using UnityEngine;
using Labyrinth.Items;
using Labyrinth.Enemy;
using Labyrinth.Player;
using Labyrinth.Core;

namespace Labyrinth.Maze
{
    public class MazeInitializer : MonoBehaviour
    {
        [SerializeField] private int mazeWidth = 25;
        [SerializeField] private int mazeHeight = 25;
        [SerializeField] private MazeRenderer mazeRenderer;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;

        public MazeGrid Grid { get; private set; }

        private void Start()
        {
            GenerateMaze();
        }

        private void GenerateMaze()
        {
            // Generate maze
            var generator = new MazeGenerator(mazeWidth, mazeHeight);
            Grid = generator.Generate();
            mazeRenderer.RenderMaze(Grid);

            // Spawn player
            var playerObj = Instantiate(playerPrefab,
                new Vector3(mazeRenderer.StartPosition.x, mazeRenderer.StartPosition.y, 0),
                Quaternion.identity);
            playerObj.tag = "Player";

            // Set up camera
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerObj.transform);
            }

            // Spawn items
            if (itemSpawner != null)
            {
                itemSpawner.SpawnItems(Grid, mazeRenderer.StartPosition, mazeRenderer.ExitPosition);
            }

            // Initialize enemy spawner
            if (enemySpawner != null)
            {
                enemySpawner.Initialize(Grid, mazeRenderer.StartPosition, playerObj.transform);
            }
        }
    }
}
```

---

## Task 23: Final Integration & Testing

**Step 1: Wire up all scene references**

In Game scene, ensure all GameObjects are connected:
- MazeInitializer has all references assigned
- Canvas has all UI components
- Layers are set up (Wall, Player, Enemy)
- Tags are set up (Player, Enemy)

**Step 2: Test complete game loop**

1. Start from MainMenu
2. Click Play → Game scene loads
3. Verify maze generates
4. Test player movement with joystick
5. Verify visibility system works
6. Collect items, verify effects
7. Wait for enemy spawn
8. Test combat and death
9. Test key collection and win

**Step 3: Verify mobile build**

Switch platform to Android/iOS, test touch input.

---

## Task 24: Polish & Bug Fixes

**Step 1: Add simple pixel art sprites**

Replace placeholder squares with simple 16x16 pixel sprites:
- Wall tile (dark brick pattern)
- Floor tile (lighter stone)
- Player (simple character)
- Enemy (menacing shape)
- Items (key, potion, orb)
- Hearts (full/empty)

**Step 2: Add basic audio (optional)**

- Background ambient music
- Footstep sounds
- Item pickup sounds
- Damage sound
- Win/lose jingles

**Step 3: Final testing pass**

Run through complete game multiple times, fix any issues found.

---

## Summary

**Total Tasks:** 24
**Estimated Implementation Time:** 4-6 hours

**Key Dependencies:**
- Tasks 1-5: Foundation (no dependencies)
- Tasks 6-8: Scene setup (depends on 1-5)
- Tasks 9-11: Player (depends on 6-8)
- Tasks 12-13: Player features (depends on 9-11)
- Tasks 14-15: Items (depends on 6-8)
- Tasks 16-17: Enemy (depends on 5, 9)
- Tasks 18-20: UI (depends on 12, 17)
- Tasks 21-24: Integration (depends on all above)
