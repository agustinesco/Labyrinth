using UnityEngine;
using Labyrinth.Player;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Projectile thrown by the Blind Mole enemy.
    /// Travels in a direction and deals damage on contact with the player.
    /// </summary>
    public class BlindMoleProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float knockbackForce = 0.5f;

        private Vector2 _direction;
        private bool _initialized;
        private float _lifetimeTimer;

        public void Initialize(Vector2 direction, float projectileSpeed, int projectileDamage)
        {
            _direction = direction.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            _initialized = true;
            _lifetimeTimer = lifetime;

            // Rotate sprite to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (!_initialized) return;

            // Move in direction
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            // Lifetime check
            _lifetimeTimer -= Time.deltaTime;
            if (_lifetimeTimer <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // Check for no-clip or invisibility
                if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                    return;

                if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
                    return;

                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);

                    // Apply knockback
                    other.transform.position += (Vector3)(_direction * knockbackForce);
                }

                Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                // Hit a wall or solid object
                int wallLayer = LayerMask.NameToLayer("Wall");
                if (other.gameObject.layer == wallLayer || other.gameObject.layer == 8)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
