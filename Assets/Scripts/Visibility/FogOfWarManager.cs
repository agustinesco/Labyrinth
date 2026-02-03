using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Player;
using Labyrinth.Leveling;

namespace Labyrinth.Visibility
{
    /// <summary>
    /// Manages cone-based fog of war visibility using dual textures:
    /// - Visibility texture: Updated each frame with currently visible cells
    /// - Exploration texture: Persistent, marks cells that have ever been seen
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        [Header("Cone Settings")]
        [SerializeField] private int rayCount = 120;
        [SerializeField] private float baseVisibilityRadius = 8f;
        [SerializeField] private float coneAngle = 120f;

        [Header("Ambient Circle Settings")]
        [SerializeField] private float ambientRadius = 3f;
        [SerializeField] private int ambientRayCount = 60;

        [Header("References")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private Renderer darknessOverlay;

        [Header("Maze Settings")]
        [SerializeField] private int mazeWidth = 25;
        [SerializeField] private int mazeHeight = 25;

        [Header("Smoothness Settings")]
        [SerializeField] private int textureResolutionMultiplier = 8;
        [SerializeField] private float edgeSoftness = 0.25f;

        private Texture2D _visibilityTexture;
        private Texture2D _explorationTexture;
        private float[,] _exploredValues;
        private float[,] _visibilityValues;
        private int _texWidth;
        private int _texHeight;

        private Transform _player;
        private PlayerController _playerController;
        private Material _material;

        private float _visibilityBonus;
        private float _visibilityBoostTimer;

        private List<PlacedLightSource> _placedLightSources = new List<PlacedLightSource>();

        private static readonly int VisibilityTexProperty = Shader.PropertyToID("_VisibilityTex");
        private static readonly int ExplorationTexProperty = Shader.PropertyToID("_ExplorationTex");
        private static readonly int MazeSizeProperty = Shader.PropertyToID("_MazeSize");

        public float CurrentRadius => baseVisibilityRadius + _visibilityBonus + (PlayerLevelSystem.Instance?.PermanentVisionBonus ?? 0f);

        private void Awake()
        {
            InitializeTextures();
        }

        private void Start()
        {
            FindPlayer();
            SetupMaterial();
        }

        private void InitializeTextures()
        {
            _texWidth = mazeWidth * textureResolutionMultiplier;
            _texHeight = mazeHeight * textureResolutionMultiplier;

            // Create visibility texture (cleared each frame) with point filtering for sharp edges
            _visibilityTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.RFloat, false);
            _visibilityTexture.filterMode = FilterMode.Point;
            _visibilityTexture.wrapMode = TextureWrapMode.Clamp;

            // Create exploration texture (persistent) with point filtering for sharp edges
            _explorationTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.RFloat, false);
            _explorationTexture.filterMode = FilterMode.Point;
            _explorationTexture.wrapMode = TextureWrapMode.Clamp;

            // Initialize value arrays
            _visibilityValues = new float[_texWidth, _texHeight];
            _exploredValues = new float[_texWidth, _texHeight];

            // Clear both textures initially
            ClearTexture(_visibilityTexture);
            ClearTexture(_explorationTexture);
        }

        private void ClearTexture(Texture2D texture)
        {
            var pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        private void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
                _playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        private void SetupMaterial()
        {
            if (darknessOverlay == null) return;

            // Create material instance
            _material = darknessOverlay.material;

            // Set textures and maze size
            _material.SetTexture(VisibilityTexProperty, _visibilityTexture);
            _material.SetTexture(ExplorationTexProperty, _explorationTexture);
            _material.SetVector(MazeSizeProperty, new Vector4(mazeWidth, mazeHeight, 0, 0));
        }

        private void Update()
        {
            UpdateVisibilityBoostTimer();
        }

        private void LateUpdate()
        {
            // Try to find player if not set (player may be spawned after Start)
            if (_player == null || _playerController == null)
            {
                FindPlayer();
                if (_player == null) return;
            }

            // Ensure material is set up
            if (_material == null)
            {
                SetupMaterial();
                if (_material == null) return;
            }

            // Ensure textures are initialized
            if (_visibilityTexture == null || _explorationTexture == null || _visibilityValues == null)
            {
                InitializeTextures();
            }

            UpdateVisibility();
        }

        private void UpdateVisibilityBoostTimer()
        {
            if (_visibilityBoostTimer > 0)
            {
                _visibilityBoostTimer -= Time.deltaTime;
                if (_visibilityBoostTimer <= 0)
                {
                    _visibilityBonus = 0;
                }
            }
        }

        private void UpdateVisibility()
        {
            // Clear visibility values
            System.Array.Clear(_visibilityValues, 0, _visibilityValues.Length);

            Vector2 playerPos = _player.position;
            Vector2 facing = _playerController.FacingDirection;
            float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

            // Cast rays for ambient circle (360° around player with smaller radius)
            float ambientAngleStep = 360f / ambientRayCount;
            for (int i = 0; i < ambientRayCount; i++)
            {
                float angle = i * ambientAngleStep * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Raycast to find wall
                RaycastHit2D hit = Physics2D.Raycast(playerPos, direction, ambientRadius, wallLayer);
                float maxDistance = hit.collider != null ? hit.distance : ambientRadius;

                // Mark points along this ray with smooth falloff
                MarkPointsAlongRay(playerPos, direction, maxDistance, ambientRadius);
            }

            // Cast rays across the directional cone
            float startAngle = facingAngle - coneAngle / 2f;
            float angleStep = coneAngle / rayCount;

            for (int i = 0; i <= rayCount; i++)
            {
                float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Raycast to find wall
                RaycastHit2D hit = Physics2D.Raycast(playerPos, direction, CurrentRadius, wallLayer);
                float maxDistance = hit.collider != null ? hit.distance : CurrentRadius;

                // Mark points along this ray with smooth falloff
                MarkPointsAlongRay(playerPos, direction, maxDistance, CurrentRadius);
            }

            // Cast rays from all placed light sources (360° circles)
            foreach (var lightSource in _placedLightSources)
            {
                if (lightSource == null) continue;

                Vector2 lightPos = lightSource.Position;
                float lightRadius = lightSource.LightRadius;
                int lightRayCount = lightSource.RayCount;

                float lightAngleStep = 360f / lightRayCount;
                for (int i = 0; i < lightRayCount; i++)
                {
                    float angle = i * lightAngleStep * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // Raycast to find wall
                    RaycastHit2D hit = Physics2D.Raycast(lightPos, direction, lightRadius, wallLayer);
                    float maxDistance = hit.collider != null ? hit.distance : lightRadius;

                    // Mark points along this ray
                    MarkPointsAlongRay(lightPos, direction, maxDistance, lightRadius);
                }
            }

            // Apply visibility values to texture
            ApplyVisibilityToTexture();
        }

        private void MarkPointsAlongRay(Vector2 origin, Vector2 direction, float maxDistance, float maxRadius)
        {
            float step = 0.25f / textureResolutionMultiplier;
            for (float d = 0; d <= maxDistance; d += step)
            {
                Vector2 point = origin + direction * d;

                // Sharp visibility - fully visible within range
                float visibility = 1f;

                // Only apply falloff if edgeSoftness > 0
                if (edgeSoftness > 0)
                {
                    // Calculate visibility based on distance from edge (smooth falloff at walls)
                    float distanceFromEdge = maxDistance - d;
                    float edgeVisibility = Mathf.Clamp01(distanceFromEdge / edgeSoftness);

                    // Also apply radial falloff near the max radius
                    float radialFalloff = 1f - Mathf.Clamp01((d - (maxRadius - edgeSoftness)) / edgeSoftness);
                    visibility = Mathf.Min(edgeVisibility, radialFalloff);
                }

                MarkPointVisible(point.x, point.y, visibility);
            }

            // When ray hits a wall, mark a full tile area to fully reveal the wall
            if (maxDistance < maxRadius)
            {
                Vector2 endPoint = origin + direction * maxDistance;

                // Mark a 1x1 tile area centered on the hit point to fully reveal wall
                // This ensures no shadow remains at wall edges
                for (float dx = -0.5f; dx <= 0.5f; dx += step)
                {
                    for (float dy = -0.5f; dy <= 0.5f; dy += step)
                    {
                        MarkPointVisible(endPoint.x + dx, endPoint.y + dy, 1f);
                    }
                }
            }
        }

        private void MarkPointVisible(float worldX, float worldY, float visibility)
        {
            // Convert world position to texture coordinates
            int texX = Mathf.RoundToInt(worldX * textureResolutionMultiplier);
            int texY = Mathf.RoundToInt(worldY * textureResolutionMultiplier);

            if (texX < 0 || texX >= _texWidth || texY < 0 || texY >= _texHeight)
                return;

            // Use max visibility (don't reduce visibility if already higher)
            _visibilityValues[texX, texY] = Mathf.Max(_visibilityValues[texX, texY], visibility);

            // Update exploration (permanent, keeps max value)
            if (visibility > _exploredValues[texX, texY])
            {
                _exploredValues[texX, texY] = visibility;
            }
        }

        private void ApplyVisibilityToTexture()
        {
            bool explorationDirty = false;

            for (int x = 0; x < _texWidth; x++)
            {
                for (int y = 0; y < _texHeight; y++)
                {
                    float vis = _visibilityValues[x, y];
                    _visibilityTexture.SetPixel(x, y, new Color(vis, vis, vis, 1f));

                    // Check if exploration needs update
                    float currentExplored = _explorationTexture.GetPixel(x, y).r;
                    if (_exploredValues[x, y] > currentExplored)
                    {
                        _explorationTexture.SetPixel(x, y, new Color(_exploredValues[x, y], 0, 0, 1f));
                        explorationDirty = true;
                    }
                }
            }

            _visibilityTexture.Apply();

            if (explorationDirty)
            {
                _explorationTexture.Apply();
            }
        }

        /// <summary>
        /// Applies a temporary visibility boost.
        /// </summary>
        public void ApplyVisibilityBoost(float bonus, float duration)
        {
            _visibilityBonus = bonus;
            _visibilityBoostTimer = duration;
        }

        /// <summary>
        /// Gets the remaining time on the visibility boost.
        /// </summary>
        public float GetVisibilityBoostTimeRemaining()
        {
            return _visibilityBoostTimer;
        }

        /// <summary>
        /// Sets the player reference manually.
        /// </summary>
        public void SetPlayer(Transform player)
        {
            _player = player;
            _playerController = player.GetComponent<PlayerController>();
        }

        /// <summary>
        /// Sets the maze dimensions and reinitializes textures.
        /// </summary>
        public void SetMazeDimensions(int width, int height)
        {
            mazeWidth = width;
            mazeHeight = height;
            InitializeTextures();
            SetupMaterial();
        }

        /// <summary>
        /// Resets the exploration state (clears all explored cells).
        /// </summary>
        public void ResetExploration()
        {
            _exploredValues = new float[_texWidth, _texHeight];
            ClearTexture(_explorationTexture);
        }

        /// <summary>
        /// Registers a placed light source to be included in visibility calculations.
        /// </summary>
        public void RegisterLightSource(PlacedLightSource lightSource)
        {
            if (!_placedLightSources.Contains(lightSource))
            {
                _placedLightSources.Add(lightSource);
            }
        }

        /// <summary>
        /// Unregisters a placed light source.
        /// </summary>
        public void UnregisterLightSource(PlacedLightSource lightSource)
        {
            _placedLightSources.Remove(lightSource);
        }

        /// <summary>
        /// Gets the wall layer mask for raycasting.
        /// </summary>
        public LayerMask WallLayer => wallLayer;

        /// <summary>
        /// Checks if a world position is currently visible to the player.
        /// </summary>
        public bool IsPositionVisible(Vector2 worldPosition, float threshold = 0.1f)
        {
            if (_visibilityValues == null) return false;

            int texX = Mathf.RoundToInt(worldPosition.x * textureResolutionMultiplier);
            int texY = Mathf.RoundToInt(worldPosition.y * textureResolutionMultiplier);

            if (texX < 0 || texX >= _texWidth || texY < 0 || texY >= _texHeight)
                return false;

            return _visibilityValues[texX, texY] >= threshold;
        }

        /// <summary>
        /// Gets the visibility value at a world position (0 = not visible, 1 = fully visible).
        /// </summary>
        public float GetVisibilityAt(Vector2 worldPosition)
        {
            if (_visibilityValues == null) return 0f;

            int texX = Mathf.RoundToInt(worldPosition.x * textureResolutionMultiplier);
            int texY = Mathf.RoundToInt(worldPosition.y * textureResolutionMultiplier);

            if (texX < 0 || texX >= _texWidth || texY < 0 || texY >= _texHeight)
                return 0f;

            return _visibilityValues[texX, texY];
        }

        /// <summary>
        /// Singleton-style access for easy querying from other scripts.
        /// </summary>
        public static FogOfWarManager Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDestroy()
        {
            if (_visibilityTexture != null)
            {
                Destroy(_visibilityTexture);
            }
            if (_explorationTexture != null)
            {
                Destroy(_explorationTexture);
            }
        }
    }
}
