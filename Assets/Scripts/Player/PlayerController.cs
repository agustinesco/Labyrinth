using UnityEngine;
using Labyrinth.Leveling;

namespace Labyrinth.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 5f;
        [SerializeField] private float wallDetectionDistance = 1.2f;
        [SerializeField] private LayerMask wallLayer;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Vector2 _facingDirection = Vector2.right;
        private float _speedBonus;
        private float _speedBoostTimer;
        private bool _isNearWall;

        public float CurrentSpeed
        {
            get
            {
                float baseTotal = baseSpeed + _speedBonus + (PlayerLevelSystem.Instance?.PermanentSpeedBonus ?? 0f);

                // Apply Wall Hugger bonus if near a wall
                if (_isNearWall && PlayerLevelSystem.Instance != null)
                {
                    float wallHuggerBonus = PlayerLevelSystem.Instance.WallHuggerSpeedBonus;
                    baseTotal *= (1f + wallHuggerBonus);
                }

                return baseTotal * (NoClipManager.Instance?.SpeedMultiplier ?? 1f);
            }
        }
        public Vector2 FacingDirection => _facingDirection;
        public bool IsNearWall => _isNearWall;

        /// <summary>
        /// Multiplier affecting how fast enemies' awareness meters fill.
        /// Lower values = stealthier (harder to detect).
        /// Base value is 1.0, reduced by sneakiness bonuses from upgrades.
        /// </summary>
        public float SneakinessMultiplier
        {
            get
            {
                float baseMultiplier = 1f;

                // Apply permanent sneakiness bonus from upgrades (reduces multiplier)
                if (PlayerLevelSystem.Instance != null)
                {
                    baseMultiplier -= PlayerLevelSystem.Instance.SneakinessBonus;
                }

                // Apply Shadow Blend effect (reduces detection)
                if (ShadowBlendManager.Instance != null && ShadowBlendManager.Instance.IsBlended)
                {
                    baseMultiplier *= ShadowBlendManager.Instance.DetectionRangeMultiplier;
                }

                // Clamp to minimum of 0.1 (can never be completely undetectable through awareness)
                return Mathf.Max(0.1f, baseMultiplier);
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;
        }

        private void Update()
        {
            if (_speedBoostTimer > 0)
            {
                _speedBoostTimer -= Time.deltaTime;
                if (_speedBoostTimer <= 0)
                {
                    _speedBonus = 0;
                }
            }

            // Check for nearby walls (Wall Hugger upgrade)
            CheckNearbyWalls();
        }

        private void CheckNearbyWalls()
        {
            // Cast rays in 4 cardinal directions to detect walls
            Vector2 pos = transform.position;
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

            _isNearWall = false;
            foreach (var dir in directions)
            {
                RaycastHit2D hit = Physics2D.Raycast(pos, dir, wallDetectionDistance, wallLayer);
                if (hit.collider != null)
                {
                    _isNearWall = true;
                    break;
                }
            }
        }

        private void FixedUpdate()
        {
            Vector2 movement = _moveInput.normalized * CurrentSpeed;
            _rb.velocity = movement;
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;

            // Update facing direction when there's movement input
            if (input.sqrMagnitude > 0.01f)
            {
                _facingDirection = input.normalized;
            }
        }

        public void ApplySpeedBoost(float bonus, float duration)
        {
            _speedBonus = bonus;
            _speedBoostTimer = duration;
        }

        public float GetSpeedBoostTimeRemaining()
        {
            return _speedBoostTimer;
        }
    }
}
