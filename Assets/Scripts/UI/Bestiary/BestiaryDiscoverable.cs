using UnityEngine;
using Labyrinth.Visibility;

namespace Labyrinth.UI.Bestiary
{
    /// <summary>
    /// Component that marks an enemy as discoverable for the bestiary.
    /// When the player sees this enemy, it gets added to their bestiary.
    /// </summary>
    public class BestiaryDiscoverable : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private float visibilityCheckInterval = 0.25f;
        [SerializeField] private float visibilityThreshold = 0.1f;

        private bool _hasBeenDiscovered;
        private float _checkTimer;

        /// <summary>
        /// Gets the enemy ID for this discoverable.
        /// </summary>
        public string EnemyId => enemyId;

        private void Update()
        {
            // Skip if already discovered this session
            if (_hasBeenDiscovered) return;

            // Also skip if already in bestiary
            if (BestiaryManager.Instance != null && BestiaryManager.Instance.IsEnemyDiscovered(enemyId))
            {
                _hasBeenDiscovered = true;
                return;
            }

            // Periodic visibility check
            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0)
            {
                _checkTimer = visibilityCheckInterval;
                CheckVisibility();
            }
        }

        private void CheckVisibility()
        {
            if (FogOfWarManager.Instance == null) return;

            bool isVisible = FogOfWarManager.Instance.IsPositionVisible(transform.position, visibilityThreshold);

            if (isVisible)
            {
                DiscoverEnemy();
            }
        }

        private void DiscoverEnemy()
        {
            _hasBeenDiscovered = true;

            if (BestiaryManager.Instance != null)
            {
                BestiaryManager.Instance.DiscoverEnemy(enemyId);
            }
        }

        /// <summary>
        /// Sets the enemy ID (useful when spawning dynamically).
        /// </summary>
        public void SetEnemyId(string id)
        {
            enemyId = id;
        }
    }
}
