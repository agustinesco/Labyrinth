using UnityEngine;

namespace Labyrinth.Enemy.Awareness
{
    /// <summary>
    /// Visual indicator that shows awareness level above an enemy.
    /// Shows a fill bar that increases as awareness builds.
    /// Only visible when awareness > 0 and < threshold.
    /// </summary>
    public class AwarenessIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private float barWidth = 0.8f;
        [SerializeField] private float barHeight = 0.1f;
        [SerializeField] private float yOffset = 0.7f;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color fillColorLow = new Color(1f, 1f, 0f, 1f); // Yellow
        [SerializeField] private Color fillColorHigh = new Color(1f, 0f, 0f, 1f); // Red

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmount = 0.1f;

        private EnemyAwarenessController _awarenessController;
        private SpriteRenderer _backgroundRenderer;
        private SpriteRenderer _fillRenderer;
        private Transform _fillTransform;
        private float _currentAlpha;
        private bool _isVisible;

        // Cached resources to prevent memory leaks
        private Texture2D _cachedTexture;
        private Sprite _cachedSprite;

        private void Awake()
        {
            _awarenessController = GetComponentInParent<EnemyAwarenessController>();
            if (_awarenessController == null)
            {
                Debug.LogWarning("AwarenessIndicator: No EnemyAwarenessController found in parent!");
                enabled = false;
                return;
            }

            CreateVisuals();
            _awarenessController.OnAwarenessChanged += OnAwarenessChanged;
        }

        private void CreateVisuals()
        {
            // Create background bar
            GameObject backgroundGO = new GameObject("AwarenessBackground");
            backgroundGO.transform.SetParent(transform);
            backgroundGO.transform.localPosition = new Vector3(0, yOffset, 0);
            backgroundGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            _backgroundRenderer = backgroundGO.AddComponent<SpriteRenderer>();
            _backgroundRenderer.sprite = GetOrCreateSquareSprite();
            _backgroundRenderer.color = backgroundColor;
            _backgroundRenderer.sortingOrder = 100;

            // Create fill bar
            GameObject fillGO = new GameObject("AwarenessFill");
            _fillTransform = fillGO.transform;
            _fillTransform.SetParent(transform);
            _fillTransform.localPosition = new Vector3(-barWidth / 2f, yOffset, -0.01f);
            _fillTransform.localScale = new Vector3(0, barHeight, 1f);

            _fillRenderer = fillGO.AddComponent<SpriteRenderer>();
            _fillRenderer.sprite = GetOrCreateSquareSprite();
            _fillRenderer.color = fillColorLow;
            _fillRenderer.sortingOrder = 101;

            // Set pivot for fill bar (left-aligned)
            // We'll handle the positioning in UpdateFillBar

            // Start hidden
            SetAlpha(0f);
        }

        private Sprite GetOrCreateSquareSprite()
        {
            // Return cached sprite if already created
            if (_cachedSprite != null)
                return _cachedSprite;

            _cachedTexture = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++)
                colors[i] = Color.white;
            _cachedTexture.SetPixels(colors);
            _cachedTexture.Apply();

            _cachedSprite = Sprite.Create(_cachedTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _cachedSprite;
        }

        private void OnAwarenessChanged(float awareness)
        {
            if (_awarenessController == null) return;

            float percent = _awarenessController.AwarenessPercent;

            // Show indicator when awareness is building (but not at 0 or full)
            _isVisible = awareness > 0 && !_awarenessController.HasDetectedPlayer;

            UpdateFillBar(percent);
        }

        private void Update()
        {
            // Fade in/out
            float targetAlpha = _isVisible ? 1f : 0f;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(_currentAlpha);

            // Pulse effect when visible
            if (_isVisible && _awarenessController != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * _awarenessController.AwarenessPercent;
                if (_backgroundRenderer != null)
                {
                    _backgroundRenderer.transform.localScale = new Vector3(barWidth * pulse, barHeight * pulse, 1f);
                }
            }

            // Billboard - always face camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void UpdateFillBar(float percent)
        {
            if (_fillTransform == null || _fillRenderer == null) return;

            // Update fill width
            float fillWidth = barWidth * percent;
            _fillTransform.localScale = new Vector3(fillWidth, barHeight, 1f);

            // Position fill bar (left-aligned)
            _fillTransform.localPosition = new Vector3(-barWidth / 2f + fillWidth / 2f, yOffset, -0.01f);

            // Lerp color from low to high based on percent
            _fillRenderer.color = Color.Lerp(fillColorLow, fillColorHigh, percent);
        }

        private void SetAlpha(float alpha)
        {
            if (_backgroundRenderer != null)
            {
                Color bgColor = _backgroundRenderer.color;
                bgColor.a = backgroundColor.a * alpha;
                _backgroundRenderer.color = bgColor;
            }

            if (_fillRenderer != null)
            {
                Color fillColor = _fillRenderer.color;
                fillColor.a = alpha;
                _fillRenderer.color = fillColor;
            }
        }

        private void OnDestroy()
        {
            if (_awarenessController != null)
            {
                _awarenessController.OnAwarenessChanged -= OnAwarenessChanged;
            }

            // Clean up cached resources to prevent memory leaks
            if (_cachedSprite != null)
            {
                Destroy(_cachedSprite);
                _cachedSprite = null;
            }
            if (_cachedTexture != null)
            {
                Destroy(_cachedTexture);
                _cachedTexture = null;
            }
        }
    }
}
