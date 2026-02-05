using UnityEngine;
using System;
using Labyrinth.Player;

namespace Labyrinth.Enemy.Awareness
{
    /// <summary>
    /// Self-contained awareness system for enemies.
    /// Automatically handles vision detection and awareness updates each frame.
    /// Enemies only need to set their facing direction when moving.
    /// </summary>
    public class EnemyAwarenessController : MonoBehaviour
    {
        [SerializeField, Tooltip("Awareness configuration for this enemy")]
        private EnemyAwarenessConfig config;

        [SerializeField, Tooltip("Reference to the player transform (auto-found if null)")]
        private Transform player;

        [SerializeField, Tooltip("Show visual awareness indicator above enemy")]
        private bool showIndicator = true;

        [SerializeField, Tooltip("Enable automatic awareness updates (set false if enemy handles it manually)")]
        private bool autoUpdate = true;

        private AwarenessIndicator _indicator;

        // Current awareness state
        private float _currentAwareness;
        private bool _isPlayerVisible;
        private bool _hasDetectedPlayer;

        // Facing direction for vision cone (default: right)
        private Vector2 _facingDirection = Vector2.right;

        // Events
        public event Action OnPlayerDetected;
        public event Action OnAwarenessLost;
        public event Action<float> OnAwarenessChanged;

        // Public accessors
        public float CurrentAwareness => _currentAwareness;
        public float AwarenessPercent => config != null ? _currentAwareness / config.DetectionThreshold : 0f;
        public bool IsPlayerVisible => _isPlayerVisible;
        public bool HasDetectedPlayer => _hasDetectedPlayer;
        public bool ShouldStopMoving => config != null && config.StopWhileGaining && _isPlayerVisible && !_hasDetectedPlayer;
        public EnemyAwarenessConfig Config => config;
        public Transform Player => player;
        public Vector2 FacingDirection => _facingDirection;

        private void Start()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            // Create awareness indicator if enabled
            if (showIndicator && config != null && !config.InstantDetection)
            {
                CreateIndicator();
            }
        }

        private void CreateIndicator()
        {
            GameObject indicatorGO = new GameObject("AwarenessIndicator");
            indicatorGO.transform.SetParent(transform);
            indicatorGO.transform.localPosition = Vector3.zero;
            _indicator = indicatorGO.AddComponent<AwarenessIndicator>();
        }

        private void Update()
        {
            if (!autoUpdate || config == null || player == null) return;

            bool canSee = CanSeePlayer();
            float distance = Vector2.Distance(transform.position, player.position);
            ProcessAwareness(canSee, distance);
        }

        /// <summary>
        /// Sets the facing direction for vision cone calculations.
        /// Call this when the enemy changes movement direction.
        /// </summary>
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                _facingDirection = direction.normalized;
            }
        }

        /// <summary>
        /// Checks if the enemy can see the player based on config's vision settings.
        /// </summary>
        public bool CanSeePlayer()
        {
            if (config == null || player == null) return false;

            Vector2 myPos = transform.position;
            Vector2 playerPos = player.position;
            Vector2 toPlayer = playerPos - myPos;
            float distance = toPlayer.magnitude;

            // Check range
            if (distance > config.VisionRange) return false;

            // Check vision angle (skip if omnidirectional)
            if (config.VisionAngle < 360f)
            {
                float angle = Vector2.Angle(_facingDirection, toPlayer);
                if (angle > config.VisionAngle / 2f) return false;
            }

            // Check line of sight
            if (config.RequiresLineOfSight)
            {
                RaycastHit2D hit = Physics2D.Raycast(myPos, toPlayer.normalized, distance, config.WallLayer);
                if (hit.collider != null) return false;
            }

            return true;
        }

        /// <summary>
        /// Manual awareness update for enemies that need custom vision logic.
        /// Prefer using autoUpdate=true and SetFacingDirection() instead.
        /// </summary>
        public void UpdateAwareness(bool canSeePlayer, float distanceToPlayer = 0f)
        {
            ProcessAwareness(canSeePlayer, distanceToPlayer);
        }

        /// <summary>
        /// Internal method that processes awareness gain/decay.
        /// </summary>
        private void ProcessAwareness(bool canSeePlayer, float distanceToPlayer)
        {
            if (config == null) return;

            _isPlayerVisible = canSeePlayer;

            // Handle instant detection
            if (config.InstantDetection && canSeePlayer && !_hasDetectedPlayer)
            {
                _currentAwareness = config.DetectionThreshold;
                _hasDetectedPlayer = true;
                OnAwarenessChanged?.Invoke(_currentAwareness);
                OnPlayerDetected?.Invoke();
                return;
            }

            float previousAwareness = _currentAwareness;

            if (canSeePlayer)
            {
                // Gain awareness
                float gainRate = config.GetEffectiveGainRate(distanceToPlayer);

                // Apply player's sneakiness multiplier
                float sneakinessMultiplier = GetPlayerSneakinessMultiplier();
                gainRate *= sneakinessMultiplier;

                _currentAwareness += gainRate * Time.deltaTime;

                // Check for detection threshold
                if (_currentAwareness >= config.DetectionThreshold && !_hasDetectedPlayer)
                {
                    _currentAwareness = config.DetectionThreshold;
                    _hasDetectedPlayer = true;
                    OnPlayerDetected?.Invoke();
                }
            }
            else
            {
                // Decay awareness
                _currentAwareness -= config.AwarenessDecayRate * Time.deltaTime;

                if (_currentAwareness <= 0f)
                {
                    _currentAwareness = 0f;

                    // Lost awareness completely
                    if (_hasDetectedPlayer)
                    {
                        _hasDetectedPlayer = false;
                        OnAwarenessLost?.Invoke();
                    }
                }
            }

            // Clamp awareness
            _currentAwareness = Mathf.Clamp(_currentAwareness, 0f, config.DetectionThreshold);

            // Fire change event if awareness changed
            if (!Mathf.Approximately(previousAwareness, _currentAwareness))
            {
                OnAwarenessChanged?.Invoke(_currentAwareness);
            }
        }

        /// <summary>
        /// Forces immediate detection (useful for sound-based detection, etc.)
        /// </summary>
        public void ForceDetection()
        {
            if (config == null) return;

            _currentAwareness = config.DetectionThreshold;
            if (!_hasDetectedPlayer)
            {
                _hasDetectedPlayer = true;
                OnPlayerDetected?.Invoke();
            }
            OnAwarenessChanged?.Invoke(_currentAwareness);
        }

        /// <summary>
        /// Resets awareness to zero.
        /// </summary>
        public void ResetAwareness()
        {
            bool wasDetected = _hasDetectedPlayer;
            _currentAwareness = 0f;
            _hasDetectedPlayer = false;
            _isPlayerVisible = false;

            if (wasDetected)
            {
                OnAwarenessLost?.Invoke();
            }
            OnAwarenessChanged?.Invoke(_currentAwareness);
        }

        /// <summary>
        /// Sets the config at runtime (useful for spawned enemies).
        /// </summary>
        public void SetConfig(EnemyAwarenessConfig newConfig)
        {
            config = newConfig;
        }

        /// <summary>
        /// Enables or disables automatic awareness updates.
        /// Disable this if the enemy has custom visibility logic and calls UpdateAwareness manually.
        /// </summary>
        public void SetAutoUpdate(bool enabled)
        {
            autoUpdate = enabled;
        }

        private float GetPlayerSneakinessMultiplier()
        {
            if (player == null) return 1f;

            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                return playerController.SneakinessMultiplier;
            }

            return 1f;
        }
    }
}
