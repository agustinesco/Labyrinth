using UnityEngine;
using Labyrinth.Player;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Blind Mole enemy that spawns at 4-way intersections.
    /// Cycles between inactive and active states. When active, detects nearby
    /// player movement and throws a projectile before hiding underground.
    /// </summary>
    public class BlindMoleController : MonoBehaviour
    {
        public enum MoleState
        {
            Inactive,   // Underground, not detecting
            Emerging,   // Warning state - visible but not yet detecting
            Active,     // Above ground, detecting player movement
            Attacking,  // Throwing projectile
            Hiding      // Going back underground
        }

        [Header("State Timing")]
        [SerializeField] private float inactiveDuration = 3f;
        [SerializeField] private float emergingDuration = 1f;
        [SerializeField] private float activeDuration = 4f;
        [SerializeField] private float hidingDuration = 1f;

        [Header("Detection")]
        [SerializeField] private float detectionRadius = 4f;
        [SerializeField] private float movementThreshold = 0.1f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private int projectileDamage = 1;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.4f, 0.3f, 0.2f, 0.5f);
        [SerializeField] private Color emergingColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
        [SerializeField] private Color activeColor = new Color(0.6f, 0.4f, 0.3f, 1f);

        private MoleState _currentState = MoleState.Inactive;
        private float _stateTimer;
        private Transform _player;
        private Vector3 _lastPlayerPosition;
        private bool _playerWasInRange;
        private Collider2D _collider;

        public MoleState CurrentState => _currentState;

        private void Start()
        {
            _collider = GetComponent<Collider2D>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            FindPlayer();
            EnterState(MoleState.Inactive);
        }

        private void Update()
        {
            // Try to find player if not set, but continue updating even without one (test mode)
            if (_player == null)
            {
                FindPlayer();
            }

            _stateTimer -= Time.deltaTime;

            switch (_currentState)
            {
                case MoleState.Inactive:
                    UpdateInactive();
                    break;
                case MoleState.Emerging:
                    UpdateEmerging();
                    break;
                case MoleState.Active:
                    UpdateActive();
                    break;
                case MoleState.Attacking:
                    UpdateAttacking();
                    break;
                case MoleState.Hiding:
                    UpdateHiding();
                    break;
            }
        }

        private void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
                _lastPlayerPosition = _player.position;
            }
        }

        private void EnterState(MoleState newState)
        {
            _currentState = newState;

            switch (newState)
            {
                case MoleState.Inactive:
                    _stateTimer = inactiveDuration;
                    SetVisuals(MoleState.Inactive);
                    if (_collider != null) _collider.enabled = false;
                    break;

                case MoleState.Emerging:
                    _stateTimer = emergingDuration;
                    SetVisuals(MoleState.Emerging);
                    if (_collider != null) _collider.enabled = true;
                    // Reset player position tracking so movement detection starts fresh when Active
                    if (_player != null)
                        _lastPlayerPosition = _player.position;
                    break;

                case MoleState.Active:
                    _stateTimer = activeDuration;
                    SetVisuals(MoleState.Active);
                    if (_collider != null) _collider.enabled = true;
                    _playerWasInRange = false;
                    if (_player != null)
                        _lastPlayerPosition = _player.position;
                    break;

                case MoleState.Attacking:
                    _stateTimer = 0.3f; // Brief attack animation time
                    break;

                case MoleState.Hiding:
                    _stateTimer = hidingDuration;
                    SetVisuals(MoleState.Hiding);
                    if (_collider != null) _collider.enabled = false;
                    break;
            }
        }

        private void UpdateInactive()
        {
            if (_stateTimer <= 0)
            {
                EnterState(MoleState.Emerging);
            }
        }

        private void UpdateEmerging()
        {
            // Warning state - mole is visible but not detecting movement yet
            // Player has time to notice and stop moving
            if (_stateTimer <= 0)
            {
                EnterState(MoleState.Active);
            }

            // Keep tracking player position so detection starts fresh when Active
            if (_player != null)
                _lastPlayerPosition = _player.position;
        }

        private void UpdateActive()
        {
            if (_stateTimer <= 0)
            {
                EnterState(MoleState.Inactive);
                return;
            }

            // Skip player detection if no player (test mode)
            if (_player == null)
                return;

            // Check if player is in detection range
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            bool playerInRange = distanceToPlayer <= detectionRadius;

            if (playerInRange)
            {
                // Check if player moved
                float playerMovement = Vector2.Distance(_player.position, _lastPlayerPosition);

                if (_playerWasInRange && playerMovement > movementThreshold)
                {
                    // Player moved while in range - attack!
                    ThrowProjectile();
                    EnterState(MoleState.Attacking);
                }

                _playerWasInRange = true;
            }
            else
            {
                _playerWasInRange = false;
            }

            _lastPlayerPosition = _player.position;
        }

        private void UpdateAttacking()
        {
            if (_stateTimer <= 0)
            {
                EnterState(MoleState.Hiding);
            }
        }

        private void UpdateHiding()
        {
            if (_stateTimer <= 0)
            {
                EnterState(MoleState.Inactive);
            }
        }

        private void ThrowProjectile()
        {
            if (_player == null) return;

            Vector2 direction = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            GameObject projectile;
            if (projectilePrefab != null)
            {
                projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // Create projectile dynamically if no prefab assigned
                projectile = CreateProjectileDynamically();
            }

            var moleProjectile = projectile.GetComponent<BlindMoleProjectile>();
            if (moleProjectile != null)
            {
                moleProjectile.Initialize(direction, projectileSpeed, projectileDamage);
            }
        }

        private GameObject CreateProjectileDynamically()
        {
            var projectile = new GameObject("MoleProjectile");
            projectile.transform.position = transform.position;

            // Add sprite renderer
            var sr = projectile.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(0.5f, 0.3f, 0.2f);
            sr.sortingOrder = 10;

            // Add collider
            var collider = projectile.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;

            // Add projectile script
            projectile.AddComponent<BlindMoleProjectile>();

            return projectile;
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 16;
            var texture = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void SetVisuals(MoleState state)
        {
            if (spriteRenderer != null)
            {
                switch (state)
                {
                    case MoleState.Inactive:
                    case MoleState.Hiding:
                        spriteRenderer.color = inactiveColor;
                        break;
                    case MoleState.Emerging:
                        spriteRenderer.color = emergingColor;
                        break;
                    case MoleState.Active:
                    case MoleState.Attacking:
                        spriteRenderer.color = activeColor;
                        break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
