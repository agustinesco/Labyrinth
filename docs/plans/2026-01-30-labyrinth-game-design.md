# Labyrinth Escape - Game Design Document

## Overview

A mobile 2D top-down procedural labyrinth game where the player navigates a fog-of-war covered maze, collects items, avoids enemies, and finds the key to escape. There is NO combat system - the player cannot attack or defeat enemies, only avoid them.

## Core Specifications

| Feature | Decision |
|---------|----------|
| Maze Size | Large (51x51 grid, corridor width 3) |
| Controls | Virtual joystick (bottom-left) |
| Visibility | Cone-based fog of war with dual textures (visibility + exploration) |
| Enemy AI | Multiple types: A* pathfinding, awareness-based detection |
| Art Style | Pixel art |
| Enemy Spawn | Pre-placed in maze (guards, moles, stalkers) + timed chaser |
| Death System | Health (3 hits, upgradeable) |
| HUD | Hearts, XP bar, inventory, enemy spawn timer |

---

## Architecture

### Scene Structure
- **MainMenu** - Title screen with Play button
- **Game** - Main gameplay scene (MazeInitializer orchestrates startup)
- **MazeTest** - Debug/testing scene

### Core GameObjects
- `GameManager` - Game state (Playing/Won/Lost), enemy spawn timer, events
- `MazeInitializer` - Entry point, orchestrates all startup
- `MazeGenerator` - Procedural labyrinth creation
- `Player` - Movement, health, inventory, item collection
- `EnemySpawnerManager` - Manages all enemy spawning
- `FogOfWarManager` - Dual-texture visibility system

### Game Flow
1. MainMenu → Player taps "Play" → Level Selection
2. Game scene loads → MazeInitializer orchestrates:
   - Maze generates → Maze renders
   - Player spawns at start
   - Camera follows player with bounds
   - Items spawn (key at exit, XP scattered, general items from pool)
   - Traps spawn in hallways
   - Enemies placed (guards, moles, stalkers)
   - Fog of war initialized
3. Enemy spawn timer starts (chaser spawns later)
4. Win: Player collects key → ObjectiveTracker → Victory
5. Lose: Health reaches 0 → Game Over

---

## Maze Generation

### Algorithm: Growing Tree
1. Create solid grid of walls (51x51)
2. Pick starting cell
3. Carve passages using Growing Tree algorithm
4. Branching factor: 0.3 (0=DFS long corridors, 1=Prim's max bifurcation)
5. Corridor width: 3 tiles

### Central Key Room
- 9x9 room carved at the center of the maze
- 2-4 random entrances (N/S/E/W)
- Key spawns at room center
- Connected to maze corridors via entrance tunnels

### Tile Types
- `Wall` - Impassable, blocks line-of-sight
- `Floor` - Walkable space
- `Start` - Player spawn point
- `Exit` / `KeyRoom` - Key spawn location (room center)

### Item Placement
- Key: Always at key room center
- XP Items: 45 scattered on floor tiles
- General Items: 20 randomly picked from item pool
- Traps: 3-5 tripwires in hallways

---

## Visibility System

### Approach: Dual-Texture Fog of War with Custom Shader
- Cone-based raycasting (60 degree cone in facing direction)
- Ambient radius: 3 units (omnidirectional)
- 100 directional rays + 60 ambient rays
- Dual textures: visibility (currently seen) and exploration (previously discovered)

### Parameters
- Base visibility radius: 4 units
- Cone angle: 60 degrees
- Ray march step: 0.12 units
- Resolution multiplier: 10

### Opacity
- Undiscovered: 1.0 (fully black)
- Discovered but not visible: 0.7 (dark fog)
- Currently visible: 0.0 (clear)
- Edge softness: 0.35

### Performance (Mobile Optimized)
- Updates every 2 frames
- Dirty region tracking (only updates changed texture areas)
- Configurable ray counts and resolution

---

## Player

### Components
- `PlayerController` - Movement, speed modifiers, wall detection, sneakiness
- `PlayerHealth` - Hearts, invincibility frames, death animation
- `PlayerInputHandler` - Joystick input + keyboard fallback in editor
- `PlayerInventory` - Storable item management
- `PlayerLevelSystem` - XP and level progression

### Movement
- Base speed: 5 units/second
- Speed modifiers stack: items, upgrades, wall hugger bonus, NoClip multiplier
- Wall detection: 4 cardinal raycasts, 1.2 unit range
- 8-directional analog movement
- Facing direction tracked for cone visibility

### Sneakiness
- Base multiplier: 1.0
- Reduced by permanent sneakiness upgrades
- Shadow Blend: up to 75% awareness reduction (25% per level)
- Minimum: 0.1 (never fully undetectable through awareness)

### Health
- Max health: 3 HP (upgradeable)
- Invincibility: 1.5 seconds after hit (sprite blinks)
- Death animation: 1.5s (spinning, shrinking, fade to red)
- Knockback: 0.5 units on hit

### Virtual Joystick
- Bottom-left position, 150px diameter
- Normalized Vector2 output
- 0.1 dead zone
- 50% opacity when idle, 100% when active

---

## Enemies

There is NO combat system - the player cannot attack or defeat enemies, only avoid them. Enemies deal contact damage but cannot be fought back.

### Chaser (EnemyController)
- **Role**: Relentless pursuer, spawns on timer
- **Movement**: Direct pursuit toward player (no pathfinding)
- **Base speed**: 2.5 units/second (half of player's 5.0)
- **Increased speed**: 3.0 after 30 seconds of chase
- **Damage**: 1 per hit, 2-second cooldown
- **Knockback**: 0.5 units on player hit
- **Bypassed by**: NoClip, Invisibility, Glider

### Patrolling Guard (PatrollingGuardController)
- **Role**: Zone defender, pre-placed in corridors
- **Movement**: Rectangular patrol along walls
- **Patrol speed**: 2.5, Chase speed: 4.0
- **Damage**: 1 per hit, 2-second cooldown
- **States**: Patrolling → Paused → GainingAwareness → Chasing → Investigating → SearchingAround → Returning
- **Detection**: Awareness meter fills when player is visible, decays when hidden
- **Pathfinding**: A* with 0.3s recalculation interval
- **Spawn rules**: Requires 15+ unit corridors, max 3 per maze

### Blind Mole (BlindMoleController)
- **Role**: Ambush predator at intersections
- **Spawn location**: 4-way intersections only
- **Cycle**: Inactive (3s) → Emerging (1s warning) → Active (4s detecting) → Attacking → Hiding (1s)
- **Detection**: Movement-based (detects when player moves >0.1 units in range)
- **Detection range**: 4 units
- **Attack**: Projectile, 1 damage, 8 units/second
- **Special**: Underground while inactive (collider disabled), instant detection (no awareness meter)
- **Spawn rules**: Max 5 per maze, 50% chance per valid intersection

### Shadow Stalker (ShadowStalkerController)
- **Role**: Psychological horror enemy
- **Speed**: 3.5 units/second
- **Damage**: 2 per hit, 2-second cooldown
- **Core mechanic**: Freezes completely when player looks at it
  - Vision check: Player must face within 75 degrees
  - Respects fog of war (walls block detection)
  - 0.1s grace period before freeze
- **Detection**: 8-unit radius, 20-unit leash (loses track beyond)
- **Pathfinding**: A* with 0.5s recalculation
- **Audio**: Creeping sound at 2s intervals within 8 units
- **Spawn rules**: Max 2 per maze, 40% chance

### Enemy Awareness System (EnemyAwarenessController)
- Awareness meter: 0 to threshold (default 100)
- Gain rate: 25/second (when player visible)
- Decay rate: 15/second (when player hidden)
- Distance scaling: Optional closer = faster gain
- Player sneakiness multiplier applied
- Vision cone: Configurable angle per enemy
- Line of sight: Raycasting against walls
- Per-enemy configs as ScriptableObjects

### Enemy Spawn Config
- Exclusion zones: 8 units from start, 5 units from exit
- Each type has max count and spawn chance
- Configured via `EnemySpawnConfig` ScriptableObject

---

## Items

### Base Behavior
- Trigger colliders for pickup
- Bobbing animation (speed 2, amplitude 0.1)
- Storable items go to inventory, non-storable activate immediately

### Item Types

| Item | Storable | Effect |
|------|----------|--------|
| Key | No | Triggers win condition |
| XP | No | Grants 1 XP immediately |
| Speed | Yes | +3 speed for 8 seconds |
| Heal | Yes | Restore 1 HP |
| Explosive | Yes | 2 damage in 2-tile radius |
| Light | Yes | Place light source (3 charges, 4-unit radius) |
| Pebbles | Yes | Drop decoy pebbles (3 uses) |
| Invisibility | Yes | Undetectable for 5 seconds |
| EchoStone | Yes | Sonar pulse reveals enemies through walls (12-unit radius, 3s reveal, 2 uses) |
| Glider | Yes | Pass through walls for 4 seconds |
| Caltrops | Yes | Scatter traps that slow enemies (0.4x speed, 4s, 3 uses) |
| EagleEye | Yes | +4 vision range for 10 seconds |
| Wisp | No | Spawns orb that pathfinds to key, leaves fading trail |
| Tunnel | No | Creates tunnel through 1-tile thick wall |
| SilkWorm | No | Creates silk trap between walls, snares enemies for 2s |

### Item Spawn Config
- Key: 1, always at key room center
- XP Items: 45 scattered
- General items: 20 randomly from pool
- Configured via `ItemSpawnConfig` ScriptableObject

---

## Traps

### Tripwire Trap
- Thin visible line across hallway (subtle, easy to miss)
- One-time use: wire snaps on contact
- Spawns arrow perpendicular to corridor
- 3-5 per maze, in hallways only
- Respects NoClip mode

### Arrow
- Projectile: 8-10 units/second
- Damage: 1 on player contact
- Destroyed on wall or player hit
- Respects player invincibility frames

---

## Leveling & Upgrades

### XP System
- XP items scattered through maze (45 per level)
- Formula: BaseXP(5) + (level-1) x ScalingFactor(5) per level
- No level cap (configurable)

### Upgrade Types (offered on level up, pick 1 of 3)
| Upgrade | Effect |
|---------|--------|
| Swift Feet | +X permanent speed bonus |
| Eagle Eye | +X permanent vision radius |
| Restoration | Restore X HP immediately |
| Wall Hugger | +X% speed when near walls |
| Shadow Blend | Stackable, each level = 25% awareness reduction (max 75%) |
| Deep Pockets | +1 inventory slot (stackable) |

---

## UI

### Main Menu
- Game title (centered top)
- "Play" button → Level Selection
- Dark background with maze pattern

### Level Selection
- Level picker with progression
- Level definitions: Level 01 through Level 05

### In-Game HUD
- Health hearts: top-left, 3 icons
- XP bar: shows progress to next level
- Inventory: grid-based item slots
- Enemy spawn timer countdown

### Level Up Overlay
- 3 random upgrade cards offered
- Player picks 1

### Pause Menu
- Pause/resume controls

### Game Over Overlay
- "Caught!" text (lose) / "Escaped!" text (win)
- "Try Again" / "Menu" buttons

### Enemy Warning
- "Something awakens..." on enemy spawn
- Fades after 2 seconds

### Bestiary
- Enemy discovery tracker
- Grid UI showing discovered enemy types

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/          (GameManager, CameraFollow)
│   ├── Maze/          (MazeGenerator, MazeGrid, MazeCell, MazeRenderer, MazeInitializer, Pathfinding)
│   ├── Player/        (PlayerController, PlayerHealth, PlayerInventory, PlayerInputHandler, NoClipManager, InvisibilityManager)
│   ├── Enemy/         (EnemyController, PatrollingGuardController, BlindMoleController, ShadowStalkerController, EnemyAwarenessController, EnemySpawnerManager)
│   ├── Items/         (BaseItem + 15 item types, ItemSpawner, ItemSpawnConfig)
│   ├── Visibility/    (FogOfWarManager, LightSource, VisibilityAwareEntity)
│   ├── Leveling/      (PlayerLevelSystem, UpgradeManager, LevelingConfig, UpgradeDefinition)
│   ├── Traps/         (TripwireTrap, Arrow, TrapSpawner)
│   ├── Progression/   (ObjectiveTracker, LevelDefinition)
│   └── UI/            (VirtualJoystick, HealthDisplay, XPDisplayUI, InventoryUI, GameOverUI, PauseMenuUI, LevelUpUI, LevelSelectionUI, BestiaryGridUI)
├── Config/            (ScriptableObject assets: maze, enemy, item, leveling configs)
├── Prefabs/
├── Sprites/
├── Scenes/            (MainMenu, Game, MazeTest)
└── Materials/
```

---

## Technical Notes

- Unity 2022.3 (2D with Sprite Renderer)
- Physics2D for collisions and raycasting
- Custom FogOfWar shader (dual-texture approach)
- ScriptableObject-based configuration for all major systems
- A* pathfinding with caching and reusable data structures
- Event-driven communication (GameManager, PlayerHealth, PlayerLevelSystem events)
- Target: 60 FPS on mid-range mobile
- Mobile-optimized fog of war (dirty region tracking, configurable update frequency)

---

## Changelog

| Date | Change |
|------|--------|
| 2026-01-30 | Initial design: 25x25 maze, 1 enemy type, 3 items |
| 2026-02-02 | Added central 9x9 key room design |
| 2026-02-02 | Added tripwire trap system |
| 2026-02-05 | Added level progression system (5 levels) |
| 2026-02-09 | Chaser enemy speed reduced to 2.5 (half player speed of 5.0), increased speed to 3.0 |
| 2026-02-09 | Document updated to reflect full current state of implementation |
