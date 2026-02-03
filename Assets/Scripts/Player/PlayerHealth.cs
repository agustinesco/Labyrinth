using UnityEngine;
using Labyrinth.Core;

namespace Labyrinth.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1.5f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int _currentHealth;
        private float _invincibilityTimer;
        private bool _isInvincible;

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
            if (_isInvincible || IsDead) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
                GameManager.Instance?.TriggerLose();
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

        private void StartInvincibility()
        {
            _isInvincible = true;
            _invincibilityTimer = invincibilityDuration;
        }
    }
}
