# Level Progression System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement a level-based progression system where each labyrinth is a distinct level with unique configuration, objectives, and unlock requirements displayed in a horizontal tree UI.

**Architecture:** Levels are defined as ScriptableObjects containing maze config, spawn pools, and composite objectives. A LevelProgressionManager singleton tracks completion/progress persisted to JSON. ObjectiveTracker monitors objectives during gameplay, saving progress on escape (key collection). UI shows a horizontal tree of levels with branching paths.

**Tech Stack:** Unity 2D, C#, ScriptableObjects, JSON persistence, Unity UI (Canvas/ScrollRect)

**Working Directory:** `/Users/agustinesco/mobileProtos/Labyritnh/.worktrees/level-progression`

---

## Task 1: Create LevelObjective Data Structure

**Files:**
- Create: `Assets/Scripts/Progression/LevelObjective.cs`

**Step 1: Create the Progression directory**

```bash
mkdir -p Assets/Scripts/Progression
```

**Step 2: Create LevelObjective.cs**

```csharp
using System;
using UnityEngine;

namespace Labyrinth.Progression
{
    public enum ObjectiveType
    {
        ReachExit,
        CollectItems,
        SurviveTime,
        TimeLimit,
        NoDetection,
        DefeatEnemies
    }

    [Serializable]
    public class LevelObjective
    {
        [SerializeField] private ObjectiveType _type;
        [SerializeField] private string _description;
        [SerializeField] private int _targetCount;
        [SerializeField] private float _targetTime;
        [SerializeField] private string _itemType;
        [SerializeField] private bool _persistProgress;

        public ObjectiveType Type => _type;
        public string Description => _description;
        public int TargetCount => _targetCount;
        public float TargetTime => _targetTime;
        public string ItemType => _itemType;
        public bool PersistProgress => _persistProgress;

        public bool IsSingleRun => !_persistProgress;
    }
}
```

**Step 3: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 4: Commit**

```bash
git add Assets/Scripts/Progression/
git commit -m "Add LevelObjective data structure"
```

---

## Task 2: Create LevelDefinition ScriptableObject

**Files:**
- Create: `Assets/Scripts/Progression/LevelDefinition.cs`

**Step 1: Create LevelDefinition.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Labyrinth.Maze;

namespace Labyrinth.Progression
{
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Labyrinth/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string _levelId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Maze Configuration")]
        [SerializeField, Min(11)] private int _mazeWidth = 31;
        [SerializeField, Min(11)] private int _mazeHeight = 31;
        [SerializeField, Min(1)] private int _corridorWidth = 3;
        [SerializeField, Range(0f, 1f)] private float _branchingFactor = 0.3f;

        [Header("Item Spawns")]
        [SerializeField] private GameObject _keyItemPrefab;
        [SerializeField] private GameObject _xpItemPrefab;
        [SerializeField, Min(0)] private int _xpItemCount = 30;
        [SerializeField] private List<ItemSpawnEntry> _itemPool = new();
        [SerializeField, Min(0)] private int _generalItemCount = 15;

        [Header("Enemy Spawns")]
        [SerializeField] private List<EnemySpawnEntry> _enemyPool = new();
        [SerializeField, Min(0f)] private float _enemySpawnDelay = 45f;

        [Header("Objectives")]
        [SerializeField] private List<LevelObjective> _objectives = new();

        [Header("Progression")]
        [SerializeField] private List<LevelDefinition> _unlockedByLevels = new();
        [SerializeField] private bool _requireAllPrerequisites = true;

        // Properties
        public string LevelId => _levelId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MazeWidth => _mazeWidth;
        public int MazeHeight => _mazeHeight;
        public int CorridorWidth => _corridorWidth;
        public float BranchingFactor => _branchingFactor;
        public GameObject KeyItemPrefab => _keyItemPrefab;
        public GameObject XpItemPrefab => _xpItemPrefab;
        public int XpItemCount => _xpItemCount;
        public IReadOnlyList<ItemSpawnEntry> ItemPool => _itemPool;
        public int GeneralItemCount => _generalItemCount;
        public IReadOnlyList<EnemySpawnEntry> EnemyPool => _enemyPool;
        public float EnemySpawnDelay => _enemySpawnDelay;
        public IReadOnlyList<LevelObjective> Objectives => _objectives;
        public IReadOnlyList<LevelDefinition> UnlockedByLevels => _unlockedByLevels;
        public bool RequireAllPrerequisites => _requireAllPrerequisites;

        public MazeGeneratorConfig CreateMazeConfig()
        {
            var config = CreateInstance<MazeGeneratorConfig>();
            config.SetValues(_mazeWidth, _mazeHeight, _corridorWidth, _branchingFactor);
            return config;
        }
    }

    [System.Serializable]
    public class ItemSpawnEntry
    {
        public GameObject prefab;
        [Min(1)] public int weight = 1;
        [Min(0)] public int maxCount = 5;
    }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        [Min(0)] public int count = 1;
        [Min(0f)] public float spawnDelayOverride = -1f;
    }
}
```

**Step 2: Modify MazeGeneratorConfig to add SetValues method**

Modify: `Assets/Scripts/Maze/MazeGeneratorConfig.cs`

Add this method after line 40:

```csharp
public void SetValues(int width, int height, int corridorWidth, float branchingFactor)
{
    this.width = width;
    this.height = height;
    this.corridorWidth = corridorWidth;
    this.branchingFactor = branchingFactor;
}
```

**Step 3: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 4: Commit**

```bash
git add Assets/Scripts/Progression/LevelDefinition.cs Assets/Scripts/Maze/MazeGeneratorConfig.cs
git commit -m "Add LevelDefinition ScriptableObject"
```

---

## Task 3: Create Progression Save Data System

**Files:**
- Create: `Assets/Scripts/Progression/ProgressionSaveData.cs`

**Step 1: Create ProgressionSaveData.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Labyrinth.Progression
{
    [Serializable]
    public class ProgressionSaveData
    {
        public List<string> completedLevels = new();
        public List<LevelProgressData> levelProgress = new();

        private static string SavePath => Path.Combine(Application.persistentDataPath, "progression.json");

        public static ProgressionSaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                return new ProgressionSaveData();
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<ProgressionSaveData>(json) ?? new ProgressionSaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load progression data: {e.Message}");
                return new ProgressionSaveData();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save progression data: {e.Message}");
            }
        }

        public bool IsLevelCompleted(string levelId)
        {
            return completedLevels.Contains(levelId);
        }

        public void MarkLevelCompleted(string levelId)
        {
            if (!completedLevels.Contains(levelId))
            {
                completedLevels.Add(levelId);
            }
        }

        public LevelProgressData GetLevelProgress(string levelId)
        {
            return levelProgress.Find(p => p.levelId == levelId);
        }

        public LevelProgressData GetOrCreateLevelProgress(string levelId)
        {
            var progress = GetLevelProgress(levelId);
            if (progress == null)
            {
                progress = new LevelProgressData { levelId = levelId };
                levelProgress.Add(progress);
            }
            return progress;
        }
    }

    [Serializable]
    public class LevelProgressData
    {
        public string levelId;
        public List<ObjectiveProgressData> objectives = new();
        public int attempts;
        public string completedAt;

        public int GetObjectiveProgress(int objectiveIndex)
        {
            var obj = objectives.Find(o => o.index == objectiveIndex);
            return obj?.progress ?? 0;
        }

        public void SetObjectiveProgress(int objectiveIndex, int progress)
        {
            var obj = objectives.Find(o => o.index == objectiveIndex);
            if (obj == null)
            {
                obj = new ObjectiveProgressData { index = objectiveIndex };
                objectives.Add(obj);
            }
            obj.progress = progress;
        }
    }

    [Serializable]
    public class ObjectiveProgressData
    {
        public int index;
        public int progress;
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/Progression/ProgressionSaveData.cs
git commit -m "Add progression save data with JSON persistence"
```

---

## Task 4: Create LevelProgressionManager Singleton

**Files:**
- Create: `Assets/Scripts/Progression/LevelProgressionManager.cs`

**Step 1: Create LevelProgressionManager.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Labyrinth.Progression
{
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        [SerializeField] private List<LevelDefinition> _allLevels = new();

        private ProgressionSaveData _saveData;
        private LevelDefinition _currentLevel;

        public LevelDefinition CurrentLevel => _currentLevel;
        public IReadOnlyList<LevelDefinition> AllLevels => _allLevels;

        public event Action<string> OnLevelCompleted;
        public event Action OnProgressLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        public void LoadProgress()
        {
            _saveData = ProgressionSaveData.Load();
            OnProgressLoaded?.Invoke();
        }

        public void SaveProgress()
        {
            _saveData.Save();
        }

        public bool IsLevelUnlocked(LevelDefinition level)
        {
            if (level == null) return false;
            if (level.UnlockedByLevels == null || level.UnlockedByLevels.Count == 0)
                return true;

            if (level.RequireAllPrerequisites)
            {
                return level.UnlockedByLevels.All(prereq => IsLevelCompleted(prereq.LevelId));
            }
            else
            {
                return level.UnlockedByLevels.Any(prereq => IsLevelCompleted(prereq.LevelId));
            }
        }

        public bool IsLevelCompleted(string levelId)
        {
            return _saveData.IsLevelCompleted(levelId);
        }

        public List<LevelDefinition> GetAvailableLevels()
        {
            return _allLevels.Where(IsLevelUnlocked).ToList();
        }

        public void StartLevel(LevelDefinition level)
        {
            if (!IsLevelUnlocked(level))
            {
                Debug.LogWarning($"Attempted to start locked level: {level.LevelId}");
                return;
            }

            _currentLevel = level;

            var progress = _saveData.GetOrCreateLevelProgress(level.LevelId);
            progress.attempts++;
            SaveProgress();

            SceneManager.LoadScene("Game");
        }

        public void CompleteCurrentLevel()
        {
            if (_currentLevel == null) return;

            _saveData.MarkLevelCompleted(_currentLevel.LevelId);

            var progress = _saveData.GetOrCreateLevelProgress(_currentLevel.LevelId);
            progress.completedAt = DateTime.Now.ToString("o");

            SaveProgress();
            OnLevelCompleted?.Invoke(_currentLevel.LevelId);
        }

        public void SaveObjectiveProgress(int objectiveIndex, int progress)
        {
            if (_currentLevel == null) return;

            var levelProgress = _saveData.GetOrCreateLevelProgress(_currentLevel.LevelId);
            levelProgress.SetObjectiveProgress(objectiveIndex, progress);
            SaveProgress();
        }

        public int GetSavedObjectiveProgress(string levelId, int objectiveIndex)
        {
            var progress = _saveData.GetLevelProgress(levelId);
            return progress?.GetObjectiveProgress(objectiveIndex) ?? 0;
        }

        public void ReturnToLevelSelection()
        {
            _currentLevel = null;
            SceneManager.LoadScene("LevelSelection");
        }

        public void ResetAllProgress()
        {
            _saveData = new ProgressionSaveData();
            SaveProgress();
            OnProgressLoaded?.Invoke();
        }
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/Progression/LevelProgressionManager.cs
git commit -m "Add LevelProgressionManager singleton"
```

---

## Task 5: Create ObjectiveTracker Component

**Files:**
- Create: `Assets/Scripts/Progression/ObjectiveTracker.cs`

**Step 1: Create ObjectiveTracker.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Labyrinth.Core;
using Labyrinth.Items;

namespace Labyrinth.Progression
{
    public class ObjectiveTracker : MonoBehaviour
    {
        public static ObjectiveTracker Instance { get; private set; }

        private LevelDefinition _level;
        private List<ObjectiveState> _objectives = new();
        private float _elapsedTime;
        private bool _wasDetected;
        private int _enemiesDefeated;

        public IReadOnlyList<ObjectiveState> Objectives => _objectives;
        public bool AllObjectivesCompleted => _objectives.TrueForAll(o => o.IsCompleted);

        public event Action<int, int, int> OnObjectiveProgress;
        public event Action<int> OnObjectiveCompleted;
        public event Action OnAllObjectivesCompleted;
        public event Action OnObjectiveFailed;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var manager = LevelProgressionManager.Instance;
            if (manager == null || manager.CurrentLevel == null)
            {
                Debug.LogWarning("ObjectiveTracker: No current level set");
                return;
            }

            Initialize(manager.CurrentLevel);
        }

        public void Initialize(LevelDefinition level)
        {
            _level = level;
            _objectives.Clear();
            _elapsedTime = 0f;
            _wasDetected = false;
            _enemiesDefeated = 0;

            for (int i = 0; i < level.Objectives.Count; i++)
            {
                var objective = level.Objectives[i];
                var state = new ObjectiveState(objective, i);

                if (objective.PersistProgress)
                {
                    int savedProgress = LevelProgressionManager.Instance.GetSavedObjectiveProgress(level.LevelId, i);
                    state.CurrentProgress = savedProgress;
                }

                _objectives.Add(state);
            }
        }

        private void Update()
        {
            if (_level == null) return;

            _elapsedTime += Time.deltaTime;
            UpdateTimerObjectives();
        }

        private void UpdateTimerObjectives()
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.IsCompleted) continue;

                switch (state.Definition.Type)
                {
                    case ObjectiveType.SurviveTime:
                        state.CurrentProgress = Mathf.FloorToInt(_elapsedTime);
                        if (_elapsedTime >= state.Definition.TargetTime)
                        {
                            CompleteObjective(i);
                        }
                        break;

                    case ObjectiveType.TimeLimit:
                        if (_elapsedTime > state.Definition.TargetTime)
                        {
                            OnObjectiveFailed?.Invoke();
                            GameManager.Instance?.TriggerLose();
                        }
                        break;
                }
            }
        }

        public void OnItemCollected(ItemType itemType)
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.IsCompleted) continue;

                if (state.Definition.Type == ObjectiveType.CollectItems)
                {
                    if (string.IsNullOrEmpty(state.Definition.ItemType) ||
                        state.Definition.ItemType == itemType.ToString())
                    {
                        state.CurrentProgress++;
                        OnObjectiveProgress?.Invoke(i, state.CurrentProgress, state.Definition.TargetCount);

                        if (state.CurrentProgress >= state.Definition.TargetCount)
                        {
                            CompleteObjective(i);
                        }
                    }
                }
            }
        }

        public void OnEnemyDefeated()
        {
            _enemiesDefeated++;

            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.IsCompleted) continue;

                if (state.Definition.Type == ObjectiveType.DefeatEnemies)
                {
                    state.CurrentProgress = _enemiesDefeated;
                    OnObjectiveProgress?.Invoke(i, state.CurrentProgress, state.Definition.TargetCount);

                    if (state.CurrentProgress >= state.Definition.TargetCount)
                    {
                        CompleteObjective(i);
                    }
                }
            }
        }

        public void OnPlayerDetected()
        {
            _wasDetected = true;

            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.Definition.Type == ObjectiveType.NoDetection && !state.IsCompleted)
                {
                    state.Failed = true;
                    OnObjectiveFailed?.Invoke();
                }
            }
        }

        public void OnKeyItemCollected()
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.IsCompleted) continue;

                if (state.Definition.Type == ObjectiveType.ReachExit)
                {
                    CompleteObjective(i);
                }
            }

            SaveProgressOnEscape();

            if (AllObjectivesCompleted)
            {
                LevelProgressionManager.Instance?.CompleteCurrentLevel();
                GameManager.Instance?.TriggerWin();
            }
            else
            {
                GameManager.Instance?.TriggerEscape();
            }
        }

        private void SaveProgressOnEscape()
        {
            var manager = LevelProgressionManager.Instance;
            if (manager == null || _level == null) return;

            for (int i = 0; i < _objectives.Count; i++)
            {
                var state = _objectives[i];
                if (state.Definition.PersistProgress && !state.Failed)
                {
                    manager.SaveObjectiveProgress(i, state.CurrentProgress);
                }
            }
        }

        private void CompleteObjective(int index)
        {
            var state = _objectives[index];
            state.IsCompleted = true;
            OnObjectiveCompleted?.Invoke(index);

            if (AllObjectivesCompleted)
            {
                OnAllObjectivesCompleted?.Invoke();
            }
        }
    }

    public class ObjectiveState
    {
        public LevelObjective Definition { get; }
        public int Index { get; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public bool Failed { get; set; }

        public ObjectiveState(LevelObjective definition, int index)
        {
            Definition = definition;
            Index = index;
            CurrentProgress = 0;
            IsCompleted = false;
            Failed = false;
        }
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/Progression/ObjectiveTracker.cs
git commit -m "Add ObjectiveTracker component for objective monitoring"
```

---

## Task 6: Modify GameManager for Escape vs Win

**Files:**
- Modify: `Assets/Scripts/Core/GameManager.cs`

**Step 1: Add OnLevelEscape event and TriggerEscape method**

Open `Assets/Scripts/Core/GameManager.cs` and modify:

After line 19 (after OnGameLose event), add:
```csharp
public event System.Action OnLevelEscape;
```

After the TriggerLose method (around line 83), add this new method:
```csharp
public void TriggerEscape()
{
    if (CurrentState != GameState.Playing) return;

    CurrentState = GameState.Won;
    OnLevelEscape?.Invoke();
    StartCoroutine(ReturnToLevelSelectionAfterDelay(1.5f));
}

private IEnumerator ReturnToLevelSelectionAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    ReturnToLevelSelection();
}

public void ReturnToLevelSelection()
{
    Labyrinth.Progression.LevelProgressionManager.Instance?.ReturnToLevelSelection();
}
```

**Step 2: Add using statement**

Add at the top of the file:
```csharp
using Labyrinth.Progression;
```

**Step 3: Modify TriggerWin to also return to level selection**

Replace the existing ReturnToMainMenuAfterDelay call in TriggerWin (around line 68) to use level selection:

Find:
```csharp
StartCoroutine(ReturnToMainMenuAfterDelay(1.5f));
```

Replace with:
```csharp
StartCoroutine(ReturnToLevelSelectionAfterDelay(1.5f));
```

**Step 4: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 5: Commit**

```bash
git add Assets/Scripts/Core/GameManager.cs
git commit -m "Add escape vs win distinction to GameManager"
```

---

## Task 7: Modify KeyItem to Use ObjectiveTracker

**Files:**
- Modify: `Assets/Scripts/Items/KeyItem.cs`

**Step 1: Modify OnCollected method**

Open `Assets/Scripts/Items/KeyItem.cs` and replace the OnCollected method (lines 19-22):

```csharp
protected override void OnCollected(GameObject player)
{
    var tracker = Labyrinth.Progression.ObjectiveTracker.Instance;
    if (tracker != null)
    {
        tracker.OnKeyItemCollected();
    }
    else
    {
        // Fallback for when no progression system is active
        GameManager.Instance?.TriggerWin();
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/Items/KeyItem.cs
git commit -m "Update KeyItem to use ObjectiveTracker"
```

---

## Task 8: Modify MazeInitializer to Use LevelDefinition

**Files:**
- Modify: `Assets/Scripts/Maze/MazeInitializer.cs`

**Step 1: Add level-aware initialization**

Open `Assets/Scripts/Maze/MazeInitializer.cs`.

Add using statement at top:
```csharp
using Labyrinth.Progression;
```

Modify the GenerateMaze method to check for current level. Find the start of GenerateMaze (line 37) and modify it:

Replace the config initialization section (where it creates the maze generator) with:

```csharp
private void GenerateMaze()
{
    MazeGeneratorConfig effectiveConfig = mazeConfig;

    // Check if we have an active level from progression
    var levelManager = LevelProgressionManager.Instance;
    if (levelManager != null && levelManager.CurrentLevel != null)
    {
        var level = levelManager.CurrentLevel;
        effectiveConfig = level.CreateMazeConfig();

        // Reset player systems for fresh start
        if (PlayerLevelSystem.Instance != null)
        {
            PlayerLevelSystem.Instance.ResetLevel();
        }
    }

    // Create generator with effective config
    _mazeGenerator = effectiveConfig.CreateGenerator();
    // ... rest of existing code continues
```

**Step 2: Add ObjectiveTracker to scene initialization**

After the player is spawned (around line 85), add:

```csharp
// Spawn ObjectiveTracker if not present
if (FindFirstObjectByType<ObjectiveTracker>() == null)
{
    var trackerObj = new GameObject("ObjectiveTracker");
    trackerObj.AddComponent<ObjectiveTracker>();
}
```

**Step 3: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 4: Commit**

```bash
git add Assets/Scripts/Maze/MazeInitializer.cs
git commit -m "Update MazeInitializer to use LevelDefinition config"
```

---

## Task 9: Create Level Selection Scene and UI Base

**Files:**
- Create: `Assets/Scenes/LevelSelection.unity`
- Create: `Assets/Scripts/UI/LevelSelectionUI.cs`

**Step 1: Create LevelSelectionUI.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Labyrinth.Progression;

namespace Labyrinth.UI
{
    public class LevelSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private GameObject _levelNodePrefab;
        [SerializeField] private GameObject _connectionLinePrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Layout")]
        [SerializeField] private float _horizontalSpacing = 300f;
        [SerializeField] private float _verticalSpacing = 200f;

        [Header("Detail Panel")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TextMeshProUGUI _detailTitle;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private Transform _objectivesContainer;
        [SerializeField] private GameObject _objectiveEntryPrefab;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeDetailButton;

        private Dictionary<string, LevelNodeUI> _nodes = new();
        private LevelDefinition _selectedLevel;

        private void Start()
        {
            _backButton.onClick.AddListener(OnBackClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _closeDetailButton.onClick.AddListener(CloseDetailPanel);

            CloseDetailPanel();
            BuildTree();
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(OnBackClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
            _closeDetailButton.onClick.RemoveListener(CloseDetailPanel);
        }

        private void BuildTree()
        {
            var manager = LevelProgressionManager.Instance;
            if (manager == null)
            {
                Debug.LogError("LevelProgressionManager not found");
                return;
            }

            ClearTree();
            var levels = manager.AllLevels;

            // Calculate tree layout
            var layout = CalculateLayout(levels);

            // Create nodes
            foreach (var level in levels)
            {
                CreateNode(level, layout[level.LevelId]);
            }

            // Create connection lines
            foreach (var level in levels)
            {
                CreateConnections(level);
            }

            // Update node states
            RefreshNodeStates();
        }

        private Dictionary<string, Vector2> CalculateLayout(IReadOnlyList<LevelDefinition> levels)
        {
            var layout = new Dictionary<string, Vector2>();
            var depths = new Dictionary<string, int>();
            var siblingCounts = new Dictionary<int, int>();

            // Calculate depth for each level
            foreach (var level in levels)
            {
                int depth = CalculateDepth(level, depths);
                depths[level.LevelId] = depth;

                if (!siblingCounts.ContainsKey(depth))
                    siblingCounts[depth] = 0;
                siblingCounts[depth]++;
            }

            // Assign positions
            var depthCurrentIndex = new Dictionary<int, int>();
            foreach (var level in levels)
            {
                int depth = depths[level.LevelId];
                if (!depthCurrentIndex.ContainsKey(depth))
                    depthCurrentIndex[depth] = 0;

                int siblingIndex = depthCurrentIndex[depth]++;
                int totalSiblings = siblingCounts[depth];

                float x = depth * _horizontalSpacing;
                float y = (siblingIndex - (totalSiblings - 1) / 2f) * _verticalSpacing;

                layout[level.LevelId] = new Vector2(x, y);
            }

            return layout;
        }

        private int CalculateDepth(LevelDefinition level, Dictionary<string, int> cache)
        {
            if (cache.TryGetValue(level.LevelId, out int cached))
                return cached;

            if (level.UnlockedByLevels == null || level.UnlockedByLevels.Count == 0)
                return 0;

            int maxParentDepth = 0;
            foreach (var parent in level.UnlockedByLevels)
            {
                maxParentDepth = Mathf.Max(maxParentDepth, CalculateDepth(parent, cache));
            }

            return maxParentDepth + 1;
        }

        private void CreateNode(LevelDefinition level, Vector2 position)
        {
            var nodeObj = Instantiate(_levelNodePrefab, _nodeContainer);
            var rectTransform = nodeObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;

            var nodeUI = nodeObj.GetComponent<LevelNodeUI>();
            nodeUI.Setup(level, OnNodeClicked);

            _nodes[level.LevelId] = nodeUI;
        }

        private void CreateConnections(LevelDefinition level)
        {
            if (level.UnlockedByLevels == null) return;

            foreach (var parent in level.UnlockedByLevels)
            {
                if (_nodes.TryGetValue(parent.LevelId, out var parentNode) &&
                    _nodes.TryGetValue(level.LevelId, out var childNode))
                {
                    CreateConnectionLine(parentNode.transform.position, childNode.transform.position);
                }
            }
        }

        private void CreateConnectionLine(Vector3 start, Vector3 end)
        {
            if (_connectionLinePrefab == null) return;

            var lineObj = Instantiate(_connectionLinePrefab, _nodeContainer);
            lineObj.transform.SetAsFirstSibling();

            var line = lineObj.GetComponent<UILineRenderer>();
            if (line != null)
            {
                line.SetPositions(start, end);
            }
        }

        private void ClearTree()
        {
            foreach (Transform child in _nodeContainer)
            {
                Destroy(child.gameObject);
            }
            _nodes.Clear();
        }

        private void RefreshNodeStates()
        {
            var manager = LevelProgressionManager.Instance;
            foreach (var kvp in _nodes)
            {
                var level = manager.AllLevels.Find(l => l.LevelId == kvp.Key);
                if (level != null)
                {
                    bool unlocked = manager.IsLevelUnlocked(level);
                    bool completed = manager.IsLevelCompleted(level.LevelId);
                    kvp.Value.UpdateState(unlocked, completed);
                }
            }
        }

        private void OnNodeClicked(LevelDefinition level)
        {
            _selectedLevel = level;
            ShowDetailPanel(level);
        }

        private void ShowDetailPanel(LevelDefinition level)
        {
            _detailPanel.SetActive(true);
            _detailTitle.text = level.DisplayName;
            _detailDescription.text = level.Description;

            // Clear existing objectives
            foreach (Transform child in _objectivesContainer)
            {
                Destroy(child.gameObject);
            }

            // Add objective entries
            var manager = LevelProgressionManager.Instance;
            for (int i = 0; i < level.Objectives.Count; i++)
            {
                var objective = level.Objectives[i];
                var entryObj = Instantiate(_objectiveEntryPrefab, _objectivesContainer);
                var entryText = entryObj.GetComponentInChildren<TextMeshProUGUI>();

                int progress = manager.GetSavedObjectiveProgress(level.LevelId, i);
                string progressText = objective.TargetCount > 0
                    ? $" ({progress}/{objective.TargetCount})"
                    : "";

                entryText.text = $"• {objective.Description}{progressText}";
            }

            // Enable start button only if level is unlocked
            _startButton.interactable = manager.IsLevelUnlocked(level);
        }

        private void CloseDetailPanel()
        {
            _detailPanel.SetActive(false);
            _selectedLevel = null;
        }

        private void OnStartClicked()
        {
            if (_selectedLevel != null)
            {
                LevelProgressionManager.Instance?.StartLevel(_selectedLevel);
            }
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/LevelSelectionUI.cs
git commit -m "Add LevelSelectionUI for tree display"
```

---

## Task 10: Create LevelNodeUI Component

**Files:**
- Create: `Assets/Scripts/UI/LevelNodeUI.cs`

**Step 1: Create LevelNodeUI.cs**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Progression;

namespace Labyrinth.UI
{
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _completedIcon;
        [SerializeField] private TextMeshProUGUI _progressText;

        [Header("Colors")]
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color _availableColor = Color.white;
        [SerializeField] private Color _completedColor = new Color(0.7f, 1f, 0.7f, 1f);

        private LevelDefinition _level;
        private Action<LevelDefinition> _onClicked;

        public void Setup(LevelDefinition level, Action<LevelDefinition> onClicked)
        {
            _level = level;
            _onClicked = onClicked;

            _nameText.text = level.DisplayName;

            if (_iconImage != null && level.Icon != null)
            {
                _iconImage.sprite = level.Icon;
                _iconImage.enabled = true;
            }
            else if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }

            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void UpdateState(bool isUnlocked, bool isCompleted)
        {
            _lockIcon.SetActive(!isUnlocked);
            _completedIcon.SetActive(isCompleted);
            _button.interactable = isUnlocked;

            if (!isUnlocked)
            {
                _backgroundImage.color = _lockedColor;
            }
            else if (isCompleted)
            {
                _backgroundImage.color = _completedColor;
            }
            else
            {
                _backgroundImage.color = _availableColor;
            }

            UpdateProgressText();
        }

        private void UpdateProgressText()
        {
            if (_progressText == null || _level == null) return;

            var manager = LevelProgressionManager.Instance;
            if (manager == null) return;

            int completedObjectives = 0;
            int totalObjectives = _level.Objectives.Count;

            for (int i = 0; i < totalObjectives; i++)
            {
                var objective = _level.Objectives[i];
                int progress = manager.GetSavedObjectiveProgress(_level.LevelId, i);

                if (objective.TargetCount > 0 && progress >= objective.TargetCount)
                {
                    completedObjectives++;
                }
                else if (objective.TargetCount == 0 && manager.IsLevelCompleted(_level.LevelId))
                {
                    completedObjectives++;
                }
            }

            if (totalObjectives > 0)
            {
                _progressText.text = $"{completedObjectives}/{totalObjectives}";
            }
            else
            {
                _progressText.text = "";
            }
        }

        private void OnClick()
        {
            _onClicked?.Invoke(_level);
        }
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/LevelNodeUI.cs
git commit -m "Add LevelNodeUI component"
```

---

## Task 11: Create UILineRenderer for Connections

**Files:**
- Create: `Assets/Scripts/UI/UILineRenderer.cs`

**Step 1: Create UILineRenderer.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : Graphic
    {
        [SerializeField] private float _lineWidth = 4f;
        [SerializeField] private Color _lineColor = Color.white;

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        public void SetPositions(Vector3 worldStart, Vector3 worldEnd)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldStart),
                canvas.worldCamera,
                out _startPoint
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldEnd),
                canvas.worldCamera,
                out _endPoint
            );

            SetVerticesDirty();
        }

        public void SetLocalPositions(Vector2 start, Vector2 end)
        {
            _startPoint = start;
            _endPoint = end;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_startPoint == _endPoint) return;

            Vector2 direction = (_endPoint - _startPoint).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * _lineWidth * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = _lineColor;

            // Create quad for line
            vertex.position = _startPoint - perpendicular;
            vh.AddVert(vertex);

            vertex.position = _startPoint + perpendicular;
            vh.AddVert(vertex);

            vertex.position = _endPoint + perpendicular;
            vh.AddVert(vertex);

            vertex.position = _endPoint - perpendicular;
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/UILineRenderer.cs
git commit -m "Add UILineRenderer for level connections"
```

---

## Task 12: Modify MainMenuUI

**Files:**
- Modify: `Assets/Scripts/UI/MainMenuUI.cs`

**Step 1: Update button text and navigation**

Open `Assets/Scripts/UI/MainMenuUI.cs` and modify the OnPlayClicked method (around line 35):

```csharp
private void OnPlayClicked()
{
    SceneManager.LoadScene("LevelSelection");
}
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/MainMenuUI.cs
git commit -m "Update MainMenuUI to navigate to LevelSelection"
```

---

## Task 13: Create Level Definition Assets

**Files:**
- Create: `Assets/Config/Levels/Level_01_FirstSteps.asset`
- Create: `Assets/Config/Levels/Level_02_Crossroads.asset`
- Create: `Assets/Config/Levels/Level_03A_TheHunt.asset`
- Create: `Assets/Config/Levels/Level_03B_ShadowPath.asset`
- Create: `Assets/Config/Levels/Level_04_TheDeep.asset`

**Step 1: Create Levels folder**

```bash
mkdir -p Assets/Config/Levels
```

**Step 2: Create levels via Unity Editor**

In Unity Editor:
1. Right-click `Assets/Config/Levels`
2. Create → Labyrinth → Level Definition
3. Name it `Level_01_FirstSteps`
4. Configure:
   - Level ID: `level_01`
   - Display Name: `The First Steps`
   - Description: `Your first expedition. Find the key and escape.`
   - Maze Width: 21, Height: 21
   - Corridor Width: 3
   - Branching Factor: 0.5
   - XP Item Count: 20
   - Enemy Spawn Delay: 60
   - Objectives: Add one `ReachExit` type with description "Find the key and escape"
   - Unlocked By Levels: (empty - starting level)

5. Repeat for other levels with appropriate settings from the design document.

**Step 3: Commit**

```bash
git add Assets/Config/Levels/
git commit -m "Add initial level definition assets"
```

---

## Task 14: Create LevelSelection Scene

**Files:**
- Create: `Assets/Scenes/LevelSelection.unity`

**Step 1: Create scene in Unity Editor**

1. File → New Scene → Basic 2D
2. Save as `Assets/Scenes/LevelSelection.unity`
3. Add to Build Settings (File → Build Settings → Add Open Scenes)

4. Create hierarchy:
```
LevelSelection (Scene)
├── LevelProgressionManager (Empty GameObject)
│   └── Add LevelProgressionManager component
│   └── Assign all level assets to _allLevels list
├── EventSystem
├── Canvas
│   ├── Header
│   │   ├── BackButton (Button)
│   │   └── Title (TextMeshPro - "Expeditions")
│   ├── ScrollView (Scroll Rect - Horizontal)
│   │   └── Viewport
│   │       └── NodeContainer (Content)
│   └── DetailPanel (Initially inactive)
│       ├── Background
│       ├── Title (TextMeshPro)
│       ├── Description (TextMeshPro)
│       ├── ObjectivesContainer
│       ├── StartButton
│       └── CloseButton
└── Camera
```

5. Add LevelSelectionUI component to Canvas and wire up references

**Step 2: Create prefabs**

Create `Assets/Prefabs/UI/LevelNode.prefab`:
- Button with Image background
- Icon Image (child)
- Name Text (TextMeshPro, child)
- Lock Icon (Image, child)
- Completed Icon (Image, child)
- Progress Text (TextMeshPro, child)
- Add LevelNodeUI component

Create `Assets/Prefabs/UI/ConnectionLine.prefab`:
- Empty GameObject with UILineRenderer component

Create `Assets/Prefabs/UI/ObjectiveEntry.prefab`:
- Text (TextMeshPro)

**Step 3: Commit**

```bash
git add Assets/Scenes/LevelSelection.unity Assets/Prefabs/UI/
git commit -m "Add LevelSelection scene and UI prefabs"
```

---

## Task 15: Add BaseItem Item Collection Event

**Files:**
- Modify: `Assets/Scripts/Items/BaseItem.cs`

**Step 1: Notify ObjectiveTracker on item collection**

Open `Assets/Scripts/Items/BaseItem.cs`.

Add using statement:
```csharp
using Labyrinth.Progression;
```

In the OnTriggerEnter2D method, after an item is collected (both storable and non-storable paths), add notification to ObjectiveTracker.

Find the section where items are collected and add:
```csharp
// After item collection logic, notify objective tracker
ObjectiveTracker.Instance?.OnItemCollected(ItemType);
```

**Step 2: Verify no compilation errors**

Run: Check Unity console for errors after save.

**Step 3: Commit**

```bash
git add Assets/Scripts/Items/BaseItem.cs
git commit -m "Add item collection notification to ObjectiveTracker"
```

---

## Task 16: Integration Testing

**Step 1: Test level selection flow**

1. Play MainMenu scene
2. Click "Look for Expedition" (formerly "Play")
3. Verify LevelSelection scene loads
4. Verify level tree displays correctly
5. Click on an unlocked level
6. Verify detail panel shows
7. Click "Start Expedition"
8. Verify Game scene loads with level config

**Step 2: Test objective tracking**

1. Start Level 1
2. Collect the key
3. Verify level completes (objectives met)
4. Verify return to LevelSelection
5. Verify Level 1 shows as completed
6. Verify Level 2 is now unlocked

**Step 3: Test progress persistence**

1. Start Level 2 (with item collection objective)
2. Collect some items
3. Collect key (escape without completing all objectives)
4. Verify return to LevelSelection
5. Check Level 2 detail panel shows saved progress
6. Restart Level 2
7. Verify progress is preserved

**Step 4: Test death resets non-persistent progress**

1. Start a level with non-persistent objective
2. Make progress
3. Get caught by enemy
4. Verify return to LevelSelection
5. Check that non-persistent progress was NOT saved

**Step 5: Commit**

```bash
git add -A
git commit -m "Complete level progression system integration"
```

---

## Summary

This plan implements:

1. **Core Data Structures** (Tasks 1-2): LevelObjective and LevelDefinition ScriptableObjects
2. **Persistence System** (Tasks 3-4): JSON-based save data and LevelProgressionManager
3. **Objective Tracking** (Task 5): ObjectiveTracker monitors gameplay progress
4. **Game Integration** (Tasks 6-8, 15): GameManager escape/win, KeyItem, MazeInitializer, BaseItem
5. **UI System** (Tasks 9-12): LevelSelectionUI, LevelNodeUI, UILineRenderer, MainMenuUI
6. **Content** (Tasks 13-14): Level assets and scene setup
7. **Testing** (Task 16): Integration verification
