using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private Text levelText;

        [Header("Card References")]
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardBackgrounds;
        [SerializeField] private Image[] cardIcons;
        [SerializeField] private Text[] cardTitles;
        [SerializeField] private Text[] cardDescriptions;

        private List<Upgrade> _currentUpgrades;
        private float _previousTimeScale;
        private bool _isSubscribed;

        private void Start()
        {
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            TrySubscribe();

            // Set up card button listeners
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i; // Capture for closure
                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.AddListener(() => OnCardSelected(index));
                }
            }
        }

        private void Update()
        {
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
                PlayerLevelSystem.Instance.OnLevelUp += ShowLevelUpPanel;
                _isSubscribed = true;
                Debug.Log("LevelUpUI: Successfully subscribed to OnLevelUp event");
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && PlayerLevelSystem.Instance != null)
            {
                PlayerLevelSystem.Instance.OnLevelUp -= ShowLevelUpPanel;
            }
        }

        private void ShowLevelUpPanel(int newLevel)
        {
            Debug.Log($"LevelUpUI: ShowLevelUpPanel called for level {newLevel}");

            if (levelUpPanel == null)
            {
                Debug.LogError("LevelUpUI: levelUpPanel is null!");
                return;
            }

            if (UpgradeManager.Instance == null)
            {
                Debug.LogError("LevelUpUI: UpgradeManager.Instance is null!");
                return;
            }

            // Pause the game
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Update level text
            if (levelText != null)
            {
                levelText.text = $"Level {newLevel}!";
            }

            // Get random upgrades
            _currentUpgrades = UpgradeManager.Instance.GetRandomUpgrades(3);

            // Update card displays
            for (int i = 0; i < cardButtons.Length && i < _currentUpgrades.Count; i++)
            {
                var upgrade = _currentUpgrades[i];

                // Set card background to a darker version of the upgrade color
                if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                {
                    Color bgColor = upgrade.CardColor * 0.3f;
                    bgColor.a = 0.95f;
                    cardBackgrounds[i].color = bgColor;
                }

                // Set icon to the upgrade color
                if (cardIcons != null && i < cardIcons.Length && cardIcons[i] != null)
                {
                    cardIcons[i].color = upgrade.CardColor;
                }

                if (cardTitles != null && i < cardTitles.Length && cardTitles[i] != null)
                {
                    cardTitles[i].text = upgrade.DisplayName;
                }

                if (cardDescriptions != null && i < cardDescriptions.Length && cardDescriptions[i] != null)
                {
                    cardDescriptions[i].text = upgrade.Description;
                }
            }

            levelUpPanel.SetActive(true);
            Debug.Log($"LevelUpUI: Panel activated, showing {_currentUpgrades?.Count ?? 0} upgrades");
        }

        private void OnCardSelected(int index)
        {
            if (_currentUpgrades == null || index >= _currentUpgrades.Count)
                return;

            // Apply the selected upgrade
            var selectedUpgrade = _currentUpgrades[index];
            UpgradeManager.Instance?.ApplyUpgrade(selectedUpgrade);

            // Hide panel and resume game
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            Time.timeScale = _previousTimeScale > 0 ? _previousTimeScale : 1f;
        }
    }
}
