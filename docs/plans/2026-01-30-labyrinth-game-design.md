# Labyrinth Escape - Game Design Document

## Overview

A mobile 2D top-down procedural labyrinth game where the player must find a key to escape while avoiding an enemy that spawns after a timer.

## Core Specifications

| Feature | Decision |
|---------|----------|
| Maze Size | Medium (25x25 grid, ~20-30 navigable cells) |
| Controls | Virtual joystick (bottom-left) |
| Visibility | Line-of-sight (raycasting, blocked by walls) |
| Enemy AI | A* pathfinding, always finds player |
| Art Style | Pixel art |
| Enemy Spawn | Fixed timer (45 seconds) |
| Death System | Health (3 hits) |
| HUD | Minimal (hearts only) |

---

## Architecture

### Scene Structure
- **MainMenu** - Title screen with Play button
- **Game** - Labyrinth gameplay scene

### Core GameObjects
- `GameManager` - Game state, enemy spawn timer
- `MazeGenerator` - Procedural labyrinth creation
- `Player` - Movement, health, item collection
- `Enemy` - A* pathfinding, chases player
- `InputManager` - Virtual joystick handling
- `VisibilitySystem` - Line-of-sight rendering

### Game Flow
1. MainMenu → Player taps "Play"
2. Game scene loads → Maze generates → Player spawns at start → Key spawns at end → Items scattered
3. 45-second timer starts
4. Timer hits zero → Enemy spawns at maze entrance
5. Win: Player touches key → Return to MainMenu
6. Lose: Health reaches 0 → Return to MainMenu

---

## Maze Generation

### Algorithm: Recursive Backtracking
1. Create solid grid of walls (25x25)
2. Pick starting cell (bottom-left corner)
3. Carve passages using recursive backtracking
4. Mark furthest point from start as key location
5. Place items along dead-ends and corridors

### Tile Types
- `Wall` - Impassable, blocks line-of-sight
- `Floor` - Walkable space
- `Start` - Player spawn point
- `Exit` - Key spawn location

### Item Placement
- Key: Always at furthest cell from start
- SpeedItem: 2-3 in dead-ends
- LightSource: 2-3 along corridors

---

## Visibility System

### Approach: Raycasting with Shadow Mesh
- Cast 90-120 rays from player in 360 degrees
- Rays stop when hitting walls
- Create mesh from ray endpoints for visible area

### Parameters
- Base visibility radius: 4 units
- LightSource boost: +3 units for 10 seconds
- Rays cast only when player moves

### Rendering
- Darkness layer (black) covering entire maze
- SpriteMask cuts hole based on visibility mesh
- Soft edge on visibility boundary

---

## Player

### Components
- `PlayerController` - Movement from joystick
- `PlayerHealth` - 3 hearts, 1.5s invincibility after hit
- `ItemCollector` - Trigger-based pickup

### Movement
- Base speed: 5 units/second
- SpeedItem boost: +3 units/second for 8 seconds
- 8-directional analog movement
- CircleCollider2D (radius 0.4)

### Virtual Joystick
- Bottom-left position, 150px diameter
- Normalized Vector2 output
- 0.1 dead zone
- 50% opacity when idle

### Feedback
- Sprite flashes red on damage
- Brief screen shake on hit
- Sprite blinks during invincibility

---

## Enemy

### Behavior
- Spawns at maze entrance after 45 seconds
- A* pathfinding to player position
- Recalculates path every 0.5 seconds

### Movement
- Base speed: 4 units/second
- Increases to 4.5 after 30 seconds of chase

### Contact Damage
- 1 damage on contact
- Knockback player slightly
- 2-second cooldown between hits
- There is NO combat system - the player cannot attack or defeat enemies, only avoid them

### Visibility
- Only visible in player's line-of-sight
- Audio cue (heartbeat) when nearby

---

## Items

### Base Behavior
- Trigger colliders for pickup
- Bobbing animation
- Glow/sparkle particles

### Key Item
- Golden key sprite
- Triggers win on pickup
- 1 per maze, at furthest point

### SpeedItem
- Blue potion sprite
- +3 speed for 8 seconds
- Duration stacks
- 2-3 per maze in dead-ends

### LightSource
- Yellow orb sprite
- +3 visibility for 10 seconds
- Duration stacks
- 2-3 per maze in corridors

---

## UI

### Main Menu
- Game title (centered top)
- "Play" button (centered, large tap target)
- Dark background with maze pattern

### In-Game HUD
- Health hearts: top-left, 3 icons
- Active effect icons below hearts

### Game Over Overlay
- "Caught!" text
- "Try Again" / "Menu" buttons

### Win Overlay
- "Escaped!" text
- "Play Again" / "Menu" buttons

### Enemy Warning
- "Something awakens..." at 45 seconds
- Fades after 2 seconds

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/          (GameManager, SceneLoader)
│   ├── Maze/          (MazeGenerator, MazeCell, Pathfinding)
│   ├── Player/        (PlayerController, PlayerHealth, ItemCollector)
│   ├── Enemy/         (EnemyController, EnemyPathfinding)
│   ├── Items/         (BaseItem, KeyItem, SpeedItem, LightSourceItem)
│   ├── Visibility/    (VisibilityController, ShadowCaster)
│   └── UI/            (UIManager, VirtualJoystick, HealthDisplay)
├── Prefabs/
├── Sprites/
├── Scenes/            (MainMenu, Game)
└── Materials/
```

---

## Technical Notes

- Unity 2D with Sprite Renderer
- Physics2D for collisions and raycasting
- No external packages required
- SpriteMask for visibility system
- Target: 60 FPS on mid-range mobile
- Maze generation: < 0.5 seconds
