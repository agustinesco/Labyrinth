using UnityEngine;
using System.Collections.Generic;
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
        [SerializeField] private float pathRecalculateInterval = 0.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float detectionRange = 8f;

        private Transform _target;
        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _pathTimer;
        private float _chaseTimer;
        private float _attackTimer;
        private SpriteRenderer _spriteRenderer;

        // Path caching - only recalculate when target moves significantly
        private Vector2Int _lastTargetPos;
        private Vector2Int _lastOwnPos;
        private const int TargetMoveThreshold = 2; // Recalculate if target moved this many cells

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

            // Shadow Blend reduces detection range
            if (Player.ShadowBlendManager.Instance != null && Player.ShadowBlendManager.Instance.IsBlended)
            {
                float effectiveRange = detectionRange * Player.ShadowBlendManager.Instance.DetectionRangeMultiplier;
                float distanceToPlayer = Vector2.Distance(transform.position, _target.position);
                if (distanceToPlayer > effectiveRange)
                    return; // Can't detect blended player at this distance
            }

            _chaseTimer += Time.deltaTime;
            _attackTimer -= Time.deltaTime;

            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0)
            {
                TryRecalculatePath();
                _pathTimer = pathRecalculateInterval;
            }

            MoveAlongPath();
        }

        private void TryRecalculatePath()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(_target.position.x),
                Mathf.RoundToInt(_target.position.y)
            );

            // Only recalculate if target or self has moved significantly, or path is exhausted
            bool targetMoved = Mathf.Abs(targetPos.x - _lastTargetPos.x) + Mathf.Abs(targetPos.y - _lastTargetPos.y) >= TargetMoveThreshold;
            bool selfMoved = Mathf.Abs(currentPos.x - _lastOwnPos.x) + Mathf.Abs(currentPos.y - _lastOwnPos.y) >= TargetMoveThreshold;
            bool pathExhausted = _currentPath == null || _pathIndex >= _currentPath.Count;

            if (targetMoved || selfMoved || pathExhausted)
            {
                _currentPath = _pathfinding.FindPath(currentPos, targetPos);
                _pathIndex = 0;
                _lastTargetPos = targetPos;
                _lastOwnPos = currentPos;
            }
        }

        private void MoveAlongPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
                return;

            Vector3 targetPosition = new Vector3(_currentPath[_pathIndex].x, _currentPath[_pathIndex].y, 0);
            float speed = _chaseTimer > speedIncreaseAfter ? increasedSpeed : baseSpeed;

            // Apply caltrops slow effect if present
            var slowEffect = GetComponent<CaltropsSlowEffect>();
            if (slowEffect != null && slowEffect.IsSlowed)
            {
                speed *= slowEffect.GetSpeedMultiplier();
            }

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
