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
