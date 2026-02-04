using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    public class LevelUpCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image iconImage;

        [Header("Text References (TMP preferred)")]
        [SerializeField] private TMP_Text titleTextTMP;
        [SerializeField] private TMP_Text descriptionTextTMP;

        [Header("Text References (Legacy fallback)")]
        [SerializeField] private Text titleTextLegacy;
        [SerializeField] private Text descriptionTextLegacy;

        [Header("Button")]
        [SerializeField] private Button cardButton;

        private LevelUpUpgrade _upgrade;
        private Action<LevelUpUpgrade> _onCardSelected;

        private void Awake()
        {
            // Auto-find button if not assigned
            if (cardButton == null)
            {
                cardButton = GetComponent<Button>();
            }

            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnClick);
            }
        }

        /// <summary>
        /// Populates the card with upgrade data.
        /// </summary>
        public void Setup(LevelUpUpgrade upgrade, Action<LevelUpUpgrade> onSelected)
        {
            _upgrade = upgrade;
            _onCardSelected = onSelected;

            // Set title
            if (titleTextTMP != null)
            {
                titleTextTMP.text = upgrade.DisplayName;
            }
            else if (titleTextLegacy != null)
            {
                titleTextLegacy.text = upgrade.DisplayName;
            }

            // Set description
            if (descriptionTextTMP != null)
            {
                descriptionTextTMP.text = upgrade.Description;
            }
            else if (descriptionTextLegacy != null)
            {
                descriptionTextLegacy.text = upgrade.Description;
            }

            // Set icon
            if (iconImage != null)
            {
                if (upgrade.Icon != null)
                {
                    iconImage.sprite = upgrade.Icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    // Use tint color if no icon
                    iconImage.color = upgrade.CardTint;
                }
            }

            // Optionally tint the card background
            if (cardBackground != null && upgrade.CardTint != Color.white)
            {
                // Keep original sprite but could add subtle tint
                // cardBackground.color = upgrade.CardTint;
            }
        }

        private void OnClick()
        {
            _onCardSelected?.Invoke(_upgrade);
        }
    }
}
