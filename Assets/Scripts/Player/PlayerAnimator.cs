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
        private float _lastHorizontalInput;

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
        private static readonly int WalkDiagonalUpHash = Animator.StringToHash("PlayerMovingDiagonallyUp");
        private static readonly int WalkDiagonalDownHash = Animator.StringToHash("PlayerMovingDiagonallyDown");

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

            // Initialize facing direction based on current scale
            _facingLeft = transform.localScale.x < 0;

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

                // Update sprite flip direction when there's meaningful horizontal input
                // Use a threshold to prevent flickering from tiny input values
                if (Mathf.Abs(facingDir.x) > 0.1f)
                {
                    _lastHorizontalInput = facingDir.x;
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
                _animator.Play(animHash, 0);
            }
        }

        private void PlayWalkAnimation(Vector2 facingDir)
        {
            float absX = Mathf.Abs(facingDir.x);
            float absY = Mathf.Abs(facingDir.y);

            // Diagonal movement: both axes have significant input
            bool isDiagonal = absX > 0.3f && absY > 0.3f;

            if (isDiagonal)
            {
                if (facingDir.y > 0)
                {
                    PlayAnimation(WalkDiagonalUpHash);
                }
                else
                {
                    PlayAnimation(WalkDiagonalDownHash);
                }
            }
            else if (absY > absX)
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
            bool currentlyFacingLeft = scale.x < 0;

            // Only flip if the direction actually changed
            if (currentlyFacingLeft != _facingLeft)
            {
                scale.x = _facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
}
