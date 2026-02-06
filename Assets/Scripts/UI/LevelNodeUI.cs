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
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Outline _selectionOutline;

        [Header("Configuration")]
        [SerializeField] private LevelDefinition _level;

        private Action<LevelDefinition> _onClicked;

        public LevelDefinition Level => _level;

        public void Initialize(Action<LevelDefinition> onClicked)
        {
            _onClicked = onClicked;

            if (_level != null)
            {
                _nameText.text = _level.DisplayName;
            }

            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void UpdateState(bool isUnlocked, bool isCompleted)
        {
            _button.interactable = isUnlocked;
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
