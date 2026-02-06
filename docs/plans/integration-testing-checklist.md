# Level Progression - Integration Testing Checklist

## Pre-Test Setup
1. Open project in Unity Editor
2. Ensure all scripts compile without errors
3. Add LevelSelection scene to Build Settings
4. Configure LevelProgressionManager with level definitions:
   - Assign all 5 level assets to `_levelDefinitions` list
5. Set up LevelSelectionUI references:
   - Wire up buttons, prefabs, and UI elements
6. Delete any existing `progression.json` from persistent data path

## Test Cases

### 1. Level Unlock Flow
- [ ] Start game - only Level 1 should be available
- [ ] Complete Level 1 - Level 2 should unlock
- [ ] Complete Level 2 - Both Level 3A and 3B should unlock
- [ ] Complete Level 3A OR 3B - Level 4 should unlock

### 2. Level Selection UI
- [ ] Back button returns to MainMenu
- [ ] Locked levels show disabled state
- [ ] Available levels show active state
- [ ] Completed levels show completed state
- [ ] Clicking node shows detail panel
- [ ] Start button only enabled for unlocked levels
- [ ] Connection lines draw between parent/child nodes

### 3. Objective Tracking - Persistent
- [ ] CollectItems: Progress saves on escape
- [ ] Progress resets to saved value on new run
- [ ] Progress NOT saved on death

### 4. Objective Tracking - Single Run
- [ ] TimeLimit: Fails if time exceeded
- [ ] SurviveTime: Must survive full duration in one run
- [ ] NoDetection: Fails immediately on detection
- [ ] Single-run objectives reset each attempt

### 5. Escape vs Win
- [ ] Collecting key triggers escape (not immediate win)
- [ ] Escape saves persistent objective progress
- [ ] Win only triggers when ALL objectives complete
- [ ] Death saves nothing

### 6. Fresh Start Each Level
- [ ] Player level resets to 1 on level start
- [ ] Upgrades reset on level start
- [ ] XP resets on level start

### 7. Persistence
- [ ] Progress saves to JSON file
- [ ] Progress loads on game restart
- [ ] Corrupted save file handled gracefully

### 8. MazeInitializer Integration
- [ ] Maze uses config from current level
- [ ] Items spawn from level's item pool
- [ ] Enemies spawn from level's enemy pool

## Expected Test Results
All checkboxes should pass for full integration approval.

## Notes
- Test on both Editor and device build
- Clear persistent data between full test runs
- Check Unity console for errors during all tests
