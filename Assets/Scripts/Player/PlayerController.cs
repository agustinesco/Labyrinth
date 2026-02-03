using UnityEngine;
using Labyrinth.Leveling;

namespace Labyrinth.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 5f;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Vector2 _facingDirection = Vector2.right;
        private float _speedBonus;
        private float _speedBoostTimer;

        public float CurrentSpeed => (baseSpeed + _speedBonus + (PlayerLevelSystem.Instance?.PermanentSpeedBonus ?? 0f)) * (NoClipManager.Instance?.SpeedMultiplier ?? 1f);
        public Vector2 FacingDirection => _facingDirection;

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
