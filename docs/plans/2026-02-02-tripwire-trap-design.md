# Tripwire Trap Design

## Overview

A tripwire trap that spawns randomly in hallways. When the player steps on the wire, it snaps and an arrow shoots perpendicular across the corridor, dealing damage if it hits.

## Design Decisions

- **Arrow direction:** Fixed perpendicular to corridor (classic dungeon trap style)
- **Visibility:** Subtle/faint - visible but easy to miss when rushing
- **Damage:** 1 heart (same as enemy hit)
- **Reusability:** One-time use - wire snaps, trap destroyed
- **Spawn count:** 3-5 traps per maze

## Components

### TripwireTrap (GameObject + Script)
- Thin visible line rendered across the hallway
- Trigger collider to detect player contact
- Stores the arrow firing direction (perpendicular to hallway)
- Spawns arrow and destroys itself when triggered

### Arrow (GameObject + Script)
- Projectile that moves in a straight line at 8-10 units/sec
- Deals 1 damage on player contact
- Destroys itself when hitting a wall or player
- Respects player invincibility frames

### TrapSpawner (Script)
- Finds valid hallway positions (floor cells with walls on opposite sides)
- Determines hallway orientation to set arrow direction
- Spawns 3-5 traps randomly distributed through maze
- Excludes start/exit positions with buffer zone

## Spawning Logic

1. Identify hallway cells - floor cells with walls on two opposite sides
2. Determine orientation:
   - Horizontal hallway (walls above/below) → arrow shoots UP or DOWN
   - Vertical hallway (walls left/right) → arrow shoots LEFT or RIGHT
3. Filter out start position, exit position, and nearby cells
4. Randomly select 3-5 positions from valid candidates

## Trigger Sequence

1. Player touches tripwire (OnTriggerEnter2D)
2. Spawn arrow at wall edge, facing perpendicular direction
3. Destroy tripwire GameObject
4. Arrow travels until hitting player or wall
5. On player hit: deal 1 damage, apply knockback, destroy arrow
6. On wall hit: destroy arrow

## File Structure

```
Assets/Scripts/Traps/
├── TripwireTrap.cs
├── Arrow.cs
└── TrapSpawner.cs

Assets/Prefabs/Traps/
├── TripwireTrap.prefab
└── Arrow.prefab
```

## Integration

- TrapSpawner called from MazeInitializer after maze generation
- Uses existing PlayerHealth.TakeDamage() for damage
- Uses existing collision layers and "Player" tag
