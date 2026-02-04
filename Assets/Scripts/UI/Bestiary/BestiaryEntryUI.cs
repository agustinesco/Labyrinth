using UnityEngine;
using UnityEngine.UI;
using System;

namespace Labyrinth.UI.Bestiary
{
    /// <summary>
    /// UI component for a single bestiary entry in the grid.
    /// </summary>
    public class BestiaryEntryUI : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;

        private BestiaryEntryData _entryData;
        private bool _isDiscovered;
        private Sprite _lockedIcon;
        private Color _lockedColor;
        private Color _unlockedColor;

        /// <summary>
        /// Event fired when this entry is clicked.
        /// </summary>
        public event Action<BestiaryEntryData, bool> OnEntryClicked;

        /// <summary>
        /// Gets the entry data for this UI element.
        /// </summary>
        public BestiaryEntryData EntryData => _entryData;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (iconImage == null && transform.childCount > 0)
            {
                iconImage = transform.GetChild(0).GetComponent<Image>();
            }
        }

        private void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// Sets up the entry with data and discovery state.
        /// </summary>
        public void Setup(BestiaryEntryData entryData, bool isDiscovered, Sprite lockedIcon, Color lockedColor, Color unlockedColor)
        {
            _entryData = entryData;
            _lockedIcon = lockedIcon;
            _lockedColor = lockedColor;
            _unlockedColor = unlockedColor;

            SetDiscovered(isDiscovered);
        }

        /// <summary>
        /// Updates the discovered state of this entry.
        /// </summary>
        public void SetDiscovered(bool isDiscovered)
        {
            _isDiscovered = isDiscovered;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_isDiscovered)
            {
                // Show unlocked state
                if (iconImage != null && _entryData != null)
                {
                    iconImage.sprite = _entryData.icon;
                    iconImage.color = _unlockedColor;
                }

                if (backgroundImage != null)
                {
                    backgroundImage.color = _unlockedColor;
                }
            }
            else
            {
                // Show locked state
                if (iconImage != null)
                {
                    iconImage.sprite = _lockedIcon;
                    iconImage.color = _lockedColor;
                }

                if (backgroundImage != null)
                {
                    backgroundImage.color = _lockedColor;
                }
            }
        }

        private void OnClick()
        {
            OnEntryClicked?.Invoke(_entryData, _isDiscovered);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }
    }
}
