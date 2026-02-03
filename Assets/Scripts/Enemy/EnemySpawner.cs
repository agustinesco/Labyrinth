using UnityEngine;
using Labyrinth.Core;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerTransform;

        private MazeGrid _grid;
        private Vector2 _spawnPosition;

        public void Initialize(MazeGrid grid, Vector2 startPosition, Transform player)
        {
            _grid = grid;
            _spawnPosition = startPosition;
            playerTransform = player;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn += SpawnEnemy;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemySpawn -= SpawnEnemy;
            }
        }

        private void SpawnEnemy()
        {
            var enemyObj = Instantiate(enemyPrefab, new Vector3(_spawnPosition.x, _spawnPosition.y, 0), Quaternion.identity);
            var enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Initialize(_grid, playerTransform);
            }
        }
    }
}
