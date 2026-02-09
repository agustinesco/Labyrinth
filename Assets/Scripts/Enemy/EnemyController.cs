using UnityEngine;
using Labyrinth.Maze;
using Labyrinth.Player;
using Labyrinth.Core;
using Labyrinth.Items;

namespace Labyrinth.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 4f;
        [SerializeField] private float speedIncreaseAfter = 30f;
        [SerializeField] private float increasedSpeed = 4.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float detectionRange = 8f;

        private Transform _target;
        private MazeGrid _grid;
        private float _chaseTimer;
        private float _attackTimer;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(MazeGrid grid, Transform target)
        {
            _grid = grid;
            _target = target;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_target == null || _grid == null || GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            // Don't chase player in no-clip mode or invisibility mode
            if (Player.NoClipManager.Instance != null && Player.NoClipManager.Instance.IsNoClipActive)
                return;

            if (Player.InvisibilityManager.Instance != null && Player.InvisibilityManager.Instance.IsInvisible)
                return;

            _chaseTimer += Time.deltaTime;
            _attackTimer -= Time.deltaTime;

            MoveTowardPlayer();
            RotateTowardPlayer();
        }

        private void MoveTowardPlayer()
        {
            float speed = _chaseTimer > speedIncreaseAfter ? increasedSpeed : baseSpeed;

            // Apply caltrops slow effect if present
            var slowEffect = GetComponent<CaltropsSlowEffect>();
            if (slowEffect != null && slowEffect.IsSlowed)
            {
                speed *= slowEffect.GetSpeedMultiplier();
            }

            transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);
        }

        private void RotateTowardPlayer()
        {
            Vector2 direction = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Don't deal damage in no-clip mode or invisibility mode
            if (Player.NoClipManager.Instance != null && Player.NoClipManager.Instance.IsNoClipActive)
                return;

            if (Player.InvisibilityManager.Instance != null && Player.InvisibilityManager.Instance.IsInvisible)
                return;

            // Don't deal damage while glider is active (player can pass through walls)
            if (GliderEffect.Instance != null && GliderEffect.Instance.IsActive)
                return;

            if (other.CompareTag("Player") && _attackTimer <= 0)
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);
                    _attackTimer = attackCooldown;

                    // Knockback player
                    Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                    other.transform.position += (Vector3)(knockbackDir * 0.5f);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = visible;
            }
        }
    }
}
