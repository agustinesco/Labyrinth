using UnityEngine;
using TMPro;

namespace Labyrinth.UI
{
    /// <summary>
    /// Singleton UI that displays temporary item-related messages in the center of the screen.
    /// </summary>
    public class ItemMessageUI : MonoBehaviour
    {
        public static ItemMessageUI Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 1.5f;

        private float _fadeTimer;
        private bool _isShowing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!_isShowing) return;

            _fadeTimer -= Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / displayDuration);
            }

            if (_fadeTimer <= 0)
            {
                _isShowing = false;
            }
        }

        public void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
            _isShowing = true;
            _fadeTimer = displayDuration;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
        }
    }
}
