using UnityEngine;
using Labyrinth.Maze;

namespace Labyrinth.Items
{
    /// <summary>
    /// Manages the glider effect that allows the player to pass through walls.
    /// When the effect ends, if the player is inside a wall, they are teleported to the nearest floor tile.
    /// </summary>
    public class GliderEffect : MonoBehaviour
    {
        public static GliderEffect Instance { get; private set; }

        private float _remainingDuration;
        private bool _isActive;
        private Collider2D _playerCollider;
        private MazeGrid _mazeGrid;
        private SpriteRenderer _playerSprite;
        private Color _originalColor;

        [Header("Visual Feedback")]
        [SerializeField] private Color gliderColor = new Color(0.5f, 0.8f, 1f, 0.6f);

        public bool IsActive => _isActive;
        public float RemainingDuration => _remainingDuration;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _playerCollider = GetComponent<Collider2D>();
            _playerSprite = GetComponent<SpriteRenderer>();
            if (_playerSprite != null)
            {
                _originalColor = _playerSprite.color;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Activates the glider effect for the specified duration.
        /// </summary>
        public void Activate(float duration, MazeGrid grid)
        {
            _mazeGrid = grid;
            _remainingDuration = duration;

            if (!_isActive)
            {
                _isActive = true;
                EnableWallPass(true);
                UpdateVisual(true);
                Debug.Log($"[Glider] Activated for {duration} seconds");
            }
            else
            {
                // Extend duration if already active
                Debug.Log($"[Glider] Extended duration by {duration} seconds");
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            _remainingDuration -= Time.deltaTime;

            // Pulse effect while active
            if (_playerSprite != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                Color pulsedColor = Color.Lerp(gliderColor, _originalColor, pulse * 0.3f);
                pulsedColor.a = gliderColor.a;
                _playerSprite.color = pulsedColor;
            }

            if (_remainingDuration <= 0)
            {
                Deactivate();
            }
        }

        private void Deactivate()
        {
            _isActive = false;
            EnableWallPass(false);
            UpdateVisual(false);

            // Check if player is inside a wall
            if (IsInsideWall())
            {
                TeleportToNearestFloor();
            }

            Debug.Log("[Glider] Deactivated");
        }

        private void EnableWallPass(bool enable)
        {
            if (_playerCollider == null) return;

            // Find all wall colliders and ignore collision with player
            int wallLayer = LayerMask.NameToLayer("Walls");
            if (wallLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, wallLayer, enable);
            }
        }

        private void UpdateVisual(bool active)
        {
            if (_playerSprite == null) return;

            if (active)
            {
                _playerSprite.color = gliderColor;
            }
            else
            {
                _playerSprite.color = _originalColor;
            }
        }

        private bool IsInsideWall()
        {
            if (_mazeGrid == null) return false;

            Vector2 playerPos = transform.position;
            int cellX = Mathf.FloorToInt(playerPos.x);
            int cellY = Mathf.FloorToInt(playerPos.y);

            // Check if position is within grid bounds
            if (cellX < 0 || cellX >= _mazeGrid.Width || cellY < 0 || cellY >= _mazeGrid.Height)
            {
                return true; // Out of bounds = inside wall
            }

            return _mazeGrid.GetCell(cellX, cellY).IsWall;
        }

        private void TeleportToNearestFloor()
        {
            if (_mazeGrid == null)
            {
                Debug.LogWarning("[Glider] Cannot find nearest floor - no maze grid reference");
                return;
            }

            Vector2 playerPos = transform.position;
            int playerCellX = Mathf.FloorToInt(playerPos.x);
            int playerCellY = Mathf.FloorToInt(playerPos.y);

            Vector2Int? nearestFloor = FindNearestFloorTile(playerCellX, playerCellY);

            if (nearestFloor.HasValue)
            {
                // Teleport to center of the floor tile
                Vector3 newPos = new Vector3(nearestFloor.Value.x + 0.5f, nearestFloor.Value.y + 0.5f, transform.position.z);
                transform.position = newPos;
                Debug.Log($"[Glider] Teleported player to nearest floor at {nearestFloor.Value}");
            }
            else
            {
                Debug.LogWarning("[Glider] Could not find a nearby floor tile!");
            }
        }

        private Vector2Int? FindNearestFloorTile(int startX, int startY)
        {
            // Search in expanding squares around the player position
            int maxSearchRadius = 20;

            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                // Check all cells at this radius
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Only check cells on the perimeter of the square
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;

                        int checkX = startX + dx;
                        int checkY = startY + dy;

                        // Check bounds
                        if (checkX < 0 || checkX >= _mazeGrid.Width || checkY < 0 || checkY >= _mazeGrid.Height)
                            continue;

                        // Check if this is a floor tile
                        if (!_mazeGrid.GetCell(checkX, checkY).IsWall)
                        {
                            return new Vector2Int(checkX, checkY);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Force deactivate the effect (e.g., when player dies or level ends).
        /// </summary>
        public void ForceDeactivate()
        {
            if (_isActive)
            {
                _remainingDuration = 0;
                Deactivate();
            }
        }
    }
}
