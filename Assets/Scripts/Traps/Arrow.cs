using UnityEngine;
using Labyrinth.Player;

namespace Labyrinth.Traps
{
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 0.5f;
        [SerializeField] private float lifetime = 5f;

        private Vector2 _direction;
        private float _spawnTime;

        public void Initialize(Vector2 direction)
        {
            _direction = direction.normalized;
            _spawnTime = Time.time;

            // Rotate arrow to face direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            // Move the arrow using transform (works reliably regardless of rigidbody type)
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            // Destroy after lifetime to prevent orphaned arrows
            if (Time.time - _spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);

                    // Apply knockback in arrow direction
                    other.transform.position += (Vector3)(_direction * knockbackForce);
                }
                Destroy(gameObject);
            }
            else if (!other.isTrigger && other.gameObject.layer == 8) // Layer 8 = Wall
            {
                // Hit a wall
                Destroy(gameObject);
            }
        }
    }
}
