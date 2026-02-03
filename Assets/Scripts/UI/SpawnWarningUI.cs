using UnityEngine;
using TMPro;
using Labyrinth.Core;

namespace Labyrinth.UI
{
    public class SpawnWarningUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private CanvasGroup canvasGroup;

        private float _fadeTimer;
        private bool _isShowing;

        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn += ShowWarning;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn -= ShowWarning;
            }
        }

        private void Update()
        {
            if (_isShowing)
            {
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
        }

        private void ShowWarning()
        {
            _isShowing = true;
            _fadeTimer = displayDuration;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
        }
    }
}
