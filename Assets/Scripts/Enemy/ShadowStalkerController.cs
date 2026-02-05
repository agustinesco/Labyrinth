using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Player;
using Labyrinth.Core;
using Labyrinth.Visibility;
using Labyrinth.Enemy.Awareness;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Shadow Stalker enemy that only moves when NOT in the player's vision.
    /// Freezes completely when the player can see it, creating tension as
    /// the player must keep checking behind them.
    /// </summary>
    public class ShadowStalkerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float pathRecalculateInterval = 0.5f;

        [Header("Vision Detection")]
        [SerializeField] private float visibilityThreshold = 0.1f;
        [SerializeField] private float freezeGracePeriod = 0.1f; // Brief delay before freezing

        [Header("Audio Feedback")]
        [SerializeField] private float creepingSoundInterval = 2f;
        [SerializeField] private float creepingSoundRange = 8f;

        [Header("Combat Settings")]
        [SerializeField] private int damage = 2;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.1f, 0.3f, 1f);
        [SerializeField] private Color frozenColor = new Color(0.4f, 0.2f, 0.5f, 1f);

        [Header("Awareness")]
        [SerializeField] private EnemyAwarenessConfig awarenessConfig;

        private Transform _player;
        private EnemyAwarenessController _awarenessController;
        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private SpriteRenderer _spriteRenderer;

        // Pathfinding
        private List<Vector2Int> _currentPath;
        private int _pathIndex;
        private float _pathTimer;

        // Path caching
        private Vector2Int _lastTargetPos;
        private Vector2Int _lastOwnPos;
        private const int TargetMoveThreshold = 2;

        // State
        private bool _isFrozen;
        private float _freezeTimer;
        private float _attackTimer;
        private float _soundTimer;

        // Track visibility state for smooth transitions
        private bool _wasVisibleLastFrame;

        public bool IsFrozen => _isFrozen;

        public void Initialize(MazeGrid grid, Transform player)
        {
            _grid = grid;
            _player = player;
            _pathfinding = new Pathfinding(grid);
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Setup awareness controller (manual updates due to NoClip/Invisibility handling)
            _awarenessController = GetComponent<EnemyAwarenessController>();
            if (_awarenessController == null)
            {
                _awarenessController = gameObject.AddComponent<EnemyAwarenessController>();
            }
            if (awarenessConfig != null)
            {
                _awarenessController.SetConfig(awarenessConfig);
            }
            // Disable auto-update since we need special handling for NoClip/Invisibility
            _awarenessController.SetAutoUpdate(false);
            _awarenessController.OnPlayerDetected += OnAwarenessDetection;
            _awarenessController.OnAwarenessLost += OnAwarenessLost;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = normalColor;
            }
        }

        private void OnAwarenessDetection()
        {
            // Shadow stalker has locked onto the player
            Debug.Log("[ShadowStalker] Player detected - locked on!");
        }

        private void OnAwarenessLost()
        {
            // Shadow stalker lost track of the player
            Debug.Log("[ShadowStalker] Lost awareness of player");
        }

        private void Update()
        {
            if (_player == null || _grid == null)
                return;

            // Only act when game is playing
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                return;

            // Don't chase player in no-clip or invisibility mode
            if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                return;

            if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
            {
                // When player is invisible, stalker can move freely
                _isFrozen = false;
                UpdateMovement();
                return;
            }

            _attackTimer -= Time.deltaTime;

            // Check if player can see us (for freeze mechanic)
            bool isCurrentlyVisible = IsVisibleToPlayer();

            // Update awareness - stalker detects player when within range
            UpdateAwarenessState();

            // Handle freeze state transitions
            UpdateFreezeState(isCurrentlyVisible);

            // Update visual feedback
            UpdateVisuals();

            // Only move when not frozen AND has detected player (awareness threshold reached)
            if (!_isFrozen && HasDetectedPlayer())
            {
                UpdateMovement();
                UpdateCreepingSound();
            }

            _wasVisibleLastFrame = isCurrentlyVisible;
        }

        private bool IsVisibleToPlayer()
        {
            // Use the FogOfWarManager to check if our position is visible
            if (FogOfWarManager.Instance == null)
                return false;

            return FogOfWarManager.Instance.IsPositionVisible(transform.position, visibilityThreshold);
        }

        private void UpdateAwarenessState()
        {
            if (_awarenessController == null || _player == null)
                return;

            // Use the awareness controller's built-in vision detection
            bool canDetectPlayer = _awarenessController.CanSeePlayer();
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);

            // Update awareness (stalker continues building awareness while tracking)
            _awarenessController.UpdateAwareness(canDetectPlayer, distanceToPlayer);
        }

        private bool HasDetectedPlayer()
        {
            // If no awareness controller, fall back to old behavior (always follow)
            if (_awarenessController == null)
                return true;

            return _awarenessController.HasDetectedPlayer;
        }

        private void UpdateFreezeState(bool isCurrentlyVisible)
        {
            if (isCurrentlyVisible)
            {
                // Player can see us - freeze!
                if (!_wasVisibleLastFrame)
                {
                    // Just became visible - start grace period
                    _freezeTimer = freezeGracePeriod;
                }
                else
                {
                    _freezeTimer -= Time.deltaTime;
                    if (_freezeTimer <= 0)
                    {
                        _isFrozen = true;
                    }
                }
            }
            else
            {
                // Player can't see us - we can move
                _isFrozen = false;
                _freezeTimer = 0;
            }
        }

        private void UpdateVisuals()
        {
            if (_spriteRenderer == null) return;

            // Lerp color based on frozen state
            Color targetColor = _isFrozen ? frozenColor : normalColor;
            _spriteRenderer.color = Color.Lerp(_spriteRenderer.color, targetColor, Time.deltaTime * 10f);
        }

        private void UpdateMovement()
        {
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
                Mathf.RoundToInt(_player.position.x),
                Mathf.RoundToInt(_player.position.y)
            );

            // Only recalculate if needed
            bool targetMoved = Mathf.Abs(targetPos.x - _lastTargetPos.x) + Mathf.Abs(targetPos.y - _lastTargetPos.y) >= TargetMoveThreshold;
            bool selfMoved = Mathf.Abs(currentPos.x - _lastOwnPos.x) + Mathf.Abs(currentPos.y - _lastOwnPos.y) >= TargetMoveThreshold;
            bool pathExhausted = _currentPath == null || _pathIndex >= _currentPath.Count;

            if (targetMoved || selfMoved || pathExhausted)
            {
                _currentPath = _pathfinding.FindPath(currentPos, targetPos);
                _pathIndex = (_currentPath != null && _currentPath.Count > 1) ? 1 : 0;
                _lastTargetPos = targetPos;
                _lastOwnPos = currentPos;
            }
        }

        private void MoveAlongPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
                return;

            Vector3 targetPosition = new Vector3(_currentPath[_pathIndex].x, _currentPath[_pathIndex].y, 0);

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                _pathIndex++;
            }
        }

        private void UpdateCreepingSound()
        {
            if (_player == null) return;

            _soundTimer -= Time.deltaTime;
            if (_soundTimer <= 0)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
                if (distanceToPlayer <= creepingSoundRange)
                {
                    // TODO: Play creeping sound effect
                    // The closer the stalker, the louder/more frequent the sound
                    Debug.Log($"[ShadowStalker] *creeping sounds* (distance: {distanceToPlayer:F1})");
                }
                _soundTimer = creepingSoundInterval;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerCollision(other);
        }

        private void HandlePlayerCollision(Collider2D other)
        {
            // Don't deal damage in no-clip or invisibility mode
            if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                return;

            if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
                return;

            // Can only attack when not frozen
            if (_isFrozen)
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

                    Debug.Log("[ShadowStalker] Attacked player!");
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

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw frozen state indicator
            Gizmos.color = _isFrozen ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw path
            if (_currentPath != null && _currentPath.Count > 0)
            {
                Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.5f);
                for (int i = _pathIndex; i < _currentPath.Count - 1; i++)
                {
                    Vector3 from = new Vector3(_currentPath[i].x, _currentPath[i].y, 0);
                    Vector3 to = new Vector3(_currentPath[i + 1].x, _currentPath[i + 1].y, 0);
                    Gizmos.DrawLine(from, to);
                }
            }

            // Draw sound range
            if (_player != null)
            {
                Gizmos.color = new Color(1f, 0f, 1f, 0.1f);
                Gizmos.DrawWireSphere(transform.position, creepingSoundRange);
            }

            // Draw detection range (from config)
            if (_awarenessController != null && _awarenessController.Config != null)
            {
                Gizmos.color = HasDetectedPlayer() ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _awarenessController.Config.VisionRange);
            }
        }

        private void OnDestroy()
        {
            if (_awarenessController != null)
            {
                _awarenessController.OnPlayerDetected -= OnAwarenessDetection;
                _awarenessController.OnAwarenessLost -= OnAwarenessLost;
            }
        }
    }
}
