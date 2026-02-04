using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Labyrinth.UI
{
    /// <summary>
    /// Manages the pause menu with a tab system for System and Bestiary tabs.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Pause Button")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseButtonIcon;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button closeButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button systemTabButton;
        [SerializeField] private Button bestiaryTabButton;

        [Header("Tab Panels")]
        [SerializeField] private GameObject systemPanel;
        [SerializeField] private GameObject bestiaryPanel;

        [Header("System Tab")]
        [SerializeField] private Button returnToMenuButton;

        [Header("Tab Visual Settings")]
        [SerializeField] private Color activeTabColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.2f, 0.2f, 0.25f, 1f);

        private float _previousTimeScale = 1f;
        private bool _isPaused;

        private enum Tab { System, Bestiary }
        private Tab _currentTab = Tab.System;

        private void Start()
        {
            // Ensure pause panel starts hidden
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            // Setup button listeners
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Resume);
            }

            if (systemTabButton != null)
            {
                systemTabButton.onClick.AddListener(() => SwitchTab(Tab.System));
            }

            if (bestiaryTabButton != null)
            {
                bestiaryTabButton.onClick.AddListener(() => SwitchTab(Tab.Bestiary));
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            // Initialize to system tab
            SwitchTab(Tab.System);
        }

        private void Update()
        {
            // Allow escape key to toggle pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Default to system tab when opening
            SwitchTab(Tab.System);
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = _previousTimeScale;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;

            // Update panel visibility
            if (systemPanel != null)
            {
                systemPanel.SetActive(tab == Tab.System);
            }

            if (bestiaryPanel != null)
            {
                bestiaryPanel.SetActive(tab == Tab.Bestiary);
            }

            // Update tab button colors
            UpdateTabButtonColors();
        }

        private void UpdateTabButtonColors()
        {
            if (systemTabButton != null)
            {
                var colors = systemTabButton.colors;
                colors.normalColor = _currentTab == Tab.System ? activeTabColor : inactiveTabColor;
                systemTabButton.colors = colors;

                // Also update the image directly for immediate effect
                var img = systemTabButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _currentTab == Tab.System ? activeTabColor : inactiveTabColor;
                }
            }

            if (bestiaryTabButton != null)
            {
                var colors = bestiaryTabButton.colors;
                colors.normalColor = _currentTab == Tab.Bestiary ? activeTabColor : inactiveTabColor;
                bestiaryTabButton.colors = colors;

                var img = bestiaryTabButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _currentTab == Tab.Bestiary ? activeTabColor : inactiveTabColor;
                }
            }
        }

        private void ReturnToMainMenu()
        {
            // Restore time scale before loading new scene
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            // Ensure time scale is restored if destroyed while paused
            if (_isPaused)
            {
                Time.timeScale = _previousTimeScale;
            }

            // Clean up listeners
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Resume);
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }
    }
}
