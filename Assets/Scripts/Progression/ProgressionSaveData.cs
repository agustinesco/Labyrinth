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
