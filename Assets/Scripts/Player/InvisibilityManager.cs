using UnityEngine;
using System.Collections;

namespace Labyrinth.Player
{
    /// <summary>
    /// Manages player invisibility state.
    /// When active: enemies cannot detect or chase the player.
    /// </summary>
    public class InvisibilityManager : MonoBehaviour
    {
        public static InvisibilityManager Instance { get; private set; }

        [SerializeField] private Color invisibilityTint = new Color(1f, 1f, 1f, 0.5f);

        private bool _isInvisible;
        private float _invisibilityTimer;
        private SpriteRenderer _playerSprite;
        private Color _originalColor;
        private Coroutine _invisibilityCoroutine;

        public bool IsInvisible => _isInvisible;
        public float RemainingTime => _invisibilityTimer;

        public event System.Action<bool> OnInvisibilityChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Activates invisibility for the specified duration.
        /// </summary>
        public void ActivateInvisibility(float duration)
        {
            // If already invisible, extend the timer
            if (_isInvisible)
            {
                _invisibilityTimer += duration;
                Debug.Log($"[Invisibility] Extended - {_invisibilityTimer:F1}s remaining");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            _playerSprite = player.GetComponent<SpriteRenderer>();

            if (_playerSprite != null)
            {
                _originalColor = _playerSprite.color;
                _playerSprite.color = invisibilityTint;
            }

            _isInvisible = true;
            _invisibilityTimer = duration;
            OnInvisibilityChanged?.Invoke(true);

            if (_invisibilityCoroutine != null)
            {
                StopCoroutine(_invisibilityCoroutine);
            }
            _invisibilityCoroutine = StartCoroutine(InvisibilityTimerCoroutine());

            Debug.Log($"[Invisibility] Activated for {duration}s");
        }

        private IEnumerator InvisibilityTimerCoroutine()
        {
            while (_invisibilityTimer > 0)
            {
                _invisibilityTimer -= Time.deltaTime;
                yield return null;
            }

            DeactivateInvisibility();
        }

        private void DeactivateInvisibility()
        {
            if (!_isInvisible) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && _playerSprite != null)
            {
                _playerSprite.color = _originalColor;
            }

            _isInvisible = false;
            _invisibilityTimer = 0;
            OnInvisibilityChanged?.Invoke(false);

            Debug.Log("[Invisibility] Deactivated");
        }
    }
}
