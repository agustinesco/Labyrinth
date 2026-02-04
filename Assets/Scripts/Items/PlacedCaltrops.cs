using UnityEngine;
using Labyrinth.Visibility;
using Labyrinth.Enemy;
using Labyrinth.Player;

namespace Labyrinth.Items
{
    /// <summary>
    /// Caltrops placed on the ground by the player.
    /// Slows enemies that walk over them and damages the player if they step on their own caltrops.
    /// </summary>
    public class PlacedCaltrops : MonoBehaviour
    {
        private static readonly Color CaltropsColor = new Color(0.3f, 0.3f, 0.3f); // Dark grey/metallic

        private float _speedMultiplier = 0.4f;
        private float _slowDuration = 4f;
        private int _playerDamage = 1;
        private CircleCollider2D _collider;

        public static void SpawnAt(Vector2 position, float speedMultiplier, float slowDuration, int playerDamage)
        {
            GameObject caltropsObj = new GameObject("PlacedCaltrops");
            caltropsObj.transform.position = new Vector3(position.x, position.y, 0);

            // Add the PlacedCaltrops component
            var caltrops = caltropsObj.AddComponent<PlacedCaltrops>();
            caltrops._speedMultiplier = Mathf.Clamp01(speedMultiplier);
            caltrops._slowDuration = slowDuration;
            caltrops._playerDamage = Mathf.Max(0, playerDamage);

            // Add visual (spiky pattern)
            SpriteRenderer sr = caltropsObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCaltropsSprite();
            sr.color = CaltropsColor;
            sr.sortingOrder = 3; // Above floor, below items
            caltropsObj.transform.localScale = Vector3.one * 0.6f;

            // Add trigger collider for detection
            var collider = caltropsObj.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            caltrops._collider = collider;

            // Add visibility awareness so it shows/hides with fog
            caltropsObj.AddComponent<VisibilityAwareEntity>();

            Debug.Log($"PlacedCaltrops: Spawned at {position}");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check for enemies
            var enemyController = other.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                ApplySlowToEnemy(other.gameObject);
                return;
            }

            var patrollingGuard = other.GetComponent<PatrollingGuardController>();
            if (patrollingGuard != null)
            {
                ApplySlowToEnemy(other.gameObject);
                return;
            }

            // Check for player
            if (other.CompareTag("Player"))
            {
                DamagePlayer(other.gameObject);
            }
        }

        private void ApplySlowToEnemy(GameObject enemy)
        {
            // Add or get the slow effect component
            var slowEffect = enemy.GetComponent<CaltropsSlowEffect>();
            if (slowEffect == null)
            {
                slowEffect = enemy.AddComponent<CaltropsSlowEffect>();
            }
            slowEffect.ApplySlow(_speedMultiplier, _slowDuration);

            Debug.Log($"PlacedCaltrops: Slowed enemy {enemy.name}");
        }

        private void DamagePlayer(GameObject player)
        {
            if (_playerDamage <= 0) return;

            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsInvincible)
            {
                playerHealth.TakeDamage(_playerDamage);
                Debug.Log($"PlacedCaltrops: Player stepped on caltrops, took {_playerDamage} damage");

                // Destroy after damaging player (one-time trap for player)
                Destroy(gameObject);
            }
        }

        private static Sprite CreateCaltropsSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            // Clear background
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            // Draw spiky caltrops pattern (4 triangular spikes)
            DrawSpike(texture, center, 0, size);    // Up
            DrawSpike(texture, center, 90, size);   // Right
            DrawSpike(texture, center, 180, size);  // Down
            DrawSpike(texture, center, 270, size);  // Left

            // Draw central circle
            float innerRadius = size * 0.15f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < innerRadius)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void DrawSpike(Texture2D texture, Vector2 center, float angleDegrees, int size)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

            float spikeLength = size * 0.4f;
            float spikeWidth = size * 0.15f;

            // Draw triangular spike
            for (int i = 0; i < (int)spikeLength; i++)
            {
                float progress = i / spikeLength;
                float currentWidth = spikeWidth * (1f - progress);

                Vector2 spikePoint = center + direction * i;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                for (int w = (int)(-currentWidth); w <= (int)currentWidth; w++)
                {
                    Vector2 pixelPos = spikePoint + perpendicular * w;
                    int px = Mathf.RoundToInt(pixelPos.x);
                    int py = Mathf.RoundToInt(pixelPos.y);

                    if (px >= 0 && px < size && py >= 0 && py < size)
                    {
                        texture.SetPixel(px, py, Color.white);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Component that applies a slow effect to an enemy.
    /// </summary>
    public class CaltropsSlowEffect : MonoBehaviour
    {
        private float _originalSpeed;
        private float _slowedSpeed;
        private float _remainingDuration;
        private bool _isSlowed;

        // Cache references
        private EnemyController _enemyController;
        private PatrollingGuardController _guardController;

        private void Awake()
        {
            _enemyController = GetComponent<EnemyController>();
            _guardController = GetComponent<PatrollingGuardController>();
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            _remainingDuration = duration;

            if (!_isSlowed)
            {
                _isSlowed = true;
                // Store and modify speed via transform scale temporarily
                // (actual speed modification would need to be handled in enemy controllers)
                // For now, we'll use a visual indicator and time-based slow
            }
        }

        private void Update()
        {
            if (!_isSlowed) return;

            _remainingDuration -= Time.deltaTime;

            // Apply slow by reducing the enemy's Time.timeScale equivalent via deltaTime manipulation
            // This is a simplified approach - full implementation would modify enemy speed directly

            if (_remainingDuration <= 0)
            {
                RemoveSlow();
            }
        }

        private void RemoveSlow()
        {
            _isSlowed = false;
            Destroy(this);
        }

        /// <summary>
        /// Returns whether this enemy is currently slowed.
        /// </summary>
        public bool IsSlowed => _isSlowed;

        /// <summary>
        /// Returns the speed multiplier to apply when slowed.
        /// </summary>
        public float GetSpeedMultiplier()
        {
            return _isSlowed ? 0.4f : 1f; // Default slow multiplier
        }
    }
}
