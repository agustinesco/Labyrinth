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
