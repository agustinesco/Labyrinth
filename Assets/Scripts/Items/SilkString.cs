using UnityEngine;
using System.Collections;

namespace Labyrinth.Items
{
    /// <summary>
    /// A silk string trap that snares enemies passing through it.
    /// Disappears after a set number of traps or after a duration.
    /// </summary>
    public class SilkString : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float snareDuration = 2f;
        [SerializeField] private int maxTraps = 3;
        [SerializeField] private float lifetime = 30f;

        private int _trapsRemaining;
        private float _lifetimeRemaining;
        private LineRenderer _lineRenderer;

        private void Start()
        {
            _trapsRemaining = maxTraps;
            _lifetimeRemaining = lifetime;
        }

        private void Update()
        {
            _lifetimeRemaining -= Time.deltaTime;
            if (_lifetimeRemaining <= 0)
            {
                Debug.Log("[SilkString] Expired due to timeout");
                Destroy(gameObject);
            }

            // Fade out as lifetime decreases
            if (_lineRenderer != null && lifetime > 0)
            {
                float alpha = Mathf.Clamp01(_lifetimeRemaining / lifetime);
                Color startColor = _lineRenderer.startColor;
                Color endColor = _lineRenderer.endColor;
                startColor.a = alpha;
                endColor.a = alpha;
                _lineRenderer.startColor = startColor;
                _lineRenderer.endColor = endColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TrySnareEnemy(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Backup check in case OnTriggerEnter was missed
            TrySnareEnemy(other);
        }

        private void TrySnareEnemy(Collider2D other)
        {
            // Skip if already used up
            if (_trapsRemaining <= 0) return;

            // Check if it's an enemy (check both the collider object and its parent)
            GameObject enemyObj = other.gameObject;

            var enemyController = enemyObj.GetComponent<Labyrinth.Enemy.EnemyController>();
            var patrollingGuard = enemyObj.GetComponent<Labyrinth.Enemy.PatrollingGuardController>();
            var blindMole = enemyObj.GetComponent<Labyrinth.Enemy.BlindMoleController>();
            var shadowStalker = enemyObj.GetComponent<Labyrinth.Enemy.ShadowStalkerController>();

            // Also check parent in case collider is on a child object
            if (enemyController == null && patrollingGuard == null &&
                blindMole == null && shadowStalker == null && enemyObj.transform.parent != null)
            {
                GameObject parentObj = enemyObj.transform.parent.gameObject;
                enemyController = parentObj.GetComponent<Labyrinth.Enemy.EnemyController>();
                patrollingGuard = parentObj.GetComponent<Labyrinth.Enemy.PatrollingGuardController>();
                blindMole = parentObj.GetComponent<Labyrinth.Enemy.BlindMoleController>();
                shadowStalker = parentObj.GetComponent<Labyrinth.Enemy.ShadowStalkerController>();

                if (enemyController != null || patrollingGuard != null ||
                    blindMole != null || shadowStalker != null)
                {
                    enemyObj = parentObj;
                }
            }

            bool isEnemy = enemyController != null || patrollingGuard != null ||
                          blindMole != null || shadowStalker != null;

            if (!isEnemy) return;

            // Apply snare effect
            var snareEffect = enemyObj.GetComponent<SilkSnareEffect>();
            if (snareEffect == null)
            {
                snareEffect = enemyObj.AddComponent<SilkSnareEffect>();
            }

            if (!snareEffect.IsSnared)
            {
                snareEffect.ApplySnare(snareDuration);
                _trapsRemaining--;

                Debug.Log($"[SilkString] Snared {enemyObj.name}! {_trapsRemaining} traps remaining");

                if (_trapsRemaining <= 0)
                {
                    Debug.Log("[SilkString] All traps used, destroying");
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Initializes the silk string with custom parameters.
        /// </summary>
        public void Initialize(float snareDuration, int maxTraps, float lifetime)
        {
            this.snareDuration = snareDuration;
            this.maxTraps = maxTraps;
            this.lifetime = lifetime;
            _trapsRemaining = maxTraps;
            _lifetimeRemaining = lifetime;
        }

        /// <summary>
        /// Creates a silk string between two points.
        /// </summary>
        public static SilkString CreateBetween(Vector2 pointA, Vector2 pointB, float snareDuration = 2f, int maxTraps = 3, float lifetime = 30f)
        {
            GameObject obj = new GameObject("SilkString");

            // Position at midpoint
            Vector2 midpoint = (pointA + pointB) / 2f;
            obj.transform.position = new Vector3(midpoint.x, midpoint.y, 0);

            // Add LineRenderer for visual
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(pointA.x, pointA.y, 0));
            lr.SetPosition(1, new Vector3(pointB.x, pointB.y, 0));
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.9f, 0.9f, 0.85f, 0.8f); // Silk white color
            lr.endColor = new Color(0.9f, 0.9f, 0.85f, 0.8f);
            lr.sortingOrder = 10;

            // Add Rigidbody2D (required for trigger detection)
            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            // Add BoxCollider2D as trigger along the line
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Calculate collider size and rotation
            Vector2 direction = pointB - pointA;
            float length = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            col.size = new Vector2(length, 0.5f); // Trigger area thickness
            col.offset = Vector2.zero;

            // Add SilkString component
            SilkString silkString = obj.AddComponent<SilkString>();
            silkString._lineRenderer = lr;
            silkString.Initialize(snareDuration, maxTraps, lifetime);

            // Add visibility awareness
            obj.AddComponent<Labyrinth.Visibility.VisibilityAwareEntity>();

            Debug.Log($"[SilkString] Created between {pointA} and {pointB}");
            return silkString;
        }
    }

    /// <summary>
    /// Effect applied to enemies when snared by silk string.
    /// </summary>
    public class SilkSnareEffect : MonoBehaviour
    {
        private float _snareTimeRemaining;
        private bool _isSnared;
        private Vector3 _snarePosition;

        // Cache enemy controllers
        private Labyrinth.Enemy.EnemyController _enemyController;
        private Labyrinth.Enemy.PatrollingGuardController _patrollingGuard;
        private Labyrinth.Enemy.BlindMoleController _blindMole;
        private Labyrinth.Enemy.ShadowStalkerController _shadowStalker;
        private Rigidbody2D _rigidbody;

        public bool IsSnared => _isSnared;

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            _enemyController = GetComponent<Labyrinth.Enemy.EnemyController>();
            _patrollingGuard = GetComponent<Labyrinth.Enemy.PatrollingGuardController>();
            _blindMole = GetComponent<Labyrinth.Enemy.BlindMoleController>();
            _shadowStalker = GetComponent<Labyrinth.Enemy.ShadowStalkerController>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        public void ApplySnare(float duration)
        {
            if (_isSnared) return;

            // Re-cache components in case Awake didn't run or they weren't found
            if (_enemyController == null && _patrollingGuard == null &&
                _blindMole == null && _shadowStalker == null)
            {
                CacheComponents();
            }

            _isSnared = true;
            _snareTimeRemaining = duration;
            _snarePosition = transform.position;

            // Disable enemy movement by disabling their controllers
            if (_enemyController != null) _enemyController.enabled = false;
            if (_patrollingGuard != null) _patrollingGuard.enabled = false;
            if (_blindMole != null) _blindMole.enabled = false;
            if (_shadowStalker != null) _shadowStalker.enabled = false;

            // Stop rigidbody movement if present
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.simulated = false;
            }

            Debug.Log($"[SilkSnare] Enemy snared for {duration} seconds");
        }

        private void Update()
        {
            if (!_isSnared) return;

            _snareTimeRemaining -= Time.deltaTime;
            if (_snareTimeRemaining <= 0)
            {
                ReleaseSnare();
            }
        }

        // Use LateUpdate to ensure position is locked AFTER enemy Update runs
        private void LateUpdate()
        {
            if (!_isSnared) return;

            // Force enemy to stay in place after any movement attempts
            transform.position = _snarePosition;
        }

        private void ReleaseSnare()
        {
            _isSnared = false;

            // Re-enable enemy movement
            if (_enemyController != null) _enemyController.enabled = true;
            if (_patrollingGuard != null) _patrollingGuard.enabled = true;
            if (_blindMole != null) _blindMole.enabled = true;
            if (_shadowStalker != null) _shadowStalker.enabled = true;

            // Re-enable rigidbody if present
            if (_rigidbody != null)
            {
                _rigidbody.simulated = true;
            }

            Debug.Log("[SilkSnare] Enemy released from snare");
        }

        private void OnDestroy()
        {
            // Ensure enemy is released if this component is destroyed
            if (_isSnared)
            {
                ReleaseSnare();
            }
        }
    }
}
