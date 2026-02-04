using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    /// <summary>
    /// Displays player XP as a progress bar.
    /// Uses tiled sprites for the bar body and end cap sprites for decoration.
    /// </summary>
    public class XPDisplayUI : MonoBehaviour
    {
        [Header("Bar References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private RectTransform fillEndRect;

        [Header("Bar Settings")]
        [SerializeField] private float fillEndWidth = 8f;

        private bool _isSubscribed;
        private RectTransform _rectTransform;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
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
                PlayerLevelSystem.Instance.OnXPChanged += UpdateXPBar;
                PlayerLevelSystem.Instance.OnLevelUp += OnLevelUp;
                _isSubscribed = true;

                // Initialize display
                UpdateXPBar(PlayerLevelSystem.Instance.CurrentXP, PlayerLevelSystem.Instance.XPForNextLevel);
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && PlayerLevelSystem.Instance != null)
            {
                PlayerLevelSystem.Instance.OnXPChanged -= UpdateXPBar;
                PlayerLevelSystem.Instance.OnLevelUp -= OnLevelUp;
            }
        }

        private void OnLevelUp(int level)
        {
            // Reset bar on level up, then update with new values
            if (PlayerLevelSystem.Instance != null)
            {
                UpdateXPBar(PlayerLevelSystem.Instance.CurrentXP, PlayerLevelSystem.Instance.XPForNextLevel);
            }
        }

        /// <summary>
        /// Updates the XP bar fill based on current progress.
        /// </summary>
        private void UpdateXPBar(int currentXP, int xpForNextLevel)
        {
            if (fillRect == null) return;

            float progress = xpForNextLevel > 0 ? (float)currentXP / xpForNextLevel : 0f;
            progress = Mathf.Clamp01(progress);

            // Get the current bar width from RectTransform (responsive)
            float barWidth = _rectTransform != null ? _rectTransform.rect.width : 200f;

            // Calculate fill width (leaving room for end cap)
            float maxFillWidth = barWidth - fillEndWidth;
            float fillWidth = maxFillWidth * progress;

            // Ensure minimum width when there's any progress
            if (progress > 0 && fillWidth < 4f)
            {
                fillWidth = 4f;
            }

            // Update fill tiled section width
            fillRect.sizeDelta = new Vector2(fillWidth, fillRect.sizeDelta.y);

            // Position the fill end cap at the right edge of the fill
            if (fillEndRect != null)
            {
                fillEndRect.anchoredPosition = new Vector2(fillWidth, 0);

                // Hide end cap when no progress
                fillEndRect.gameObject.SetActive(progress > 0);
            }
        }
    }
}
