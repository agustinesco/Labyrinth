using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Labyrinth.Progression;

namespace Labyrinth.UI
{
    public class LevelSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Detail Panel")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TextMeshProUGUI _detailTitle;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private Transform _objectivesContainer;
        [SerializeField] private GameObject _objectiveEntryPrefab;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeDetailButton;

        private Dictionary<string, LevelNodeUI> _nodes = new();
        private LevelDefinition _selectedLevel;
        private LevelNodeUI _selectedNode;

        private void Start()
        {
            _backButton.onClick.AddListener(OnBackClicked);
            _startButton.onClick.AddListener(OnStartClicked);
            _closeDetailButton.onClick.AddListener(CloseDetailPanel);

            CloseDetailPanel();
            BuildTree();
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(OnBackClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
            _closeDetailButton.onClick.RemoveListener(CloseDetailPanel);
        }

        private void BuildTree()
        {
            var manager = LevelProgressionManager.Instance;
            if (manager == null)
            {
                Debug.LogError("LevelProgressionManager not found");
                return;
            }

            _nodes.Clear();

            // Find pre-placed nodes and initialize them
            foreach (var node in _nodeContainer.GetComponentsInChildren<LevelNodeUI>())
            {
                if (node.Level != null)
                {
                    node.Initialize(OnNodeClicked);
                    _nodes[node.Level.LevelId] = node;
                }
            }

            RefreshNodeStates();
        }

        private void RefreshNodeStates()
        {
            var manager = LevelProgressionManager.Instance;
            foreach (var kvp in _nodes)
            {
                var level = kvp.Value.Level;
                if (level != null)
                {
                    bool unlocked = manager.IsLevelUnlocked(level);
                    bool completed = manager.IsLevelCompleted(level.LevelId);
                    kvp.Value.UpdateState(unlocked, completed);
                }
            }
        }

        private void OnNodeClicked(LevelDefinition level)
        {
            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
            }

            _selectedLevel = level;

            if (_nodes.TryGetValue(level.LevelId, out var node))
            {
                _selectedNode = node;
                _selectedNode.SetSelected(true);
            }

            ShowDetailPanel(level);
        }

        private void ShowDetailPanel(LevelDefinition level)
        {
            _detailPanel.SetActive(true);
            _detailTitle.text = level.DisplayName;
            _detailDescription.text = level.Description;

            foreach (Transform child in _objectivesContainer)
            {
                Destroy(child.gameObject);
            }

            var manager = LevelProgressionManager.Instance;
            for (int i = 0; i < level.Objectives.Count; i++)
            {
                var objective = level.Objectives[i];
                var entryObj = Instantiate(_objectiveEntryPrefab, _objectivesContainer);
                var entryText = entryObj.GetComponentInChildren<TextMeshProUGUI>();

                int progress = manager.GetSavedObjectiveProgress(level.LevelId, i);
                string progressText = objective.TargetCount > 0
                    ? $" ({progress}/{objective.TargetCount})"
                    : "";

                entryText.text = $"• {objective.Description}{progressText}";
            }

            _startButton.interactable = manager.IsLevelUnlocked(level);
        }

        private void CloseDetailPanel()
        {
            _detailPanel.SetActive(false);
            _selectedLevel = null;

            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
                _selectedNode = null;
            }
        }

        private void OnStartClicked()
        {
            if (_selectedLevel != null)
            {
                LevelProgressionManager.Instance?.StartLevel(_selectedLevel);
            }
        }

        public void OnBackClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
