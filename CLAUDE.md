# Claude Code Project Instructions

## ⚠️ Unity Rules

- **NEVER edit Unity prefab (.prefab), scene (.unity), or asset (.asset) YAML files directly.** These are serialized binary-like files managed by Unity's editor. Always use MCP tools (manage_scene, manage_components, find_gameobjects) to modify Unity objects, or generate C# editor scripts that make changes at runtime/edit-time.

### MCP Instance Verification

- Before performing any Unity MCP operation, verify the active Unity instance by calling `mcp__unityMCP__read_console` or checking the project name.
- If multiple Unity instances are running, confirm with the user which instance/project to target before proceeding.

### Error Recovery

- When a Unity MCP tool call fails or returns unexpected results, do NOT retry the same approach more than twice.
- Instead, stop and suggest: (1) restarting the MCP connection, (2) checking which Unity instance is active, or (3) an alternative approach like generating a C# editor script.

## Git Rules

- **NEVER push directly to main branch** - Always create feature branches and use pull requests
- Always create a new branch for changes
- Use descriptive branch names

## Project Overview

Labyrinth2 is a mobile 2D top-down procedural maze game built with Unity 2022.3. The player navigates a fog-of-war covered labyrinth, collects items, avoids enemies with A* pathfinding, and finds the key to escape. There is NO combat system - the player cannot attack or defeat enemies, only avoid them. Enemies deal contact damage but cannot be fought back.

## Build and Test Commands

**Run Tests (via Unity MCP):**
```
mcp__unityMCP__run_tests with mode="EditMode"
```
Then poll results with `mcp__unityMCP__get_test_job`.

**Check Compilation:**
```
mcp__unityMCP__read_console to check for errors
mcp__unityMCP__refresh_unity to trigger recompilation
```

**Play Mode:**
```
mcp__unityMCP__manage_editor with action="play"
```

## Architecture

### Namespace Structure
All code lives under the `Labyrinth` namespace:
- `Labyrinth.Core` - GameManager, CameraFollow
- `Labyrinth.Maze` - MazeGenerator, MazeGrid, MazeCell, Pathfinding, MazeRenderer, MazeInitializer
- `Labyrinth.Player` - PlayerController, PlayerHealth, PlayerInputHandler, PlayerLevelSystem
- `Labyrinth.Enemy` - Enemy types, spawners, awareness system
- `Labyrinth.Items` - BaseItem and 19+ item implementations
- `Labyrinth.Visibility` - FogOfWarManager, LightSource
- `Labyrinth.Leveling` - XP and upgrade systems
- `Labyrinth.Traps` - TripwireTrap, Arrow, TrapSpawner
- `Labyrinth.UI` - All UI components

### Core Patterns

**ScriptableObject Configuration:** Major systems are configured via ScriptableObjects in `Assets/Config/`:
- `MazeGeneratorConfig` - Maze dimensions, corridor width, branching factor
- `EnemySpawnConfig` - Enemy types and counts
- `ItemSpawnConfig` - Item distribution
- `LevelingConfig` - XP scaling
- `EnemyAwarenessConfig` - Per-enemy detection settings

**Game Initialization Flow:**
```
MazeInitializer.Start()
  → MazeGenerator.Generate()
  → MazeRenderer.RenderMaze()
  → Instantiate player at start position
  → CameraFollow.SetTarget() + SetBounds()
  → ItemSpawner.SpawnItems()
  → TrapSpawner.SpawnTraps()
  → EnemySpawnerManager setup
  → FogOfWarManager.SetMazeDimensions()
```

**Event-Driven Communication:**
- `GameManager`: OnEnemySpawn, OnGameWin, OnGameLose
- `PlayerHealth`: OnHealthChanged, OnDeath
- `PlayerLevelSystem`: OnLevelUp, OnXPChanged
- `EnemyAwarenessController`: OnPlayerDetected, OnAwarenessChanged

### Maze Generation
Uses Growing Tree algorithm with configurable branching factor (0=DFS, 1=Prim's). Features a 9x9 key room at center with 2-4 random entrances. Default grid is 51x51 with corridor width 3.

### Enemy Awareness System
Enemies use gradual detection via awareness meters. When player is visible, awareness fills; when hidden, it decays. Detection respects player's sneakiness multiplier and can scale with distance. Configured per-enemy via `EnemyAwarenessConfig`.

### Visibility System
Dual-texture fog of war with cone-based raycasting. Visibility texture shows currently visible areas; exploration texture shows previously visited. Performance optimized for mobile with configurable ray counts, update frequency, and resolution multiplier.

### Pathfinding
Optimized A* with reusable data structures, path caching, and cardinal + diagonal movement. Enemy recalculation interval is 0.5s; guards use 0.3s.

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Maze/MazeInitializer.cs` | Game scene entry point, orchestrates all startup |
| `Assets/Scripts/Core/GameManager.cs` | Game state, enemy spawn timer, win/lose logic |
| `Assets/Scripts/Maze/MazeGenerator.cs` | Procedural maze generation |
| `Assets/Scripts/Maze/Pathfinding.cs` | A* pathfinding for enemies |
| `Assets/Scripts/Enemy/EnemyAwarenessController.cs` | Gradual detection system |
| `Assets/Scripts/Visibility/FogOfWarManager.cs` | Fog of war rendering |
| `Assets/Scripts/Player/PlayerController.cs` | Player movement and speed modifiers |

## Scenes

- **MainMenu** - Title screen with Play button
- **Game** - Main gameplay scene (MazeInitializer orchestrates startup)
- **MazeTest** - Debug/testing scene

## Testing

Tests are in `Assets/Tests/EditMode/` using NUnit. Current test coverage:
- MazeGeneratorTests - Grid generation, start/exit marking
- MazeGridTests - Grid boundaries and cell access
- MazeCellTests - Cell state management
- PathfindingTests - A* pathfinding correctness
- CaltropsTests - Caltrop damage mechanics
- PlayerHealthTests - Health tracking, damage, healing
- EchoStoneTests - Echo sonar mechanics

## Design Documents

Detailed design specs are in `/docs/plans/`:
- `2026-01-30-labyrinth-game-design.md` - Core design document
- `2026-01-30-labyrinth-game-implementation.md` - Task-by-task implementation plan
- `2026-02-02-key-room-design.md` - Center key room specification
- `2026-02-02-tripwire-trap-design.md` - Trap system specification
