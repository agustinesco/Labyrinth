using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Labyrinth.UI.Bestiary
{
    /// <summary>
    /// Manages the bestiary grid display, showing all enemy entries.
    /// </summary>
    public class BestiaryGridUI : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject entryPrefab;

        [Header("Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private TMP_Text detailBehaviorText;
        [SerializeField] private Button closeDetailButton;

        [Header("Counter")]
        [SerializeField] private TMP_Text discoveryCounterText;

        [Header("Locked Entry Settings")]
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color unlockedColor = Color.white;

        private List<BestiaryEntryUI> _entryUIs = new List<BestiaryEntryUI>();
        private BestiaryManager _bestiaryManager;

        private void Start()
        {
            _bestiaryManager = BestiaryManager.Instance;

            if (closeDetailButton != null)
            {
                closeDetailButton.onClick.AddListener(HideDetail);
            }

            // Hide detail panel initially
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }

            BuildGrid();
            UpdateCounter();
        }

        private void OnEnable()
        {
            if (_bestiaryManager != null)
            {
                _bestiaryManager.OnEnemyDiscovered += OnEnemyDiscovered;
            }

            RefreshGrid();
            UpdateCounter();
        }

        private void OnDisable()
        {
            if (_bestiaryManager != null)
            {
                _bestiaryManager.OnEnemyDiscovered -= OnEnemyDiscovered;
            }
        }

        private void BuildGrid()
        {
            if (_bestiaryManager == null || _bestiaryManager.Data == null) return;

            // Clear existing entries
            foreach (var entry in _entryUIs)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }
            _entryUIs.Clear();

            // Create entries for each enemy
            foreach (var entryData in _bestiaryManager.Data.Entries)
            {
                CreateEntryUI(entryData);
            }
        }

        private void CreateEntryUI(BestiaryEntryData entryData)
        {
            if (gridContainer == null) return;

            GameObject entryObj;
            if (entryPrefab != null)
            {
                entryObj = Instantiate(entryPrefab, gridContainer);
            }
            else
            {
                entryObj = CreateDefaultEntryUI();
                entryObj.transform.SetParent(gridContainer, false);
            }

            var entryUI = entryObj.GetComponent<BestiaryEntryUI>();
            if (entryUI == null)
            {
                entryUI = entryObj.AddComponent<BestiaryEntryUI>();
            }

            bool isDiscovered = _bestiaryManager.IsEnemyDiscovered(entryData.enemyId);
            entryUI.Setup(entryData, isDiscovered, lockedIcon, lockedColor, unlockedColor);
            entryUI.OnEntryClicked += ShowDetail;

            _entryUIs.Add(entryUI);
        }

        private GameObject CreateDefaultEntryUI()
        {
            // Create a default entry UI if no prefab is assigned
            var entryObj = new GameObject("BestiaryEntry");

            var rectTransform = entryObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 80);

            var image = entryObj.AddComponent<Image>();
            image.color = lockedColor;

            var button = entryObj.AddComponent<Button>();

            // Add icon as child
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(entryObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8, 8);
            iconRect.offsetMax = new Vector2(-8, -8);

            var iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;

            return entryObj;
        }

        private void RefreshGrid()
        {
            if (_bestiaryManager == null) return;

            foreach (var entryUI in _entryUIs)
            {
                if (entryUI != null && entryUI.EntryData != null)
                {
                    bool isDiscovered = _bestiaryManager.IsEnemyDiscovered(entryUI.EntryData.enemyId);
                    entryUI.SetDiscovered(isDiscovered);
                }
            }
        }

        private void UpdateCounter()
        {
            if (discoveryCounterText == null || _bestiaryManager == null) return;

            int discovered = _bestiaryManager.GetDiscoveredCount();
            int total = _bestiaryManager.GetTotalEnemyCount();
            discoveryCounterText.text = $"{discovered}/{total}";
        }

        private void OnEnemyDiscovered(string enemyId)
        {
            RefreshGrid();
            UpdateCounter();
        }

        private void ShowDetail(BestiaryEntryData entryData, bool isDiscovered)
        {
            if (detailPanel == null) return;

            detailPanel.SetActive(true);

            if (isDiscovered)
            {
                if (detailIcon != null)
                {
                    detailIcon.sprite = entryData.icon;
                    detailIcon.color = Color.white;
                }

                if (detailNameText != null)
                {
                    detailNameText.text = entryData.displayName;
                }

                if (detailDescriptionText != null)
                {
                    detailDescriptionText.text = entryData.description;
                }

                if (detailBehaviorText != null)
                {
                    detailBehaviorText.text = entryData.behaviorTip;
                }
            }
            else
            {
                // Show locked state
                if (detailIcon != null)
                {
                    detailIcon.sprite = lockedIcon;
                    detailIcon.color = lockedColor;
                }

                if (detailNameText != null)
                {
                    detailNameText.text = "???";
                }

                if (detailDescriptionText != null)
                {
                    detailDescriptionText.text = "You haven't encountered this enemy yet.";
                }

                if (detailBehaviorText != null)
                {
                    detailBehaviorText.text = "";
                }
            }
        }

        private void HideDetail()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (closeDetailButton != null)
            {
                closeDetailButton.onClick.RemoveListener(HideDetail);
            }

            foreach (var entryUI in _entryUIs)
            {
                if (entryUI != null)
                {
                    entryUI.OnEntryClicked -= ShowDetail;
                }
            }
        }
    }
}
