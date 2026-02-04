using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private TMP_Text levelText;

        [Header("Card Setup")]
        [SerializeField] private LevelUpCard cardPrefab;
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private int numberOfCards = 3;

        private List<LevelUpCard> _cardInstances = new List<LevelUpCard>();
        private List<LevelUpUpgrade> _currentUpgrades;
        private float _previousTimeScale;
        private bool _isSubscribed;

        private void Awake()
        {
            // Subscribe early - Awake runs even on inactive objects
            TrySubscribe();
        }

        private void Start()
        {
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            CreateCardInstances();
        }

        private void CreateCardInstances()
        {
            if (cardPrefab == null || cardsContainer == null)
            {
                Debug.LogError("LevelUpUI: cardPrefab or cardsContainer is not assigned!");
                return;
            }

            // Clear existing cards
            foreach (var card in _cardInstances)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _cardInstances.Clear();

            // Create new card instances
            for (int i = 0; i < numberOfCards; i++)
            {
                var cardInstance = Instantiate(cardPrefab, cardsContainer);
                cardInstance.gameObject.SetActive(false);
                _cardInstances.Add(cardInstance);
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
            _currentUpgrades = UpgradeManager.Instance.GetRandomUpgrades(numberOfCards);

            // Ensure we have card instances
            if (_cardInstances.Count == 0)
            {
                CreateCardInstances();
            }

            // Setup card displays
            for (int i = 0; i < _cardInstances.Count; i++)
            {
                if (i < _currentUpgrades.Count)
                {
                    _cardInstances[i].gameObject.SetActive(true);
                    _cardInstances[i].Setup(_currentUpgrades[i], OnCardSelected);
                }
                else
                {
                    _cardInstances[i].gameObject.SetActive(false);
                }
            }

            levelUpPanel.SetActive(true);
            Debug.Log($"LevelUpUI: Panel activated, showing {_currentUpgrades?.Count ?? 0} upgrades");
        }

        private void OnCardSelected(LevelUpUpgrade upgrade)
        {
            if (upgrade == null)
                return;

            // Apply the selected upgrade
            UpgradeManager.Instance?.ApplyUpgrade(upgrade);

            // Hide panel and resume game
            if (levelUpPanel != null)
            {
                levelUpPanel.SetActive(false);
            }

            Time.timeScale = _previousTimeScale > 0 ? _previousTimeScale : 1f;
        }
    }
}
