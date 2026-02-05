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
