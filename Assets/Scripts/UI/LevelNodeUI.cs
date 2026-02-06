using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Progression;

namespace Labyrinth.UI
{
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _completedIcon;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private Outline _selectionOutline;

        [Header("Configuration")]
        [SerializeField] private LevelDefinition _level;

        [Header("Colors")]
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color _availableColor = Color.white;
        [SerializeField] private Color _completedColor = new Color(0.7f, 1f, 0.7f, 1f);

        private Action<LevelDefinition> _onClicked;

        public LevelDefinition Level => _level;

        public void Initialize(Action<LevelDefinition> onClicked)
        {
            _onClicked = onClicked;

            if (_level != null)
            {
                _nameText.text = _level.DisplayName;

                if (_iconImage != null && _level.Icon != null)
                {
                    _iconImage.sprite = _level.Icon;
                    _iconImage.enabled = true;
                }
                else if (_iconImage != null)
                {
                    _iconImage.enabled = false;
                }
            }

            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void UpdateState(bool isUnlocked, bool isCompleted)
        {
            _lockIcon.SetActive(!isUnlocked);
            _completedIcon.SetActive(isCompleted);
            _button.interactable = isUnlocked;

            if (!isUnlocked)
            {
                _backgroundImage.color = _lockedColor;
            }
            else if (isCompleted)
            {
                _backgroundImage.color = _completedColor;
            }
            else
            {
                _backgroundImage.color = _availableColor;
            }

            UpdateProgressText();
        }

        private void UpdateProgressText()
        {
            if (_progressText == null || _level == null) return;

            var manager = LevelProgressionManager.Instance;
            if (manager == null) return;

            int completedObjectives = 0;
            int totalObjectives = _level.Objectives.Count;

            for (int i = 0; i < totalObjectives; i++)
            {
                var objective = _level.Objectives[i];
                int progress = manager.GetSavedObjectiveProgress(_level.LevelId, i);

                if (objective.TargetCount > 0 && progress >= objective.TargetCount)
                {
                    completedObjectives++;
                }
                else if (objective.TargetCount == 0 && manager.IsLevelCompleted(_level.LevelId))
                {
                    completedObjectives++;
                }
            }

            if (totalObjectives > 0)
            {
                _progressText.text = $"{completedObjectives}/{totalObjectives}";
            }
            else
            {
                _progressText.text = "";
            }
        }

        private void OnClick()
        {
            _onClicked?.Invoke(_level);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionOutline != null)
            {
                _selectionOutline.enabled = selected;
            }
        }
    }
}
