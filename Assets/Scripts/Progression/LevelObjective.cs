using System;
using UnityEngine;

namespace Labyrinth.Progression
{
    public enum ObjectiveType
    {
        ReachExit,
        CollectItems,
        SurviveTime,
        TimeLimit
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
