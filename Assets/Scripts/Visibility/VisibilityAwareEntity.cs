using UnityEngine;

namespace Labyrinth.Visibility
{
    /// <summary>
    /// Hides this entity's renderers when outside the player's line of sight.
    /// Attach to enemies, items, traps, or any object that should be hidden in fog.
    /// </summary>
    public class VisibilityAwareEntity : MonoBehaviour
    {
        [SerializeField] private float visibilityThreshold = 0.1f;
        [SerializeField] private bool fadeInsteadOfHide = false;
        [SerializeField] private float fadeSpeed = 5f;

        private Renderer[] _renderers;
        private float _targetAlpha;
        private float _currentAlpha;
        private bool _initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            _renderers = GetComponentsInChildren<Renderer>();
            _currentAlpha = 0f;
            _targetAlpha = 0f;
            _initialized = true;

            // Start hidden
            UpdateRenderersVisibility(false);
        }

        private void LateUpdate()
        {
            if (!_initialized) Initialize();

            var fogManager = FogOfWarManager.Instance;
            if (fogManager == null)
            {
                // No fog manager, show everything
                UpdateRenderersVisibility(true);
                return;
            }

            bool isVisible = fogManager.IsPositionVisible(transform.position, visibilityThreshold);

            if (fadeInsteadOfHide)
            {
                _targetAlpha = isVisible ? 1f : 0f;
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);
                UpdateRenderersAlpha(_currentAlpha);
            }
            else
            {
                UpdateRenderersVisibility(isVisible);
            }
        }

        private void UpdateRenderersVisibility(bool visible)
        {
            if (_renderers == null) return;

            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void UpdateRenderersAlpha(float alpha)
        {
            if (_renderers == null) return;

            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = alpha > 0.01f;

                    // Try to set alpha on sprite renderers
                    if (renderer is SpriteRenderer spriteRenderer)
                    {
                        var color = spriteRenderer.color;
                        color.a = alpha;
                        spriteRenderer.color = color;
                    }
                }
            }
        }
    }
}
