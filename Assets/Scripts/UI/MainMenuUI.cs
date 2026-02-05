using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Labyrinth.UI
{
    /// <summary>
    /// Handles the main menu UI functionality.
    /// Provides the entry point to start the game.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;

        private void Start()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        /// <summary>
        /// Called when Play button is clicked. Loads the Level Selection scene.
        /// </summary>
        private void OnPlayClicked()
        {
            SceneManager.LoadScene("LevelSelection");
        }
    }
}
