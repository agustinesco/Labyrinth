using UnityEngine;
using System;
using Labyrinth.Player;

namespace Labyrinth.Enemy.Awareness
{
    /// <summary>
    /// Manages the awareness meter for an enemy.
    /// Tracks how aware the enemy is of the player and triggers detection events.
    /// </summary>
    public class EnemyAwarenessController : MonoBehaviour
    {
        [SerializeField, Tooltip("Awareness configuration for this enemy")]
        private EnemyAwarenessConfig config;

        [SerializeField, Tooltip("Reference to the player transform (auto-found if null)")]
        private Transform player;

        [SerializeField, Tooltip("Show visual awareness indicator above enemy")]
        private bool showIndicator = true;

        private AwarenessIndicator _indicator;

        // Current awareness state
        private float _currentAwareness;
        private bool _isPlayerVisible;
        private bool _hasDetectedPlayer;

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

        /// <summary>
        /// Call this each frame to update awareness based on visibility.
        /// </summary>
        /// <param name="canSeePlayer">Whether the enemy can currently see the player</param>
        /// <param name="distanceToPlayer">Distance to the player (for distance-based scaling)</param>
        public void UpdateAwareness(bool canSeePlayer, float distanceToPlayer = 0f)
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
