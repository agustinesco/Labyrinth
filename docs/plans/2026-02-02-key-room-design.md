# Key Room Design

## Overview

A special 9x9 squared room in the center of the maze that contains the key item. The room has 2-4 entrances randomly selected from the four cardinal directions, each connected to the maze.

## Specifications

- **Room size:** 9x9 tiles
- **Location:** Center of the maze
- **Entrances:** 2-4 randomly selected, centered on each wall (N, S, E, W)
- **Key placement:** Center of the room

## Implementation

### MazeGenerator.cs Changes

1. Add room properties (`_roomSize`, `_roomCenterX`, `_roomCenterY`, `_roomEntrances`)
2. Add `CarveKeyRoom()` method - carves room and selects entrances
3. Modify `IsValidCarveTarget()` - skip cells overlapping room area
4. Add `ConnectRoomToMaze()` - connects entrances to maze corridors
5. Use room center as exit position instead of furthest cell

### MazeCell.cs Changes

- Add `IsKeyRoom` property to mark room cells

### Generation Flow

1. Calculate room center position
2. Carve the 9x9 room area
3. Randomly select 2-4 entrances
4. Generate maze (skipping room area)
5. Connect each entrance to nearest maze corridor
6. Mark room center as exit (key position)

### Files Unchanged

- ItemSpawner.cs - Already uses exit position for key
- MazeRenderer.cs - Renders room as floor tiles
- TrapSpawner.cs - Works normally
