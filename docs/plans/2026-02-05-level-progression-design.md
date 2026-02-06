# Level Progression System Design

## Overview

Transform Labyrinth from a single randomized experience into a structured progression with distinct levels. Each level is a self-contained challenge with its own maze configuration, item pool, enemy pool, and objectives.

## Key Decisions

- **Fresh start each level** - Player starts at level 1 with no upgrades each attempt
- **Composite objectives** - Levels can require multiple conditions
- **Branching paths** - Completing a level unlocks child levels, player chooses path
- **Horizontal tree UI** - Left-to-right progression, mobile-friendly scrolling
- **ScriptableObject per level** - Self-contained asset with all settings
- **JSON file persistence** - Flexible save format for completion and progress
- **Minimal initial content** - 5 levels to prove the architecture

## Core Data Structures

### LevelDefinition (ScriptableObject)

```
LevelDefinition
├── Basic Info
│   ├── levelId (string, unique identifier)
│   ├── displayName (string)
│   ├── description (string)
│   └── icon (Sprite, optional)
│
├── Maze Configuration (embedded)
│   ├── mazeWidth, mazeHeight
│   ├── corridorWidth
│   └── branchingFactor
│
├── Spawn Pools
│   ├── itemPool (List<ItemSpawnEntry>)
│   │   └── Each: prefab, weight, maxCount
│   ├── enemyPool (List<EnemySpawnEntry>)
│   │   └── Each: prefab, count, spawnDelay
│   └── xpItemCount
│
├── Objectives (List<LevelObjective>)
│
└── Progression
    └── unlockedByLevels (List<LevelDefinition>)
```

### LevelObjective (Serializable class)

```
LevelObjective
├── type (enum: CollectItems, SurviveTime, TimeLimit, NoDetection)
├── parameters (int targetCount, float timeSeconds, string itemType, etc.)
├── persistProgress (bool) - if true, progress saves on escape
└── description (string) - displayed to player
```

Objectives use a type enum with parameters:
- `ReachExit` - no params
- `CollectKey` - no params
- `CollectItems` - itemType, count
- `SurviveTime` - seconds
- `TimeLimit` - seconds (fail if exceeded)
- `NoDetection` - fail if spotted

Multiple objectives combine with AND logic.

## Progression Tracking & Persistence

### LevelProgressionManager (Singleton)

```
LevelProgressionManager
├── allLevels (List<LevelDefinition>) - loaded from Resources
├── completedLevelIds (HashSet<string>) - from save file
├── currentLevel (LevelDefinition) - active level being played
│
├── Methods
│   ├── IsLevelUnlocked(levelId) → bool
│   ├── IsLevelCompleted(levelId) → bool
│   ├── GetAvailableLevels() → List<LevelDefinition>
│   ├── StartLevel(levelDef) - sets currentLevel, loads Game scene
│   ├── CompleteCurrentLevel() - marks complete, saves
│   └── LoadProgress() / SaveProgress()
│
└── Events
    ├── OnLevelCompleted(levelId)
    └── OnProgressLoaded()
```

### Save File Format (JSON)

Stored at `Application.persistentDataPath/progression.json`:

```json
{
  "completedLevels": ["level_01", "level_02a"],
  "levelProgress": {
    "level_02a": {
      "objectives": [
        { "index": 0, "progress": 3 },
        { "index": 1, "progress": 0 }
      ]
    }
  },
  "levelStats": {
    "level_01": {
      "completedAt": "2024-01-15T10:30:00",
      "attempts": 3
    }
  }
}
```

### Unlock Logic

A level is unlocked when ALL levels in its `unlockedByLevels` list are completed. If the list is empty, the level is available from the start (root level).

## Objective System

### Key Item = Escape

The key item is the exit mechanism, not an objective. Reaching the key item ends the run successfully. Objectives are separate goals that can span multiple runs.

- **Escape** - Player reached key, run ends successfully
- **Progress saves on escape** - For objectives with `persistProgress: true`
- **Death = progress lost** - That run's progress doesn't save
- **Single-run objectives** - `persistProgress: false` must complete in one attempt

### ObjectiveTracker (MonoBehaviour)

```
ObjectiveTracker
├── Initialize(LevelDefinition)
│   └── Loads saved progress for persistProgress objectives
│
├── OnKeyItemCollected() - called when player escapes
│   ├── Saves progress for persistProgress objectives
│   ├── If allCompleted → mark level complete
│   └── Return to level selection
│
├── OnPlayerDeath()
│   └── No progress saved, return to menu
│
└── ResetNonPersistentObjectives()
    └── Called at run start for objectives with persistProgress=false
```

### Example Objectives

- "Collect 10 gems" (`persistProgress: true`) - Can collect 4 this run, escape, next run starts at 4
- "Escape under 60 seconds" (`persistProgress: false`) - Must do in single run
- "Escape without detection" (`persistProgress: false`) - Single run, resets if spotted

## UI - Level Selection Tree

### Main Menu Changes

- Replace "Start Expedition" button with "Look for Expedition"
- Clicking opens the Level Selection screen

### LevelSelectionUI (New Screen)

```
LevelSelectionUI
├── Header Bar
│   ├── Back button → MainMenu
│   └── Title: "Expeditions"
│
├── ScrollRect (horizontal scrolling)
│   └── Content container
│       └── LevelNode instances positioned by tree layout
│
├── Connection lines (UI LineRenderer or Image-based)
│   └── Drawn between parent → child nodes
│
├── LevelNode (Prefab)
│   ├── Icon/thumbnail
│   ├── Level name
│   ├── Status indicator (locked/available/completed)
│   ├── Objective progress preview (e.g., "2/3 objectives")
│   └── Button → Opens level detail panel
│
└── LevelDetailPanel (Modal)
    ├── Level name & description
    ├── Objectives list with progress
    ├── Maze preview (optional)
    └── "Start Expedition" button
```

### Tree Layout Logic

Levels are positioned automatically based on depth:
- X position = depth in tree (how many levels from root)
- Y position = distributed evenly among siblings
- Lines connect each level to its prerequisites

### Visual States

| State | Appearance |
|-------|------------|
| Locked | Greyed out, lock icon, prerequisites shown |
| Available | Full color, glowing border |
| In Progress | Full color, partial objective indicators |
| Completed | Checkmark overlay, slightly dimmed |

### Navigation Flow

| From | To | Action |
|------|----|--------|
| MainMenu | LevelSelection | Click "Look for Expedition" |
| LevelSelection | MainMenu | Click "Back" |
| LevelSelection | Game | Select level → "Start" |
| Game | LevelSelection | Escape (key), Win, or Lose |

## Integration with Existing Systems

### MazeInitializer Changes

Pull config from current level instead of hardcoded references:

```csharp
void Start() {
    var levelDef = LevelProgressionManager.Instance.currentLevel;
    var mazeConfig = levelDef.GetMazeConfig();
    // ... rest of initialization uses levelDef for spawns
}
```

### GameManager Changes

- Remove auto-return to MainMenu on win
- Add `OnLevelEscape` event (distinct from win)
- Win = all objectives complete + escaped
- Escape = reached key without completing all objectives

```
GameManager
├── TriggerEscape() - player reached key
│   └── Fires OnLevelEscape, ObjectiveTracker saves progress
├── TriggerWin() - called by ObjectiveTracker when all complete
│   └── Fires OnGameWin, marks level complete
└── TriggerLose() - unchanged, no progress saved
```

### PlayerLevelSystem Reset

On each level start:
```csharp
PlayerLevelSystem.Instance.ResetLevel();
UpgradeManager.Instance.ResetUpgrades();
```

## Initial Level Content

### Level Tree Structure (5 levels)

```
[Level 1: The First Steps]
         │
         ▼
[Level 2: Crossroads]
         │
    ┌────┴────┐
    ▼         ▼
[Level 3A:  [Level 3B:
 The Hunt]   Shadow Path]
    │         │
    └────┬────┘
         ▼
[Level 4: The Deep]
```

### Level Definitions

| Level | Maze | Enemies | Objectives |
|-------|------|---------|------------|
| **1: The First Steps** | Small (21x21), high branching | 1 Patrolling Guard (60s delay) | Reach the key (tutorial) |
| **2: Crossroads** | Medium (31x31) | 2 Guards, 1 Mole | Collect 5 gems (persist: true) |
| **3A: The Hunt** | Medium (31x31), long corridors | 4 Guards | No detection (persist: false), Escape under 90s (persist: false) |
| **3B: Shadow Path** | Medium (35x25), wide | 3 Moles | No detection (persist: false), Collect 3 light sources (persist: true) |
| **4: The Deep** | Large (41x41) | 3 Guards, 2 Moles | Collect 8 gems (persist: true), Survive 120s (persist: false) |

### Unlock Requirements

- Level 1: None (starting level)
- Level 2: Level 1
- Level 3A: Level 2
- Level 3B: Level 2
- Level 4: Level 3A OR Level 3B

## File Structure

### New Files

```
Assets/Scripts/
├── Progression/
│   ├── LevelDefinition.cs (ScriptableObject)
│   ├── LevelObjective.cs (Serializable class)
│   ├── LevelProgressionManager.cs (Singleton)
│   ├── ObjectiveTracker.cs (MonoBehaviour)
│   └── ProgressionSaveData.cs (JSON serialization)
│
├── UI/
│   ├── LevelSelectionUI.cs
│   ├── LevelNodeUI.cs
│   ├── LevelDetailPanelUI.cs
│   └── TreeLayoutManager.cs

Assets/Config/
└── Levels/
    ├── Level_01_FirstSteps.asset
    ├── Level_02_Crossroads.asset
    ├── Level_03A_TheHunt.asset
    ├── Level_03B_ShadowPath.asset
    └── Level_04_TheDeep.asset

Assets/Scenes/
└── LevelSelection.unity (new scene)

Assets/Prefabs/UI/
├── LevelNode.prefab
└── LevelDetailPanel.prefab
```

### Modified Files

- `MainMenuUI.cs` - Replace button, add navigation
- `GameManager.cs` - Add escape vs win distinction, remove auto-return
- `MazeInitializer.cs` - Pull config from current level
- `KeyItem.cs` - Notify ObjectiveTracker instead of direct win
- `PlayerLevelSystem.cs` - Ensure ResetLevel works properly
