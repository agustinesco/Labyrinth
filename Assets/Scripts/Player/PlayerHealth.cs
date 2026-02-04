using UnityEngine;
using System.Collections;
using Labyrinth.Core;

namespace Labyrinth.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1.5f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Death Animation")]
        [SerializeField] private float deathAnimationDuration = 1.5f;
        [SerializeField] private float deathSpinSpeed = 720f;
        [SerializeField] private float deathShrinkSpeed = 2f;
        [SerializeField] private Color deathColor = Color.red;

        private int _currentHealth;
        private float _invincibilityTimer;
        private bool _isInvincible;
        private bool _isPlayingDeathAnimation;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => _currentHealth <= 0;
        public bool IsInvincible => _isInvincible;

        public event System.Action<int> OnHealthChanged;
        public event System.Action OnDeath;

        private void Start()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth);
        }

        private void Update()
        {
            if (_isInvincible)
            {
                _invincibilityTimer -= Time.deltaTime;

                // Blink effect
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = Mathf.FloorToInt(_invincibilityTimer * 10) % 2 == 0;
                }

                if (_invincibilityTimer <= 0)
                {
                    _isInvincible = false;
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.enabled = true;
                    }
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isInvincible || IsDead || _isPlayingDeathAnimation) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth);

            if (IsDead)
            {
                StartCoroutine(PlayDeathAnimation());
            }
            else
            {
                StartInvincibility();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth);
        }

        /// <summary>
        /// Increases max health by the specified amount and heals the same amount.
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            if (IsDead) return;

            maxHealth += amount;
            _currentHealth += amount; // Also heal to compensate for the new max
            OnHealthChanged?.Invoke(_currentHealth);
        }

        private void StartInvincibility()
        {
            _isInvincible = true;
            _invincibilityTimer = invincibilityDuration;
        }

        private IEnumerator PlayDeathAnimation()
        {
            _isPlayingDeathAnimation = true;
            OnDeath?.Invoke();

            // Disable player controls
            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Ensure sprite is visible
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            Vector3 originalScale = transform.localScale;
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;

            while (elapsed < deathAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / deathAnimationDuration;

                // Spin the player
                transform.Rotate(0, 0, deathSpinSpeed * Time.deltaTime);

                // Shrink the player
                float scale = Mathf.Lerp(1f, 0f, progress);
                transform.localScale = originalScale * scale;

                // Fade to death color
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, deathColor, progress);
                }

                yield return null;
            }

            // Hide the player
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            // Trigger game over
            GameManager.Instance?.TriggerLose();
        }
    }
}
