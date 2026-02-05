using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Labyrinth.Progression;

namespace Labyrinth.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float enemySpawnDelay = 45f;

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public float EnemySpawnTimer { get; private set; }
        public bool EnemySpawned { get; private set; }

        public event System.Action OnEnemySpawn;
        public event System.Action OnGameWin;
        public event System.Action OnGameLose;
        public event System.Action OnLevelEscape;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnemySpawnTimer = enemySpawnDelay;
            CurrentState = GameState.Playing;
            EnemySpawned = false;
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            if (!EnemySpawned)
            {
                EnemySpawnTimer -= Time.deltaTime;
                if (EnemySpawnTimer <= 0)
                {
                    SpawnEnemy();
                }
            }
        }

        private void SpawnEnemy()
        {
            EnemySpawned = true;
            OnEnemySpawn?.Invoke();
        }

        public void TriggerWin()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Won;
            OnGameWin?.Invoke();
            StartCoroutine(ReturnToLevelSelectionAfterDelay(1.5f));
        }

        public void TriggerEscape()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Won;
            OnLevelEscape?.Invoke();
            StartCoroutine(ReturnToLevelSelectionAfterDelay(1.5f));
        }

        private IEnumerator ReturnToLevelSelectionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToLevelSelection();
        }

        public void ReturnToLevelSelection()
        {
            LevelProgressionManager.Instance?.ReturnToLevelSelection();
        }

        private IEnumerator ReturnToMainMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadMainMenu();
        }

        public void TriggerLose()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Lost;
            OnGameLose?.Invoke();
            StartCoroutine(ReturnToMainMenuAfterDelay(1.5f));
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public enum GameState
    {
        Playing,
        Won,
        Lost
    }
}
