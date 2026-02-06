using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Player;
using Labyrinth.Leveling;
using Labyrinth.Items;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Labyrinth.Visibility
{
    /// <summary>
    /// Manages cone-based fog of war visibility using dual textures.
    /// Optimized for mobile with throttled updates and batched texture operations.
    /// </summary>
    [ExecuteAlways]
    public class FogOfWarManager : MonoBehaviour
    {
        [Header("Cone Settings")]
        [SerializeField, Tooltip("Number of rays for directional vision cone (lower = better performance)")]
        private int rayCount = 100;
        [SerializeField] private float baseVisibilityRadius = 4f;
        [SerializeField] private float coneAngle = 60f;

        [Header("Ambient Circle Settings")]
        [SerializeField] private float ambientRadius = 3f;
        [SerializeField, Tooltip("Number of rays for ambient circle (lower = better performance)")]
        private int ambientRayCount = 60;

        [Header("References")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private Renderer darknessOverlay;

        [Header("Maze Settings")]
        [SerializeField] private int mazeWidth = 25;
        [SerializeField] private int mazeHeight = 25;

        [Header("Performance Settings")]
        [SerializeField, Tooltip("Texture resolution multiplier (lower = better performance, 6 is balanced for mobile)")]
        private int textureResolutionMultiplier = 10;
        [SerializeField, Tooltip("Update visibility every N frames (1 = every frame, 2 = every other frame, etc.)")]
        private int updateEveryNFrames = 2;
        [SerializeField, Tooltip("Distance step for ray marching (higher = better performance, lower = more accurate)")]
        private float rayMarchStep = 0.12f;

        [Header("Smoothness Settings")]
        [SerializeField] private float edgeSoftness = 0.35f;

        [Header("Visibility Opacity Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of areas never seen (fully dark by default)")]
        private float undiscoveredOpacity = 1.0f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of areas seen before but not currently visible")]
        private float discoveredOpacity = 0.7f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacity of areas currently in line of sight (transparent by default)")]
        private float visibleOpacity = 0.0f;
        [SerializeField, Tooltip("Color of the fog")]
        private Color fogColor = Color.black;

        private Texture2D _visibilityTexture;
        private Texture2D _explorationTexture;
        private float[,] _exploredValues;
        private float[,] _visibilityValues;
        private bool _mapRevealed;
        private int _texWidth;
        private int _texHeight;

        // Cached pixel arrays to avoid allocations
        private Color[] _visibilityPixels;
        private Color[] _explorationPixels;
        private bool _explorationDirty;

        private Transform _player;
        private PlayerController _playerController;
        private Material _material;

        private float _visibilityBonus;
        private float _visibilityBoostTimer;

        private List<PlacedLightSource> _placedLightSources = new List<PlacedLightSource>();

        // Frame throttling
        private int _frameCounter;
        private Vector2 _lastPlayerPos;
        private Vector2 _lastFacingDir;
        private bool _forceUpdate;

        // Dirty region tracking — only update the texture region that changed
        private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY;
        private int _prevDirtyMinX, _prevDirtyMinY, _prevDirtyMaxX, _prevDirtyMaxY;
        private bool _hasDirtyRegion;
        private bool _hasPrevDirtyRegion;

        private static readonly int VisibilityTexProperty = Shader.PropertyToID("_VisibilityTex");
        private static readonly int ExplorationTexProperty = Shader.PropertyToID("_ExplorationTex");
        private static readonly int MazeSizeProperty = Shader.PropertyToID("_MazeSize");
        private static readonly int UndiscoveredOpacityProperty = Shader.PropertyToID("_UnexploredOpacity");
        private static readonly int DiscoveredOpacityProperty = Shader.PropertyToID("_ExploredOpacity");
        private static readonly int VisibleOpacityProperty = Shader.PropertyToID("_VisibleOpacity");
        private static readonly int FogColorProperty = Shader.PropertyToID("_FogColor");
        private static readonly int TexelSizeProperty = Shader.PropertyToID("_TexelSize");

        public float CurrentRadius => baseVisibilityRadius + _visibilityBonus + (PlayerLevelSystem.Instance?.PermanentVisionBonus ?? 0f);

        /// <summary>
        /// Opacity of undiscovered areas (0 = transparent, 1 = fully opaque).
        /// </summary>
        public float UndiscoveredOpacity
        {
            get => undiscoveredOpacity;
            set
            {
                undiscoveredOpacity = Mathf.Clamp01(value);
                UpdateShaderOpacitySettings();
            }
        }

        /// <summary>
        /// Opacity of discovered but not currently visible areas (0 = transparent, 1 = fully opaque).
        /// </summary>
        public float DiscoveredOpacity
        {
            get => discoveredOpacity;
            set
            {
                discoveredOpacity = Mathf.Clamp01(value);
                UpdateShaderOpacitySettings();
            }
        }

        /// <summary>
        /// Opacity of areas currently in line of sight (0 = transparent, 1 = fully opaque).
        /// </summary>
        public float VisibleOpacity
        {
            get => visibleOpacity;
            set
            {
                visibleOpacity = Mathf.Clamp01(value);
                UpdateShaderOpacitySettings();
            }
        }

        /// <summary>
        /// Color of the fog.
        /// </summary>
        public Color FogColor
        {
            get => fogColor;
            set
            {
                fogColor = value;
                UpdateShaderOpacitySettings();
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            InitializeTextures();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (darknessOverlay != null)
                    darknessOverlay.gameObject.SetActive(false);
#endif
                return;
            }

            if (darknessOverlay != null)
            {
                darknessOverlay.gameObject.SetActive(true);
            }

            FindPlayer();
            SetupMaterial();
        }

        private void InitializeTextures()
        {
            _texWidth = mazeWidth * textureResolutionMultiplier;
            _texHeight = mazeHeight * textureResolutionMultiplier;

            // Create visibility texture with bilinear filtering for smoother look at lower resolution
            _visibilityTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.RFloat, false);
            _visibilityTexture.filterMode = FilterMode.Bilinear;
            _visibilityTexture.wrapMode = TextureWrapMode.Clamp;

            // Create exploration texture
            _explorationTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.RFloat, false);
            _explorationTexture.filterMode = FilterMode.Bilinear;
            _explorationTexture.wrapMode = TextureWrapMode.Clamp;

            // Initialize value arrays
            _visibilityValues = new float[_texWidth, _texHeight];
            _exploredValues = new float[_texWidth, _texHeight];

            // Pre-allocate pixel arrays to avoid GC allocations
            int pixelCount = _texWidth * _texHeight;
            _visibilityPixels = new Color[pixelCount];
            _explorationPixels = new Color[pixelCount];

            // Initialize pixel arrays to black
            for (int i = 0; i < pixelCount; i++)
            {
                _visibilityPixels[i] = Color.black;
                _explorationPixels[i] = Color.black;
            }

            _visibilityTexture.SetPixels(_visibilityPixels);
            _visibilityTexture.Apply();
            _explorationTexture.SetPixels(_explorationPixels);
            _explorationTexture.Apply();
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _material = darknessOverlay.sharedMaterial;
            }
            else
#endif
            {
                _material = darknessOverlay.material;
            }

            if (_material == null) return;

            _material.SetTexture(VisibilityTexProperty, _visibilityTexture);
            _material.SetTexture(ExplorationTexProperty, _explorationTexture);
            _material.SetVector(MazeSizeProperty, new Vector4(mazeWidth, mazeHeight, 0, 0));
            _material.SetVector(TexelSizeProperty, new Vector4(
                1f / (mazeWidth * textureResolutionMultiplier),
                1f / (mazeHeight * textureResolutionMultiplier), 0, 0));

            UpdateShaderOpacitySettings();
        }

        private void UpdateShaderOpacitySettings()
        {
            if (_material == null) return;

            _material.SetFloat(UndiscoveredOpacityProperty, undiscoveredOpacity);
            _material.SetFloat(DiscoveredOpacityProperty, discoveredOpacity);
            _material.SetFloat(VisibleOpacityProperty, visibleOpacity);
            _material.SetColor(FogColorProperty, fogColor);
        }

        private void Update()
        {
            UpdateVisibilityBoostTimer();
        }

        private void LateUpdate()
        {
            if (_player == null || _playerController == null)
            {
                FindPlayer();
                if (_player == null) return;
            }

            if (_material == null)
            {
                SetupMaterial();
                if (_material == null) return;
            }

            if (_visibilityTexture == null || _explorationTexture == null || _visibilityValues == null)
            {
                InitializeTextures();
            }

            // Frame throttling - skip updates based on settings
            _frameCounter++;

            // Check if player moved significantly or turned
            Vector2 currentPos = _player.position;
            Vector2 currentFacing = _playerController.FacingDirection;
            bool playerMoved = Vector2.SqrMagnitude(currentPos - _lastPlayerPos) > 0.01f;
            bool playerTurned = Vector2.Dot(currentFacing, _lastFacingDir) < 0.99f;

            // Force update if player moved/turned, otherwise respect frame throttling
            bool shouldUpdate = _forceUpdate || playerMoved || playerTurned || (_frameCounter >= updateEveryNFrames);

            if (shouldUpdate)
            {
                _frameCounter = 0;
                _forceUpdate = false;
                _lastPlayerPos = currentPos;
                _lastFacingDir = currentFacing;
                UpdateVisibility();
            }
        }

        private void UpdateVisibilityBoostTimer()
        {
            if (_visibilityBoostTimer > 0)
            {
                _visibilityBoostTimer -= Time.deltaTime;
                if (_visibilityBoostTimer <= 0)
                {
                    _visibilityBonus = 0;
                    _forceUpdate = true;
                }
            }
        }

        private void UpdateVisibility()
        {
            if (_mapRevealed)
            {
                // Fill all visibility to fully visible
                for (int x = 0; x < _texWidth; x++)
                    for (int y = 0; y < _texHeight; y++)
                        _visibilityValues[x, y] = 1f;
                _dirtyMinX = 0; _dirtyMinY = 0;
                _dirtyMaxX = _texWidth - 1; _dirtyMaxY = _texHeight - 1;
                _hasDirtyRegion = true;
                ApplyVisibilityToTexture();
                return;
            }

            // Save previous dirty bounds
            _prevDirtyMinX = _dirtyMinX;
            _prevDirtyMinY = _dirtyMinY;
            _prevDirtyMaxX = _dirtyMaxX;
            _prevDirtyMaxY = _dirtyMaxY;
            _hasPrevDirtyRegion = _hasDirtyRegion;

            // Reset current dirty bounds
            _dirtyMinX = _texWidth;
            _dirtyMinY = _texHeight;
            _dirtyMaxX = 0;
            _dirtyMaxY = 0;
            _hasDirtyRegion = false;

            // Clear only the previous dirty region instead of the full array
            if (_hasPrevDirtyRegion)
            {
                for (int x = _prevDirtyMinX; x <= _prevDirtyMaxX; x++)
                    for (int y = _prevDirtyMinY; y <= _prevDirtyMaxY; y++)
                        _visibilityValues[x, y] = 0f;
            }

            _explorationDirty = false;

            Vector2 playerPos = _player.position;
            Vector2 facing = _playerController.FacingDirection;
            float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

            // Check if glider is active - vision passes through walls
            bool gliderActive = GliderEffect.Instance != null && GliderEffect.Instance.IsActive;

            // Cast rays for ambient circle (360° around player with smaller radius)
            float ambientAngleStep = 360f / ambientRayCount;
            for (int i = 0; i < ambientRayCount; i++)
            {
                float angle = i * ambientAngleStep * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float maxDistance = ambientRadius;
                if (!gliderActive)
                {
                    RaycastHit2D hit = Physics2D.Raycast(playerPos, direction, ambientRadius, wallLayer);
                    if (hit.collider != null) maxDistance = hit.distance;
                }

                MarkPointsAlongRay(playerPos, direction, maxDistance, ambientRadius);
            }

            // Cast rays across the directional cone
            float startAngle = facingAngle - coneAngle / 2f;
            float angleStep = coneAngle / rayCount;

            for (int i = 0; i <= rayCount; i++)
            {
                float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float maxDistance = CurrentRadius;
                if (!gliderActive)
                {
                    RaycastHit2D hit = Physics2D.Raycast(playerPos, direction, CurrentRadius, wallLayer);
                    if (hit.collider != null) maxDistance = hit.distance;
                }

                MarkPointsAlongRay(playerPos, direction, maxDistance, CurrentRadius);
            }

            // Cast rays from all placed light sources
            for (int ls = 0; ls < _placedLightSources.Count; ls++)
            {
                var lightSource = _placedLightSources[ls];
                if (lightSource == null) continue;

                Vector2 lightPos = lightSource.Position;
                float lightRadius = lightSource.LightRadius;
                int lightRayCount = Mathf.Min(lightSource.RayCount, 32); // Cap light rays for performance

                float lightAngleStep = 360f / lightRayCount;
                for (int i = 0; i < lightRayCount; i++)
                {
                    float angle = i * lightAngleStep * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    RaycastHit2D hit = Physics2D.Raycast(lightPos, direction, lightRadius, wallLayer);
                    float maxDistance = hit.collider != null ? hit.distance : lightRadius;

                    MarkPointsAlongRay(lightPos, direction, maxDistance, lightRadius);
                }
            }

            // Apply visibility values to texture using batched operations
            ApplyVisibilityToTexture();
        }

        private void MarkPointsAlongRay(Vector2 origin, Vector2 direction, float maxDistance, float maxRadius)
        {
            // Use configurable step size for performance
            float step = rayMarchStep;

            for (float d = 0; d <= maxDistance; d += step)
            {
                Vector2 point = origin + direction * d;

                float visibility = 1f;

                if (edgeSoftness > 0)
                {
                    float distanceFromEdge = maxDistance - d;
                    float edgeVisibility = Mathf.Clamp01(distanceFromEdge / edgeSoftness);
                    float radialFalloff = 1f - Mathf.Clamp01((d - (maxRadius - edgeSoftness)) / edgeSoftness);
                    visibility = Mathf.Min(edgeVisibility, radialFalloff);
                }

                MarkPointVisible(point.x, point.y, visibility);
            }

        }

        private void MarkPointVisible(float worldX, float worldY, float visibility)
        {
            int texX = Mathf.RoundToInt(worldX * textureResolutionMultiplier);
            int texY = Mathf.RoundToInt(worldY * textureResolutionMultiplier);

            // Mark a small area around the point to fill gaps between rays
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int px = texX + dx;
                    int py = texY + dy;

                    if (px < 0 || px >= _texWidth || py < 0 || py >= _texHeight)
                        continue;

                    // Use max visibility
                    if (visibility > _visibilityValues[px, py])
                    {
                        _visibilityValues[px, py] = visibility;
                    }

                    // Update exploration
                    if (visibility > _exploredValues[px, py])
                    {
                        _exploredValues[px, py] = visibility;
                        _explorationDirty = true;
                    }

                    // Track dirty region bounds
                    if (px < _dirtyMinX) _dirtyMinX = px;
                    if (px > _dirtyMaxX) _dirtyMaxX = px;
                    if (py < _dirtyMinY) _dirtyMinY = py;
                    if (py > _dirtyMaxY) _dirtyMaxY = py;
                    _hasDirtyRegion = true;
                }
            }
        }

        private void ApplyVisibilityToTexture()
        {
            // Determine update region (union of previous + current dirty bounds)
            int updateMinX, updateMinY, updateMaxX, updateMaxY;

            if (_hasPrevDirtyRegion && _hasDirtyRegion)
            {
                updateMinX = Mathf.Max(0, Mathf.Min(_prevDirtyMinX, _dirtyMinX));
                updateMinY = Mathf.Max(0, Mathf.Min(_prevDirtyMinY, _dirtyMinY));
                updateMaxX = Mathf.Min(_texWidth - 1, Mathf.Max(_prevDirtyMaxX, _dirtyMaxX));
                updateMaxY = Mathf.Min(_texHeight - 1, Mathf.Max(_prevDirtyMaxY, _dirtyMaxY));
            }
            else if (_hasDirtyRegion)
            {
                updateMinX = Mathf.Max(0, _dirtyMinX);
                updateMinY = Mathf.Max(0, _dirtyMinY);
                updateMaxX = Mathf.Min(_texWidth - 1, _dirtyMaxX);
                updateMaxY = Mathf.Min(_texHeight - 1, _dirtyMaxY);
            }
            else if (_hasPrevDirtyRegion)
            {
                updateMinX = Mathf.Max(0, _prevDirtyMinX);
                updateMinY = Mathf.Max(0, _prevDirtyMinY);
                updateMaxX = Mathf.Min(_texWidth - 1, _prevDirtyMaxX);
                updateMaxY = Mathf.Min(_texHeight - 1, _prevDirtyMaxY);
            }
            else
            {
                return; // Nothing to update
            }

            // Only update pixels within the dirty region
            for (int y = updateMinY; y <= updateMaxY; y++)
            {
                int rowStart = y * _texWidth;
                for (int x = updateMinX; x <= updateMaxX; x++)
                {
                    int index = rowStart + x;
                    float vis = _visibilityValues[x, y];
                    _visibilityPixels[index] = new Color(vis, vis, vis, 1f);

                    if (_explorationDirty)
                    {
                        float explored = _exploredValues[x, y];
                        _explorationPixels[index] = new Color(explored, 0, 0, 1f);
                    }
                }
            }

            // Upload textures
            _visibilityTexture.SetPixels(_visibilityPixels);
            _visibilityTexture.Apply(false);

            if (_explorationDirty)
            {
                _explorationTexture.SetPixels(_explorationPixels);
                _explorationTexture.Apply(false);
            }
        }

        /// <summary>
        /// Forces a visibility update on the next frame.
        /// </summary>
        public void ForceUpdate()
        {
            _forceUpdate = true;
        }

        /// <summary>
        /// Applies a temporary visibility boost.
        /// </summary>
        public void ApplyVisibilityBoost(float bonus, float duration)
        {
            _visibilityBonus = bonus;
            _visibilityBoostTimer = duration;
            _forceUpdate = true;
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
            _forceUpdate = true;
        }

        /// <summary>
        /// Sets the maze dimensions and reinitializes textures and overlay.
        /// </summary>
        public void SetMazeDimensions(int width, int height)
        {
            mazeWidth = width;
            mazeHeight = height;
            InitializeTextures();
            SetupMaterial();
            ResizeDarknessOverlay();
        }

        private void ResizeDarknessOverlay()
        {
            if (darknessOverlay == null) return;

            darknessOverlay.transform.position = new Vector3(
                mazeWidth / 2f,
                mazeHeight / 2f,
                darknessOverlay.transform.position.z
            );

            float scaleMultiplier = 2f;
            darknessOverlay.transform.localScale = new Vector3(
                mazeWidth * scaleMultiplier,
                mazeHeight * scaleMultiplier,
                1f
            );
        }

        /// <summary>
        /// Resets the exploration state.
        /// </summary>
        public void ResetExploration()
        {
            if (_exploredValues != null)
                System.Array.Clear(_exploredValues, 0, _exploredValues.Length);

            if (_explorationPixels != null)
            {
                for (int i = 0; i < _explorationPixels.Length; i++)
                    _explorationPixels[i] = Color.black;

                _explorationTexture.SetPixels(_explorationPixels);
                _explorationTexture.Apply(false);
            }
            _forceUpdate = true;
        }

        /// <summary>
        /// Reveals the entire map.
        /// </summary>
        public void RevealEntireMap()
        {
            if (_exploredValues == null || _explorationPixels == null)
            {
                Debug.LogWarning("[FogOfWar] Cannot reveal map - textures not initialized");
                return;
            }

            _mapRevealed = true;

            for (int x = 0; x < _texWidth; x++)
            {
                for (int y = 0; y < _texHeight; y++)
                {
                    _exploredValues[x, y] = 1f;
                }
            }

            for (int i = 0; i < _explorationPixels.Length; i++)
            {
                _explorationPixels[i] = new Color(1f, 0, 0, 1f);
            }

            _explorationTexture.SetPixels(_explorationPixels);
            _explorationTexture.Apply(false);
            _forceUpdate = true;
            Debug.Log("[FogOfWar] Entire map revealed");
        }

        /// <summary>
        /// Registers a placed light source.
        /// </summary>
        public void RegisterLightSource(PlacedLightSource lightSource)
        {
            if (!_placedLightSources.Contains(lightSource))
            {
                _placedLightSources.Add(lightSource);
                _forceUpdate = true;
            }
        }

        /// <summary>
        /// Unregisters a placed light source.
        /// </summary>
        public void UnregisterLightSource(PlacedLightSource lightSource)
        {
            if (_placedLightSources.Remove(lightSource))
            {
                _forceUpdate = true;
            }
        }

        /// <summary>
        /// Gets the wall layer mask.
        /// </summary>
        public LayerMask WallLayer => wallLayer;

        /// <summary>
        /// Checks if a world position is currently visible.
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
        /// Gets the visibility value at a world position.
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
        /// Singleton access.
        /// </summary>
        public static FogOfWarManager Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UpdateOverlayVisibility();
#endif
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            EditorApplication.delayCall += UpdateOverlayVisibility;
        }

        private void UpdateOverlayVisibility()
        {
            if (darknessOverlay != null && darknessOverlay.gameObject != null)
            {
                bool shouldBeActive = Application.isPlaying;
                if (darknessOverlay.gameObject.activeSelf != shouldBeActive)
                {
                    darknessOverlay.gameObject.SetActive(shouldBeActive);
                }
            }
        }
#endif

        private void OnDestroy()
        {
            if (_visibilityTexture != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_visibilityTexture);
                else
#endif
                    Destroy(_visibilityTexture);
            }
            if (_explorationTexture != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_explorationTexture);
                else
#endif
                    Destroy(_explorationTexture);
            }

            _visibilityPixels = null;
            _explorationPixels = null;
        }
    }
}
