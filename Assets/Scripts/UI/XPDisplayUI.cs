using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    public class XPDisplayUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text xpText;

        private bool _isSubscribed;

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            // Keep trying to subscribe until we succeed (handles timing issues)
            if (!_isSubscribed)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (_isSubscribed) return;

            if (PlayerLevelSystem.Instance != null)
            {
                PlayerLevelSystem.Instance.OnXPChanged += UpdateXPDisplay;
                PlayerLevelSystem.Instance.OnLevelUp += UpdateLevelDisplay;
                _isSubscribed = true;

                // Initialize display
                UpdateLevelDisplay(PlayerLevelSystem.Instance.CurrentLevel);
                UpdateXPDisplay(PlayerLevelSystem.Instance.CurrentXP, PlayerLevelSystem.Instance.XPForNextLevel);
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && PlayerLevelSystem.Instance != null)
            {
                PlayerLevelSystem.Instance.OnXPChanged -= UpdateXPDisplay;
                PlayerLevelSystem.Instance.OnLevelUp -= UpdateLevelDisplay;
            }
        }

        private void UpdateLevelDisplay(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }
        }

        private void UpdateXPDisplay(int currentXP, int xpForNextLevel)
        {
            if (xpText != null)
            {
                xpText.text = $"{currentXP}/{xpForNextLevel}";
            }
        }
    }
}
