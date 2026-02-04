using UnityEngine;

namespace Labyrinth.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;
        private PlayerController _playerController;
        private Vector2 _lastMoveInput;

        // Animator parameter hashes for performance
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_playerController == null || _animator == null)
                return;

            Vector2 facingDir = _playerController.FacingDirection;
            bool isMoving = facingDir.sqrMagnitude > 0.01f && GetComponent<Rigidbody2D>().velocity.sqrMagnitude > 0.01f;

            // Update animator parameters
            _animator.SetFloat(MoveXHash, facingDir.x);
            _animator.SetFloat(MoveYHash, facingDir.y);
            _animator.SetBool(IsMovingHash, isMoving);

            // Handle horizontal sprite flipping
            if (isMoving && Mathf.Abs(facingDir.x) > Mathf.Abs(facingDir.y))
            {
                // Moving horizontally - flip sprite based on direction
                Vector3 scale = transform.localScale;
                if (facingDir.x < 0)
                {
                    // Moving left - flip sprite
                    scale.x = -Mathf.Abs(scale.x);
                }
                else
                {
                    // Moving right - normal orientation
                    scale.x = Mathf.Abs(scale.x);
                }
                transform.localScale = scale;
            }
        }
    }
}
