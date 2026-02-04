using UnityEngine;
using Labyrinth.Maze;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// Manages enemy spawning using a configurable ScriptableObject.
    /// Coordinates PatrollingGuardSpawner and BlindMoleSpawner.
    /// </summary>
    public class EnemySpawnerManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField, Tooltip("Enemy spawn configuration asset")]
        private EnemySpawnConfig spawnConfig;

        [Header("Container")]
        [SerializeField, Tooltip("Parent transform for spawned enemies (optional)")]
        private Transform enemyContainer;

        [Header("Spawners")]
        [SerializeField] private PatrollingGuardSpawner patrollingGuardSpawner;
        [SerializeField] private BlindMoleSpawner blindMoleSpawner;
        [SerializeField] private ShadowStalkerSpawner shadowStalkerSpawner;

        /// <summary>
        /// Gets or sets the spawn configuration.
        /// </summary>
        public EnemySpawnConfig SpawnConfig
        {
            get => spawnConfig;
            set => spawnConfig = value;
        }

        /// <summary>
        /// Spawns all enemies based on the current configuration.
        /// </summary>
        public void SpawnEnemies(MazeGrid grid, Vector2 startPos, Vector2 exitPos, Transform player = null)
        {
            if (spawnConfig == null)
            {
                Debug.LogWarning("[EnemySpawnerManager] No EnemySpawnConfig assigned!");
                return;
            }

            // Clear existing enemies first
            ClearAllEnemies();

            // Set container on spawners
            if (patrollingGuardSpawner != null) patrollingGuardSpawner.SetContainer(enemyContainer);
            if (blindMoleSpawner != null) blindMoleSpawner.SetContainer(enemyContainer);
            if (shadowStalkerSpawner != null) shadowStalkerSpawner.SetContainer(enemyContainer);

            // Apply config to spawners and spawn
            SpawnPatrollingGuards(grid, startPos, exitPos, player);
            SpawnBlindMoles(grid);
            SpawnShadowStalkers(grid, player);

            Debug.Log($"[EnemySpawnerManager] Spawned enemies - Guards: {spawnConfig.MaxPatrollingGuards} max, Moles: {spawnConfig.MaxBlindMoles} max, Stalkers: {spawnConfig.MaxShadowStalkers} max");
        }

        private void SpawnPatrollingGuards(MazeGrid grid, Vector2 startPos, Vector2 exitPos, Transform player)
        {
            if (patrollingGuardSpawner == null)
            {
                Debug.LogWarning("[EnemySpawnerManager] No PatrollingGuardSpawner assigned!");
                return;
            }

            // Apply config settings to spawner (including prefab)
            patrollingGuardSpawner.Configure(
                spawnConfig.MaxPatrollingGuards,
                spawnConfig.PatrollingGuardSpawnChance,
                spawnConfig.MinCorridorLength,
                spawnConfig.StartExclusionRadius,
                spawnConfig.PatrollingGuardPrefab
            );

            patrollingGuardSpawner.SpawnGuards(grid, startPos, exitPos, player);
        }

        private void SpawnBlindMoles(MazeGrid grid)
        {
            if (blindMoleSpawner == null)
            {
                Debug.LogWarning("[EnemySpawnerManager] No BlindMoleSpawner assigned!");
                return;
            }

            // Apply config settings to spawner (including prefab)
            blindMoleSpawner.Configure(
                spawnConfig.MaxBlindMoles,
                spawnConfig.BlindMoleSpawnChance,
                spawnConfig.StartExclusionRadius,
                spawnConfig.ExitExclusionRadius,
                spawnConfig.BlindMolePrefab
            );

            blindMoleSpawner.SpawnMoles(grid);
        }

        private void SpawnShadowStalkers(MazeGrid grid, Transform player)
        {
            if (shadowStalkerSpawner == null)
            {
                Debug.LogWarning("[EnemySpawnerManager] No ShadowStalkerSpawner assigned!");
                return;
            }

            // Apply config settings to spawner (including prefab)
            shadowStalkerSpawner.Configure(
                spawnConfig.MaxShadowStalkers,
                spawnConfig.ShadowStalkerSpawnChance,
                spawnConfig.StartExclusionRadius,
                spawnConfig.ExitExclusionRadius,
                spawnConfig.ShadowStalkerPrefab
            );

            shadowStalkerSpawner.SpawnStalkers(grid, player);
        }

        /// <summary>
        /// Clears all spawned enemies.
        /// </summary>
        public void ClearAllEnemies()
        {
            if (patrollingGuardSpawner != null)
            {
                patrollingGuardSpawner.ClearGuards();
            }

            if (blindMoleSpawner != null)
            {
                blindMoleSpawner.ClearMoles();
            }

            if (shadowStalkerSpawner != null)
            {
                shadowStalkerSpawner.ClearStalkers();
            }
        }

        /// <summary>
        /// Regenerates all enemies with current configuration.
        /// </summary>
        public void RegenerateEnemies(MazeGrid grid, Vector2 startPos, Vector2 exitPos, Transform player = null)
        {
            SpawnEnemies(grid, startPos, exitPos, player);
        }
    }
}
