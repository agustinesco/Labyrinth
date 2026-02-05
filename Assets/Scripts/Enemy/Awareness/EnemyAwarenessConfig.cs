using UnityEngine;

namespace Labyrinth.Enemy.Awareness
{
    /// <summary>
    /// Configuration for enemy awareness behavior.
    /// Determines how quickly an enemy detects the player and how awareness decays.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyAwarenessConfig", menuName = "Labyrinth/Enemy/Awareness Config")]
    public class EnemyAwarenessConfig : ScriptableObject
    {
        [Header("Detection Settings")]
        [SerializeField, Tooltip("If true, enemy detects player instantly when in vision range (ignores awareness meter)")]
        private bool instantDetection = false;

        [SerializeField, Tooltip("Awareness level required to trigger detection (0-100)")]
        private float detectionThreshold = 100f;

        [Header("Awareness Rates")]
        [SerializeField, Tooltip("How fast awareness fills when player is visible (units per second)")]
        private float awarenessGainRate = 25f;

        [SerializeField, Tooltip("How fast awareness decays when player is not visible (units per second)")]
        private float awarenessDecayRate = 15f;

        [Header("Behavior")]
        [SerializeField, Tooltip("If true, enemy stops moving while awareness meter is filling")]
        private bool stopWhileGaining = false;

        [SerializeField, Tooltip("Multiplier for awareness gain based on distance (closer = faster). 1.0 = no distance scaling")]
        private float distanceScalingFactor = 0f;

        [SerializeField, Tooltip("Maximum distance for distance scaling calculation")]
        private float maxScalingDistance = 10f;

        [Header("Vision Settings")]
        [SerializeField, Tooltip("How far the enemy can see")]
        private float visionRange = 6f;

        [SerializeField, Tooltip("Field of view in degrees (360 = omnidirectional, 60 = narrow cone)")]
        private float visionAngle = 360f;

        [SerializeField, Tooltip("If true, walls block vision (raycast check)")]
        private bool requiresLineOfSight = true;

        [SerializeField, Tooltip("Layers that block vision")]
        private LayerMask wallLayer = 1 << 8; // Default to layer 8

        // Public accessors
        public bool InstantDetection => instantDetection;
        public float DetectionThreshold => detectionThreshold;
        public float AwarenessGainRate => awarenessGainRate;
        public float AwarenessDecayRate => awarenessDecayRate;
        public bool StopWhileGaining => stopWhileGaining;
        public float DistanceScalingFactor => distanceScalingFactor;
        public float MaxScalingDistance => maxScalingDistance;
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public LayerMask WallLayer => wallLayer;

        /// <summary>
        /// Calculates the effective gain rate based on distance to player.
        /// Closer distances result in faster awareness gain if distanceScalingFactor > 0.
        /// </summary>
        public float GetEffectiveGainRate(float distanceToPlayer)
        {
            if (distanceScalingFactor <= 0f)
                return awarenessGainRate;

            // Closer distance = higher multiplier
            float normalizedDistance = Mathf.Clamp01(distanceToPlayer / maxScalingDistance);
            float distanceMultiplier = 1f + (1f - normalizedDistance) * distanceScalingFactor;

            return awarenessGainRate * distanceMultiplier;
        }
    }
}
