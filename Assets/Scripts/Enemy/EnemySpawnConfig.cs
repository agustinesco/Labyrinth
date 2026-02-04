using UnityEngine;

namespace Labyrinth.Enemy
{
    /// <summary>
    /// ScriptableObject containing enemy spawn configuration.
    /// Create via Assets > Create > Labyrinth > Enemy Spawn Config
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Labyrinth/Enemy Spawn Config", order = 3)]
    public class EnemySpawnConfig : ScriptableObject
    {
        [Header("Enemy Prefabs")]
        [SerializeField, Tooltip("Prefab for Patrolling Guard enemy")]
        private GameObject patrollingGuardPrefab;

        [SerializeField, Tooltip("Prefab for Blind Mole enemy")]
        private GameObject blindMolePrefab;

        [SerializeField, Tooltip("Prefab for Shadow Stalker enemy")]
        private GameObject shadowStalkerPrefab;

        [Header("Patrolling Guards")]
        [SerializeField, Tooltip("Maximum number of patrolling guards to spawn")]
        [Min(0)] private int maxPatrollingGuards = 3;

        [SerializeField, Tooltip("Chance to spawn a guard at each valid location (0-1)")]
        [Range(0f, 1f)] private float patrollingGuardSpawnChance = 0.5f;

        [SerializeField, Tooltip("Minimum corridor length required for guard patrol")]
        [Min(5)] private int minCorridorLength = 15;

        [Header("Blind Moles")]
        [SerializeField, Tooltip("Maximum number of blind moles to spawn")]
        [Min(0)] private int maxBlindMoles = 5;

        [SerializeField, Tooltip("Chance to spawn a mole at each valid intersection (0-1)")]
        [Range(0f, 1f)] private float blindMoleSpawnChance = 0.5f;

        [Header("Shadow Stalkers")]
        [SerializeField, Tooltip("Maximum number of shadow stalkers to spawn")]
        [Min(0)] private int maxShadowStalkers = 2;

        [SerializeField, Tooltip("Chance to spawn a stalker at each valid location (0-1)")]
        [Range(0f, 1f)] private float shadowStalkerSpawnChance = 0.4f;

        [Header("Exclusion Zones")]
        [SerializeField, Tooltip("Minimum distance from start position for spawning")]
        [Min(0f)] private float startExclusionRadius = 8f;

        [SerializeField, Tooltip("Minimum distance from exit position for spawning")]
        [Min(0f)] private float exitExclusionRadius = 5f;

        // Prefab accessors
        public GameObject PatrollingGuardPrefab => patrollingGuardPrefab;
        public GameObject BlindMolePrefab => blindMolePrefab;
        public GameObject ShadowStalkerPrefab => shadowStalkerPrefab;

        // Public accessors
        public int MaxPatrollingGuards => maxPatrollingGuards;
        public float PatrollingGuardSpawnChance => patrollingGuardSpawnChance;
        public int MinCorridorLength => minCorridorLength;

        public int MaxBlindMoles => maxBlindMoles;
        public float BlindMoleSpawnChance => blindMoleSpawnChance;

        public int MaxShadowStalkers => maxShadowStalkers;
        public float ShadowStalkerSpawnChance => shadowStalkerSpawnChance;

        public float StartExclusionRadius => startExclusionRadius;
        public float ExitExclusionRadius => exitExclusionRadius;
    }
}
