using UnityEngine;

namespace Labyrinth.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerController _playerController;
        private Rigidbody2D _rb;
        private bool _isMoving;
        private Vector2 _lastFacingDirection = Vector2.down;
        private int _currentAnimHash;
        private bool _facingLeft;

        // Thresholds with hysteresis to prevent rapid toggling
        private const float MovingThresholdHigh = 0.1f;
        private const float MovingThresholdLow = 0.01f;

        // Animation state hashes for performance and reliable comparison
        private static readonly int IdleLookingUpHash = Animator.StringToHash("AN_PlayerIdleLookingUp");
        private static readonly int IdleLookingDownHash = Animator.StringToHash("AN_PlayerIdleLookingDown");
        private static readonly int IdleLookingSideHash = Animator.StringToHash("AN_PlayerIdleLookingSide");
        private static readonly int WalkUpHash = Animator.StringToHash("WalkUp");
        private static readonly int WalkDownHash = Animator.StringToHash("WalkDown");
        private static readonly int WalkHorizontalHash = Animator.StringToHash("WalkHorizontal");

        // Parameter hashes to disable automatic transitions
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _playerController = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            // Disable automatic transitions by setting neutral parameter values
            _animator.SetBool(IsMovingHash, false);
            _animator.SetFloat(MoveXHash, 0f);
            _animator.SetFloat(MoveYHash, 0f);

            // Play initial idle animation looking down
            _currentAnimHash = IdleLookingDownHash;
            _animator.Play(IdleLookingDownHash, 0);
        }

        private void Update()
        {
            if (_playerController == null || _animator == null || _rb == null)
                return;

            Vector2 facingDir = _playerController.FacingDirection;

            // Use hysteresis to prevent rapid toggling of isMoving state
            float velocitySqr = _rb.velocity.sqrMagnitude;
            if (_isMoving)
            {
                _isMoving = velocitySqr > MovingThresholdLow;
            }
            else
            {
                _isMoving = velocitySqr > MovingThresholdHigh;
            }

            bool isMoving = facingDir.sqrMagnitude > 0.01f && _isMoving;

            // Track facing direction while moving
            if (isMoving)
            {
                _lastFacingDirection = facingDir;

                // Update sprite flip direction only while moving and primarily horizontal
                if (Mathf.Abs(facingDir.x) > Mathf.Abs(facingDir.y))
                {
                    _facingLeft = facingDir.x < 0;
                }
            }

            // Determine and play the appropriate animation
            if (isMoving)
            {
                PlayWalkAnimation(facingDir);
            }
            else
            {
                PlayIdleAnimation();
            }

            // Apply sprite flip
            ApplySpriteFlip();
        }

        private void PlayAnimation(int animHash)
        {
            // Only change animation if it's different from current
            if (_currentAnimHash != animHash)
            {
                _currentAnimHash = animHash;
                // Use CrossFade for smoother transitions, or Play without resetting time
                _animator.CrossFade(animHash, 0f, 0);
            }
        }

        private void PlayWalkAnimation(Vector2 facingDir)
        {
            if (Mathf.Abs(facingDir.y) > Mathf.Abs(facingDir.x))
            {
                if (facingDir.y > 0)
                {
                    PlayAnimation(WalkUpHash);
                }
                else
                {
                    PlayAnimation(WalkDownHash);
                }
            }
            else
            {
                PlayAnimation(WalkHorizontalHash);
            }
        }

        private void PlayIdleAnimation()
        {
            if (Mathf.Abs(_lastFacingDirection.y) > Mathf.Abs(_lastFacingDirection.x))
            {
                if (_lastFacingDirection.y > 0)
                {
                    PlayAnimation(IdleLookingUpHash);
                }
                else
                {
                    PlayAnimation(IdleLookingDownHash);
                }
            }
            else
            {
                PlayAnimation(IdleLookingSideHash);
            }
        }

        private void ApplySpriteFlip()
        {
            Vector3 scale = transform.localScale;
            float targetScaleX = _facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);

            if (!Mathf.Approximately(scale.x, targetScaleX))
            {
                scale.x = targetScaleX;
                transform.localScale = scale;
            }
        }
    }
}
