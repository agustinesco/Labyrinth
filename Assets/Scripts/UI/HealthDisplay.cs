using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Player;

namespace Labyrinth.UI
{
    /// <summary>
    /// Displays player health as heart icons.
    /// Shows full hearts for remaining health, empty for lost.
    /// Uses color fallback when sprites are not assigned.
    /// </summary>
    public class HealthDisplay : MonoBehaviour
    {
        [Header("Heart References")]
        [SerializeField] private Image[] heartImages;

        [Header("Heart Sprites")]
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;

        [Header("Fallback Colors (when no sprites assigned)")]
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.2f, 0.3f, 1f); // Bright red
        [SerializeField] private Color emptyHeartColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Dark gray, semi-transparent

        [Header("Player Reference")]
        [SerializeField] private PlayerHealth playerHealth;

        private void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateDisplay;
                UpdateDisplay(playerHealth.CurrentHealth);
            }
            else
            {
                // Initialize with max health visual
                UpdateDisplay(heartImages != null ? heartImages.Length : 3);
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
            }
        }

        /// <summary>
        /// Allows dynamic assignment of PlayerHealth (for runtime-spawned players).
        /// </summary>
        /// <param name="health">The PlayerHealth component to track.</param>
        public void SetPlayerHealth(PlayerHealth health)
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
            }

            playerHealth = health;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateDisplay;
                UpdateDisplay(playerHealth.CurrentHealth);
            }
        }

        /// <summary>
        /// Updates the heart display based on current health.
        /// </summary>
        /// <param name="currentHealth">Current health value.</param>
        private void UpdateDisplay(int currentHealth)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    bool isFull = i < currentHealth;

                    // Use sprites if available, otherwise use colors
                    if (fullHeartSprite != null && emptyHeartSprite != null)
                    {
                        heartImages[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
                        heartImages[i].color = Color.white; // Reset color when using sprites
                    }
                    else
                    {
                        // Color-based fallback
                        heartImages[i].color = isFull ? fullHeartColor : emptyHeartColor;
                    }
                }
            }
        }
    }
}
