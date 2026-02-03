using UnityEngine;
using System;

namespace Labyrinth.Leveling
{
    public class PlayerLevelSystem : MonoBehaviour
    {
        public static PlayerLevelSystem Instance { get; private set; }

        private int _currentLevel = 1;
        private int _currentXP = 0;
        private float _permanentSpeedBonus = 0f;
        private float _permanentVisionBonus = 0f;

        public int CurrentLevel => _currentLevel;
        public int CurrentXP => _currentXP;
        public int XPForNextLevel => _currentLevel * 5;
        public float PermanentSpeedBonus => _permanentSpeedBonus;
        public float PermanentVisionBonus => _permanentVisionBonus;

        public event Action<int, int> OnXPChanged; // (currentXP, xpForNextLevel)
        public event Action<int> OnLevelUp; // (newLevel)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void AddXP(int amount)
        {
            _currentXP += amount;

            // Check for level up
            while (_currentXP >= XPForNextLevel)
            {
                _currentXP -= XPForNextLevel;
                _currentLevel++;
                OnLevelUp?.Invoke(_currentLevel);
            }

            OnXPChanged?.Invoke(_currentXP, XPForNextLevel);
        }

        public void ApplyPermanentSpeedBonus(float bonus)
        {
            _permanentSpeedBonus += bonus;
        }

        public void ApplyPermanentVisionBonus(float bonus)
        {
            _permanentVisionBonus += bonus;
        }

        public void ResetLevel()
        {
            _currentLevel = 1;
            _currentXP = 0;
            _permanentSpeedBonus = 0f;
            _permanentVisionBonus = 0f;
            OnXPChanged?.Invoke(_currentXP, XPForNextLevel);
        }
    }
}
