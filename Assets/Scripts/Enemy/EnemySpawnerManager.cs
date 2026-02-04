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

            // Apply config settings to spawner
            patrollingGuardSpawner.Configure(
                spawnConfig.MaxPatrollingGuards,
                spawnConfig.PatrollingGuardSpawnChance,
                spawnConfig.MinCorridorLength,
                spawnConfig.StartExclusionRadius
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

            // Apply config settings to spawner
            blindMoleSpawner.Configure(
                spawnConfig.MaxBlindMoles,
                spawnConfig.BlindMoleSpawnChance,
                spawnConfig.StartExclusionRadius,
                spawnConfig.ExitExclusionRadius
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

            // Apply config settings to spawner
            shadowStalkerSpawner.Configure(
                spawnConfig.MaxShadowStalkers,
                spawnConfig.ShadowStalkerSpawnChance,
                spawnConfig.StartExclusionRadius,
                spawnConfig.ExitExclusionRadius
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
