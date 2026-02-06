using System;
using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Progression;

namespace Labyrinth.UI
{
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _titleImage;
        [SerializeField] private Outline _selectionOutline;

        [Header("Configuration")]
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private Sprite _lockSprite;

        private Action<LevelDefinition> _onClicked;
        private GameObject _lockOverlay;

        public LevelDefinition Level => _level;

        private void Awake()
        {
            CreateLockOverlay();
        }

        public void Initialize(Action<LevelDefinition> onClicked)
        {
            _onClicked = onClicked;
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void UpdateState(bool isUnlocked, bool isCompleted)
        {
            _button.interactable = isUnlocked;

            if (_lockOverlay != null)
            {
                _lockOverlay.SetActive(!isUnlocked);
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

        private void CreateLockOverlay()
        {
            _lockOverlay = new GameObject("LockOverlay");
            _lockOverlay.transform.SetParent(transform, false);

            var rect = _lockOverlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _lockOverlay.AddComponent<Image>();
            image.sprite = _lockSprite;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.85f);
            image.raycastTarget = false;

            _lockOverlay.SetActive(false);
        }
    }
}
