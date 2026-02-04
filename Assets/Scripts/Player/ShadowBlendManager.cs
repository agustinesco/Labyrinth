using UnityEngine;
using Labyrinth.Leveling;

namespace Labyrinth.Player
{
    /// <summary>
    /// Manages the Shadow Blend upgrade effect.
    /// When the player stands still for a duration, enemies have reduced detection range.
    /// </summary>
    public class ShadowBlendManager : MonoBehaviour
    {
        public static ShadowBlendManager Instance { get; private set; }

        [SerializeField] private float timeToBlend = 2f;
        [SerializeField] private float detectionReductionPerLevel = 0.25f; // 25% reduction per level

        private float _standingStillTimer;
        private Vector2 _lastPosition;
        private bool _isBlended;

        /// <summary>
        /// Whether the player is currently blended into shadows.
        /// </summary>
        public bool IsBlended => _isBlended && HasShadowBlend;

        /// <summary>
        /// Whether the player has the Shadow Blend upgrade.
        /// </summary>
        public bool HasShadowBlend => PlayerLevelSystem.Instance != null && PlayerLevelSystem.Instance.ShadowBlendLevel > 0;

        /// <summary>
        /// Returns the detection range multiplier for enemies.
        /// 1.0 = normal detection, lower values = harder to detect.
        /// </summary>
        public float DetectionRangeMultiplier
        {
            get
            {
                if (!IsBlended) return 1f;

                int level = PlayerLevelSystem.Instance?.ShadowBlendLevel ?? 0;
                // Each level reduces detection by 25%, capped at 75% reduction (level 3)
                float reduction = Mathf.Min(level * detectionReductionPerLevel, 0.75f);
                return 1f - reduction;
            }
        }

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

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (!HasShadowBlend)
            {
                _isBlended = false;
                return;
            }

            Vector2 currentPosition = transform.position;
            float movementThreshold = 0.05f;

            // Check if player has moved
            if (Vector2.Distance(currentPosition, _lastPosition) > movementThreshold)
            {
                // Player moved - reset timer
                _standingStillTimer = 0f;
                _isBlended = false;
            }
            else
            {
                // Player is still - increment timer
                _standingStillTimer += Time.deltaTime;

                if (_standingStillTimer >= timeToBlend)
                {
                    _isBlended = true;
                }
            }

            _lastPosition = currentPosition;
        }
    }
}
