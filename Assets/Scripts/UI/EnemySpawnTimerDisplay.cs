using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Core;

namespace Labyrinth.UI
{
    /// <summary>
    /// Displays a countdown timer showing how much time remains until the enemy spawns.
    /// </summary>
    public class EnemySpawnTimerDisplay : MonoBehaviour
    {
        [SerializeField] private Text timerText;
        [SerializeField] private string timerPrefix = "Enemy in: ";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 1f); // Orange
        [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private float warningThreshold = 15f;
        [SerializeField] private float dangerThreshold = 5f;

        private void Update()
        {
            if (GameManager.Instance == null || timerText == null) return;

            if (GameManager.Instance.EnemySpawned)
            {
                // Hide or show "ENEMY ACTIVE" when spawned
                timerText.text = "ENEMY ACTIVE!";
                timerText.color = dangerColor;
                return;
            }

            float timeRemaining = GameManager.Instance.EnemySpawnTimer;

            // Format as MM:SS
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = $"{timerPrefix}{minutes:00}:{seconds:00}";

            // Update color based on time remaining
            if (timeRemaining <= dangerThreshold)
            {
                timerText.color = dangerColor;
            }
            else if (timeRemaining <= warningThreshold)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }
    }
}
