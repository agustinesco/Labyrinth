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
        [SerializeField] private GameObject _levelNodePrefab;
        [SerializeField] private GameObject _connectionLinePrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Layout")]
        [SerializeField] private float _horizontalSpacing = 300f;
        [SerializeField] private float _verticalSpacing = 200f;
        [SerializeField] private float _leftPadding = 150f;

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

            ClearTree();
            var levels = manager.AllLevels;

            // Calculate tree layout
            var layout = CalculateLayout(levels);

            // Create nodes
            foreach (var level in levels)
            {
                CreateNode(level, layout[level.LevelId]);
            }

            // Create connection lines
            foreach (var level in levels)
            {
                CreateConnections(level);
            }

            // Update node states
            RefreshNodeStates();
        }

        private Dictionary<string, Vector2> CalculateLayout(IReadOnlyList<LevelDefinition> levels)
        {
            var layout = new Dictionary<string, Vector2>();
            var depths = new Dictionary<string, int>();
            var siblingCounts = new Dictionary<int, int>();

            // Calculate depth for each level
            foreach (var level in levels)
            {
                int depth = CalculateDepth(level, depths);
                depths[level.LevelId] = depth;

                if (!siblingCounts.ContainsKey(depth))
                    siblingCounts[depth] = 0;
                siblingCounts[depth]++;
            }

            // Assign positions
            var depthCurrentIndex = new Dictionary<int, int>();
            foreach (var level in levels)
            {
                int depth = depths[level.LevelId];
                if (!depthCurrentIndex.ContainsKey(depth))
                    depthCurrentIndex[depth] = 0;

                int siblingIndex = depthCurrentIndex[depth]++;
                int totalSiblings = siblingCounts[depth];

                float x = _leftPadding + depth * _horizontalSpacing;
                float y = (siblingIndex - (totalSiblings - 1) / 2f) * _verticalSpacing;

                layout[level.LevelId] = new Vector2(x, y);
            }

            return layout;
        }

        private int CalculateDepth(LevelDefinition level, Dictionary<string, int> cache)
        {
            if (cache.TryGetValue(level.LevelId, out int cached))
                return cached;

            if (level.UnlockedByLevels == null || level.UnlockedByLevels.Count == 0)
                return 0;

            int maxParentDepth = 0;
            foreach (var parent in level.UnlockedByLevels)
            {
                maxParentDepth = Mathf.Max(maxParentDepth, CalculateDepth(parent, cache));
            }

            return maxParentDepth + 1;
        }

        private void CreateNode(LevelDefinition level, Vector2 position)
        {
            var nodeObj = Instantiate(_levelNodePrefab, _nodeContainer);
            var rectTransform = nodeObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;

            var nodeUI = nodeObj.GetComponent<LevelNodeUI>();
            nodeUI.Setup(level, OnNodeClicked);

            _nodes[level.LevelId] = nodeUI;
        }

        private void CreateConnections(LevelDefinition level)
        {
            if (level.UnlockedByLevels == null) return;

            foreach (var parent in level.UnlockedByLevels)
            {
                if (_nodes.TryGetValue(parent.LevelId, out var parentNode) &&
                    _nodes.TryGetValue(level.LevelId, out var childNode))
                {
                    CreateConnectionLine(parentNode.transform.position, childNode.transform.position);
                }
            }
        }

        private void CreateConnectionLine(Vector3 start, Vector3 end)
        {
            if (_connectionLinePrefab == null) return;

            var lineObj = Instantiate(_connectionLinePrefab, _nodeContainer);
            lineObj.transform.SetAsFirstSibling();

            var line = lineObj.GetComponent<UILineRenderer>();
            if (line != null)
            {
                line.SetPositions(start, end);
            }
        }

        private void ClearTree()
        {
            foreach (Transform child in _nodeContainer)
            {
                Destroy(child.gameObject);
            }
            _nodes.Clear();
        }

        private void RefreshNodeStates()
        {
            var manager = LevelProgressionManager.Instance;
            foreach (var kvp in _nodes)
            {
                var level = manager.AllLevels.FirstOrDefault(l => l.LevelId == kvp.Key);
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
            // Deselect previous node
            if (_selectedNode != null)
            {
                _selectedNode.SetSelected(false);
            }

            _selectedLevel = level;

            // Select new node
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

            // Clear existing objectives
            foreach (Transform child in _objectivesContainer)
            {
                Destroy(child.gameObject);
            }

            // Add objective entries
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

            // Enable start button only if level is unlocked
            _startButton.interactable = manager.IsLevelUnlocked(level);
        }

        private void CloseDetailPanel()
        {
            _detailPanel.SetActive(false);
            _selectedLevel = null;

            // Deselect node
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

        private void OnBackClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
