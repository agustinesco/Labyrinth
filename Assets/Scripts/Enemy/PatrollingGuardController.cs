using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Player;
using Labyrinth.Core;
using Labyrinth.Items;
using Labyrinth.Enemy.Awareness;

namespace Labyrinth.Enemy
{
    public enum GuardState
    {
        Patrolling,
        Paused,
        GainingAwareness, // Stopped, building awareness of player
        Chasing,
        Investigating,  // Going to last seen player position
        SearchingAround, // Looking around at last seen position
        Returning
    }

    public class PatrollingGuardController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float pauseDuration = 0.5f;

        [Header("Chase Settings")]
        [SerializeField] private float pathRecalculateInterval = 0.3f;
        [SerializeField] private float losePlayerTime = 3f;

        [Header("Search Settings")]
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private float lookAroundInterval = 0.75f;
        [SerializeField] private int lookDirections = 4;

        [Header("Combat Settings")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float attackCooldown = 2f;

        [Header("Awareness")]
        [SerializeField] private EnemyAwarenessConfig awarenessConfig;

        private Transform _player;
        private EnemyAwarenessController _awarenessController;
        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private SpriteRenderer _spriteRenderer;

        // Patrol waypoints (rectangular path hugging walls)
        private List<Vector2> _patrolWaypoints;
        private int _currentWaypointIndex;
        private Vector2 _facingDirection;

        // State machine
        private GuardState _currentState = GuardState.Patrolling;
        private float _pauseTimer;

        /// <summary>
        /// The direction the guard is currently facing.
        /// </summary>
        public Vector2 FacingDirection => _facingDirection;

        /// <summary>
        /// Whether the guard is currently moving.
        /// </summary>
        public bool IsMoving => _currentState == GuardState.Patrolling ||
                                _currentState == GuardState.Chasing ||
                                _currentState == GuardState.Investigating ||
                                _currentState == GuardState.Returning;
        private float _losePlayerTimer;
        private float _pathTimer;
        private float _attackTimer;

        // Pathfinding for chase/return
        private List<Vector2Int> _currentPath;
        private int _pathIndex;

        // Path caching - only recalculate when target moves significantly
        private Vector2Int _lastPathTargetPos;
        private Vector2Int _lastPathOwnPos;
        private const int TargetMoveThreshold = 2;

        // Investigation/Search state
        private Vector2 _lastSeenPlayerPosition;
        private float _searchTimer;
        private float _lookAroundTimer;
        private int _currentLookDirection;

        public void Initialize(MazeGrid grid, Transform player, List<Vector2> patrolWaypoints)
        {
            _grid = grid;
            _player = player;
            _patrolWaypoints = patrolWaypoints;
            _pathfinding = new Pathfinding(grid);
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Setup awareness controller (with manual updates due to custom visibility checks)
            _awarenessController = GetComponent<EnemyAwarenessController>();
            if (_awarenessController == null)
            {
                _awarenessController = gameObject.AddComponent<EnemyAwarenessController>();
            }
            if (awarenessConfig != null)
            {
                _awarenessController.SetConfig(awarenessConfig);
            }
            // Disable auto-update since we have custom visibility logic (NoClip, Invisibility, ShadowBlend)
            _awarenessController.SetAutoUpdate(false);
            _awarenessController.OnPlayerDetected += OnAwarenessDetection;

            // Start at first waypoint
            _currentWaypointIndex = 0;

            if (_patrolWaypoints != null && _patrolWaypoints.Count > 0)
            {
                transform.position = new Vector3(_patrolWaypoints[0].x, _patrolWaypoints[0].y, 0);
                UpdateFacingDirection();
            }
        }

        private void Update()
        {
            if (_grid == null || _patrolWaypoints == null || _patrolWaypoints.Count < 2)
                return;

            // Allow movement in test mode (no GameManager) or when game is playing
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                return;

            _attackTimer -= Time.deltaTime;

            switch (_currentState)
            {
                case GuardState.Patrolling:
                    UpdatePatrolling();
                    break;
                case GuardState.Paused:
                    UpdatePaused();
                    break;
                case GuardState.GainingAwareness:
                    UpdateGainingAwareness();
                    break;
                case GuardState.Chasing:
                    UpdateChasing();
                    break;
                case GuardState.Investigating:
                    UpdateInvestigating();
                    break;
                case GuardState.SearchingAround:
                    UpdateSearchingAround();
                    break;
                case GuardState.Returning:
                    UpdateReturning();
                    break;
            }
        }

        private void UpdatePatrolling()
        {
            // Update awareness system
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // If config says to stop while gaining awareness and we can see player
            if (canSee && _awarenessController != null && _awarenessController.ShouldStopMoving)
            {
                _currentState = GuardState.GainingAwareness;
                return;
            }

            // Get current target waypoint
            Vector2 targetWaypoint = _patrolWaypoints[_currentWaypointIndex];

            // Move toward current waypoint
            MoveToward(targetWaypoint, patrolSpeed);

            // Check if reached waypoint
            if (Vector2.Distance(transform.position, targetWaypoint) < 0.15f)
            {
                // Move to next waypoint (loop around for rectangular patrol)
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _patrolWaypoints.Count;

                // Brief pause at corners
                _pauseTimer = pauseDuration;
                _currentState = GuardState.Paused;

                UpdateFacingDirection();
            }
        }

        private void UpdatePaused()
        {
            // Update awareness system even while paused
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // If config says to stop while gaining awareness and we can see player
            if (canSee && _awarenessController != null && _awarenessController.ShouldStopMoving)
            {
                _currentState = GuardState.GainingAwareness;
                return;
            }

            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0)
            {
                UpdateFacingDirection();
                _currentState = GuardState.Patrolling;
            }
        }

        private void UpdateGainingAwareness()
        {
            // Continue updating awareness - guard is stopped, watching player
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // Face toward the player while gaining awareness
            if (_player != null)
            {
                Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    _facingDirection = toPlayer;
                    _awarenessController?.SetFacingDirection(toPlayer);
                }
            }

            // If we can no longer see the player and awareness has decayed, return to patrol
            if (!canSee && _awarenessController != null && _awarenessController.CurrentAwareness <= 0f)
            {
                _currentState = GuardState.Patrolling;
                UpdateFacingDirection();
            }
        }

        private void OnAwarenessDetection()
        {
            // Called when awareness meter reaches threshold - start chasing!
            if (_currentState != GuardState.Chasing)
            {
                StartChasing();
            }
        }

        private void UpdateChasing()
        {
            // Update awareness system (allows it to decay when player not visible)
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // Check if player is still visible
            if (canSee)
            {
                _losePlayerTimer = losePlayerTime;
                // Track last seen position
                _lastSeenPlayerPosition = _player.position;
            }
            else
            {
                _losePlayerTimer -= Time.deltaTime;
                if (_losePlayerTimer <= 0)
                {
                    // Lost sight of player - go investigate last known position
                    StartInvestigating();
                    return;
                }
            }

            // Update pathfinding
            _pathTimer -= Time.deltaTime;
            bool pathExhausted = _currentPath == null || _pathIndex >= _currentPath.Count;
            if (_pathTimer <= 0 || pathExhausted)
            {
                RecalculatePathToPlayer();
                _pathTimer = pathRecalculateInterval;
            }

            // Follow path, or move directly toward player if path is still invalid
            if (_currentPath != null && _pathIndex < _currentPath.Count)
            {
                MoveAlongPath(chaseSpeed);
            }
            else
            {
                // Fallback: move directly toward player
                MoveToward(_player.position, chaseSpeed);
            }
        }

        private void UpdateReturning()
        {
            // Update awareness system (allows it to decay)
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // Check for player detection while returning
            if (canSee)
            {
                StartChasing();
                return;
            }

            // Find nearest waypoint
            int nearestIndex = 0;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < _patrolWaypoints.Count; i++)
            {
                float dist = Vector2.Distance(transform.position, _patrolWaypoints[i]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestIndex = i;
                }
            }

            Vector2 nearestWaypoint = _patrolWaypoints[nearestIndex];

            // Check if close enough to waypoint to resume patrol
            if (nearestDist < 1.0f)
            {
                _currentWaypointIndex = nearestIndex;
                _currentPath = null;
                _currentState = GuardState.Patrolling;
                UpdateFacingDirection();
                return;
            }

            // If path is exhausted or null, move directly toward waypoint
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
            {
                MoveToward(nearestWaypoint, patrolSpeed);
            }
            else
            {
                // Follow A* path until close to destination
                MoveAlongPath(patrolSpeed);
            }

            // Recalculate path periodically
            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0)
            {
                RecalculatePathToNearestWaypoint();
                _pathTimer = pathRecalculateInterval;
            }
        }

        private void StartChasing()
        {
            _currentState = GuardState.Chasing;
            _losePlayerTimer = losePlayerTime;
            _pathTimer = 0;
        }

        private void StartInvestigating()
        {
            _currentState = GuardState.Investigating;
            _pathTimer = 0;
            RecalculatePathToLastSeenPosition();
        }

        private void UpdateInvestigating()
        {
            // Update awareness system (allows it to decay)
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // Check for player detection while investigating
            if (canSee)
            {
                StartChasing();
                return;
            }

            float distToLastSeen = Vector2.Distance(transform.position, _lastSeenPlayerPosition);

            // Check if reached last seen position
            if (distToLastSeen < 1.0f)
            {
                StartSearchingAround();
                return;
            }

            // If path is exhausted or null, move directly toward last seen position
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
            {
                MoveToward(_lastSeenPlayerPosition, chaseSpeed);
            }
            else
            {
                // Follow A* path until close to destination
                MoveAlongPath(chaseSpeed);
            }

            // Recalculate path periodically
            _pathTimer -= Time.deltaTime;
            if (_pathTimer <= 0)
            {
                RecalculatePathToLastSeenPosition();
                _pathTimer = pathRecalculateInterval;
            }
        }

        private void StartSearchingAround()
        {
            _currentState = GuardState.SearchingAround;
            _searchTimer = searchDuration;
            _lookAroundTimer = 0;
            _currentLookDirection = 0;
        }

        private void UpdateSearchingAround()
        {
            // Update awareness system (allows it to decay)
            bool canSee = CanSeePlayer();
            float distanceToPlayer = _player != null ? Vector2.Distance(transform.position, _player.position) : 0f;
            _awarenessController?.UpdateAwareness(canSee, distanceToPlayer);

            // Check for player detection while searching
            if (canSee)
            {
                StartChasing();
                return;
            }

            _searchTimer -= Time.deltaTime;
            _lookAroundTimer -= Time.deltaTime;

            // Rotate to look in different directions
            if (_lookAroundTimer <= 0)
            {
                _lookAroundTimer = lookAroundInterval;
                _currentLookDirection = (_currentLookDirection + 1) % lookDirections;

                // Calculate new facing direction based on look direction index
                float angle = (_currentLookDirection * 360f / lookDirections) * Mathf.Deg2Rad;
                _facingDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _awarenessController?.SetFacingDirection(_facingDirection);
            }

            // Done searching - return to patrol
            if (_searchTimer <= 0)
            {
                StartReturning();
            }
        }

        private void StartReturning()
        {
            _currentState = GuardState.Returning;
            _pathTimer = 0;
            RecalculatePathToNearestWaypoint();

            // Reset awareness so the guard can re-detect the player
            _awarenessController?.ResetAwareness();
        }

        private bool CanSeePlayer()
        {
            if (_player == null) return false;
            if (_awarenessController == null || _awarenessController.Config == null) return false;

            // Can't see player in no-clip mode
            if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                return false;

            // Can't see player when invisible
            if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
                return false;

            var config = _awarenessController.Config;
            Vector2 guardPos = transform.position;
            Vector2 playerPos = _player.position;
            Vector2 toPlayer = playerPos - guardPos;
            float distance = toPlayer.magnitude;

            // Apply Shadow Blend detection reduction
            float effectiveVisionRange = config.VisionRange;
            if (ShadowBlendManager.Instance != null)
            {
                effectiveVisionRange *= ShadowBlendManager.Instance.DetectionRangeMultiplier;
            }

            // Check range (with Shadow Blend modifier)
            if (distance > effectiveVisionRange)
                return false;

            // Check angle within cone (if not omnidirectional)
            if (config.VisionAngle < 360f)
            {
                float angleToPlayer = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                float facingAngle = Mathf.Atan2(_facingDirection.y, _facingDirection.x) * Mathf.Rad2Deg;
                float angleDiff = Mathf.DeltaAngle(facingAngle, angleToPlayer);

                if (Mathf.Abs(angleDiff) > config.VisionAngle / 2f)
                    return false;
            }

            // Raycast to check wall obstruction
            if (config.RequiresLineOfSight)
            {
                RaycastHit2D hit = Physics2D.Raycast(guardPos, toPlayer.normalized, distance, config.WallLayer);
                if (hit.collider != null) return false;
            }

            return true;
        }

        private void MoveToward(Vector2 target, float speed)
        {
            Vector2 currentPos = transform.position;
            Vector2 direction = (target - currentPos).normalized;

            // Apply caltrops slow effect if present
            var slowEffect = GetComponent<CaltropsSlowEffect>();
            if (slowEffect != null && slowEffect.IsSlowed)
            {
                speed *= slowEffect.GetSpeedMultiplier();
            }

            transform.position = Vector2.MoveTowards(currentPos, target, speed * Time.deltaTime);

            // Update facing direction while moving
            if (direction.sqrMagnitude > 0.001f)
            {
                _facingDirection = direction;
                _awarenessController?.SetFacingDirection(direction);
            }
        }

        private void MoveAlongPath(float speed)
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
                return;

            // Apply caltrops slow effect if present
            var slowEffect = GetComponent<CaltropsSlowEffect>();
            if (slowEffect != null && slowEffect.IsSlowed)
            {
                speed *= slowEffect.GetSpeedMultiplier();
            }

            Vector3 targetPosition = new Vector3(_currentPath[_pathIndex].x, _currentPath[_pathIndex].y, 0);

            // Update facing direction
            Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                _facingDirection = direction;
                _awarenessController?.SetFacingDirection(direction);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                _pathIndex++;
            }
        }

        private void RecalculatePathToPlayer()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(_player.position.x),
                Mathf.RoundToInt(_player.position.y)
            );

            // Only recalculate if target or self has moved significantly, or path is exhausted
            bool targetMoved = Mathf.Abs(targetPos.x - _lastPathTargetPos.x) + Mathf.Abs(targetPos.y - _lastPathTargetPos.y) >= TargetMoveThreshold;
            bool selfMoved = Mathf.Abs(currentPos.x - _lastPathOwnPos.x) + Mathf.Abs(currentPos.y - _lastPathOwnPos.y) >= TargetMoveThreshold;
            bool pathExhausted = _currentPath == null || _pathIndex >= _currentPath.Count;

            if (targetMoved || selfMoved || pathExhausted)
            {
                _currentPath = _pathfinding.FindPath(currentPos, targetPos);
                // Skip the first node (current position) to avoid looking backward
                _pathIndex = (_currentPath != null && _currentPath.Count > 1) ? 1 : 0;
                _lastPathTargetPos = targetPos;
                _lastPathOwnPos = currentPos;
            }
        }

        private void RecalculatePathToLastSeenPosition()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(_lastSeenPlayerPosition.x),
                Mathf.RoundToInt(_lastSeenPlayerPosition.y)
            );

            _currentPath = _pathfinding.FindPath(currentPos, targetPos);
            // Skip the first node (current position) to avoid looking backward
            _pathIndex = (_currentPath != null && _currentPath.Count > 1) ? 1 : 0;
        }

        private void RecalculatePathToNearestWaypoint()
        {
            Vector2Int currentPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );

            // Find nearest waypoint
            float nearestDist = float.MaxValue;
            Vector2 nearestWaypoint = _patrolWaypoints[0];
            foreach (var waypoint in _patrolWaypoints)
            {
                float dist = Vector2.Distance(transform.position, waypoint);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestWaypoint = waypoint;
                }
            }

            Vector2Int targetPos = new Vector2Int(
                Mathf.RoundToInt(nearestWaypoint.x),
                Mathf.RoundToInt(nearestWaypoint.y)
            );

            _currentPath = _pathfinding.FindPath(currentPos, targetPos);
            // Skip the first node (current position) to avoid looking backward
            _pathIndex = (_currentPath != null && _currentPath.Count > 1) ? 1 : 0;
        }

        private void UpdateFacingDirection()
        {
            if (_patrolWaypoints == null || _patrolWaypoints.Count == 0)
                return;

            Vector2 target = _patrolWaypoints[_currentWaypointIndex];
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                _facingDirection = direction;
                _awarenessController?.SetFacingDirection(direction);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Don't deal damage in no-clip mode or invisibility mode
            if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                return;

            if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
                return;

            if (other.CompareTag("Player") && _attackTimer <= 0)
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);
                    _attackTimer = attackCooldown;

                    Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                    other.transform.position += (Vector3)(knockbackDir * 0.5f);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Don't deal damage in no-clip mode or invisibility mode
            if (NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive)
                return;

            if (InvisibilityManager.Instance != null && InvisibilityManager.Instance.IsInvisible)
                return;

            if (other.CompareTag("Player") && _attackTimer <= 0)
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);
                    _attackTimer = attackCooldown;

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

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _patrolWaypoints == null || _patrolWaypoints.Count < 2)
                return;

            // Draw patrol path
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _patrolWaypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(_patrolWaypoints[i], _patrolWaypoints[i + 1]);
            }

            // Draw waypoints
            Gizmos.color = Color.green;
            foreach (var wp in _patrolWaypoints)
            {
                Gizmos.DrawWireSphere(wp, 0.2f);
            }

            // Highlight current target
            if (_currentWaypointIndex >= 0 && _currentWaypointIndex < _patrolWaypoints.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_patrolWaypoints[_currentWaypointIndex], 0.3f);
            }

            // Draw last seen position when investigating/searching
            if (_currentState == GuardState.Investigating || _currentState == GuardState.SearchingAround)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(_lastSeenPlayerPosition, 0.4f);
            }

            // Draw vision cone (only if awareness controller with config exists)
            if (_awarenessController != null && _awarenessController.Config != null)
            {
                var config = _awarenessController.Config;
                Gizmos.color = _currentState switch
                {
                    GuardState.GainingAwareness => new Color(1f, 0.8f, 0f), // Yellow-orange for gaining awareness
                    GuardState.Chasing => Color.red,
                    GuardState.Investigating => new Color(1f, 0.5f, 0f), // Orange
                    GuardState.SearchingAround => Color.magenta,
                    _ => Color.yellow
                };
                Vector3 pos = transform.position;

                float facingAngle = Mathf.Atan2(_facingDirection.y, _facingDirection.x) * Mathf.Rad2Deg;
                float leftAngle = (facingAngle + config.VisionAngle / 2f) * Mathf.Deg2Rad;
                float rightAngle = (facingAngle - config.VisionAngle / 2f) * Mathf.Deg2Rad;

                Vector3 leftDir = new Vector3(Mathf.Cos(leftAngle), Mathf.Sin(leftAngle), 0);
                Vector3 rightDir = new Vector3(Mathf.Cos(rightAngle), Mathf.Sin(rightAngle), 0);

                Gizmos.DrawLine(pos, pos + leftDir * config.VisionRange);
                Gizmos.DrawLine(pos, pos + rightDir * config.VisionRange);
            }
        }

        private void OnDestroy()
        {
            if (_awarenessController != null)
            {
                _awarenessController.OnPlayerDetected -= OnAwarenessDetection;
            }
        }
    }
}
