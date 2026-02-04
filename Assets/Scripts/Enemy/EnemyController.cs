using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Player;
using Labyrinth.Core;

namespace Labyrinth.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 4f;
        [SerializeField] private float speedIncreaseAfter = 30f;
        [SerializeField] private float increasedSpeed = 4.5f;
        [SerializeField] private float pathRecalculateInterval = 0.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 1;

        private Transform _target;
        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _pathTimer;
        private float _chaseTimer;
        private float _attackTimer;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(MazeGrid grid, Transform target)
        {
            _grid = grid;
            _target = target;
            _pathfinding = new Pathfinding(grid);
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

            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0)
            {
                RecalculatePath();
                _pathTimer = pathRecalculateInterval;
            }

            MoveAlongPath();
        }

        private void RecalculatePath()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(_target.position.x),
                Mathf.RoundToInt(_target.position.y)
            );

            _currentPath = _pathfinding.FindPath(currentPos, targetPos);
            _pathIndex = 0;
        }

        private void MoveAlongPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
                return;

            Vector3 targetPosition = new Vector3(_currentPath[_pathIndex].x, _currentPath[_pathIndex].y, 0);
            float speed = _chaseTimer > speedIncreaseAfter ? increasedSpeed : baseSpeed;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                _pathIndex++;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Don't deal damage in no-clip mode or invisibility mode
            if (Player.NoClipManager.Instance != null && Player.NoClipManager.Instance.IsNoClipActive)
                return;

            if (Player.InvisibilityManager.Instance != null && Player.InvisibilityManager.Instance.IsInvisible)
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
