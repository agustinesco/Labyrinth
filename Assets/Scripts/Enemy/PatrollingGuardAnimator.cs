using UnityEngine;

namespace Labyrinth.Enemy
{
    [RequireComponent(typeof(Animator))]
    public class PatrollingGuardAnimator : MonoBehaviour
    {
        private Animator _animator;

        // Movement tracking
        private Vector3 _lastPosition;
        private Vector2 _velocity;
        private Vector2 _lockedDirection = Vector2.down;
        private bool _wasMoving;

        // Animator parameter hashes for performance
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private const float VelocityThreshold = 0.5f;
        private const float DirectionLockThreshold = 0.3f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_animator == null)
                return;

            // Calculate velocity from actual position change
            Vector3 currentPosition = transform.position;
            _velocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;

            float speed = _velocity.magnitude;
            bool isMoving = speed > VelocityThreshold;

            // Only update direction when moving fast enough to be reliable
            if (speed > DirectionLockThreshold)
            {
                _lockedDirection = _velocity.normalized;
            }

            // Update animator parameters
            _animator.SetFloat(MoveXHash, _lockedDirection.x);
            _animator.SetFloat(MoveYHash, _lockedDirection.y);
            _animator.SetBool(IsMovingHash, isMoving);

            // Handle horizontal sprite flipping
            if (Mathf.Abs(_lockedDirection.x) > Mathf.Abs(_lockedDirection.y))
            {
                Vector3 scale = transform.localScale;
                scale.x = _lockedDirection.x < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                transform.localScale = scale;
            }

            _wasMoving = isMoving;
        }
    }
}
