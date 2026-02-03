using UnityEngine;

namespace Labyrinth.Player
{
    /// <summary>
    /// Manages no-clip cheat mode state.
    /// When active: player passes through walls, moves 3x faster,
    /// enemies don't see/chase player, traps don't trigger.
    /// </summary>
    public class NoClipManager : MonoBehaviour
    {
        public static NoClipManager Instance { get; private set; }

        [SerializeField] private float speedMultiplier = 3f;
        [SerializeField] private Color noClipTint = new Color(0.5f, 0.5f, 1f, 0.7f);

        private bool _isNoClipActive;
        private Collider2D _playerCollider;
        private SpriteRenderer _playerSprite;
        private Color _originalColor;
        private int _originalLayer;

        public bool IsNoClipActive => _isNoClipActive;
        public float SpeedMultiplier => _isNoClipActive ? speedMultiplier : 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ToggleNoClip()
        {
            if (_isNoClipActive)
                DisableNoClip();
            else
                EnableNoClip();
        }

        public void EnableNoClip()
        {
            if (_isNoClipActive) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            _playerCollider = player.GetComponent<Collider2D>();
            _playerSprite = player.GetComponent<SpriteRenderer>();

            if (_playerCollider != null)
            {
                _originalLayer = player.layer;
                // Disable the collider to allow passing through walls
                _playerCollider.enabled = false;
            }

            if (_playerSprite != null)
            {
                _originalColor = _playerSprite.color;
                _playerSprite.color = noClipTint;
            }

            _isNoClipActive = true;
            Debug.Log("[NoClip] Enabled - Ghost mode active");
        }

        public void DisableNoClip()
        {
            if (!_isNoClipActive) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (_playerCollider != null)
                {
                    // Re-enable the collider
                    _playerCollider.enabled = true;
                }

                if (_playerSprite != null)
                {
                    _playerSprite.color = _originalColor;
                }
            }

            _isNoClipActive = false;
            Debug.Log("[NoClip] Disabled - Solid mode active");
        }
    }
}
