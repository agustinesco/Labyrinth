using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Labyrinth.Core;

namespace Labyrinth.UI
{
    /// <summary>
    /// Handles the game over/win UI display.
    /// Shows appropriate message when game ends and provides restart/menu options.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private Button menuButton;

        private void Start()
        {
            // Start with panel hidden
            if (panel != null)
            {
                panel.SetActive(false);
            }

            // Wire up button listeners
            if (tryAgainButton != null)
            {
                tryAgainButton.onClick.AddListener(OnTryAgain);
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenu);
            }

            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameWin += ShowWin;
                GameManager.Instance.OnGameLose += ShowLose;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameWin -= ShowWin;
                GameManager.Instance.OnGameLose -= ShowLose;
            }
        }

        /// <summary>
        /// Shows the win message when player escapes.
        /// </summary>
        private void ShowWin()
        {
            if (messageText != null)
            {
                messageText.text = "Escaped!";
            }
            
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        /// <summary>
        /// Shows the lose message when player is caught.
        /// </summary>
        private void ShowLose()
        {
            if (messageText != null)
            {
                messageText.text = "Caught!";
            }
            
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        /// <summary>
        /// Called when Try Again button is clicked. Restarts the current game.
        /// </summary>
        private void OnTryAgain()
        {
            GameManager.Instance?.RestartGame();
        }

        /// <summary>
        /// Called when Menu button is clicked. Returns to main menu.
        /// </summary>
        private void OnMenu()
        {
            GameManager.Instance?.LoadMainMenu();
        }
    }
}
