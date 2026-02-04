using UnityEngine;
using System.Collections.Generic;
using System;

namespace Labyrinth.UI.Bestiary
{
    /// <summary>
    /// Singleton manager that tracks which enemies have been discovered.
    /// Discovery persists across game sessions using PlayerPrefs.
    /// </summary>
    public class BestiaryManager : MonoBehaviour
    {
        public static BestiaryManager Instance { get; private set; }

        [SerializeField] private BestiaryData bestiaryData;

        private HashSet<string> _discoveredEnemies = new HashSet<string>();
        private const string SAVE_KEY_PREFIX = "Bestiary_Discovered_";

        /// <summary>
        /// Event fired when a new enemy is discovered.
        /// </summary>
        public event Action<string> OnEnemyDiscovered;

        /// <summary>
        /// Gets the bestiary data asset.
        /// </summary>
        public BestiaryData Data => bestiaryData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadDiscoveredEnemies();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Marks an enemy as discovered.
        /// </summary>
        public void DiscoverEnemy(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;

            if (!_discoveredEnemies.Contains(enemyId))
            {
                _discoveredEnemies.Add(enemyId);
                SaveDiscoveredEnemy(enemyId);
                OnEnemyDiscovered?.Invoke(enemyId);
                Debug.Log($"[Bestiary] Discovered new enemy: {enemyId}");
            }
        }

        /// <summary>
        /// Checks if an enemy has been discovered.
        /// </summary>
        public bool IsEnemyDiscovered(string enemyId)
        {
            return _discoveredEnemies.Contains(enemyId);
        }

        /// <summary>
        /// Gets all discovered enemy IDs.
        /// </summary>
        public IReadOnlyCollection<string> GetDiscoveredEnemies()
        {
            return _discoveredEnemies;
        }

        /// <summary>
        /// Gets the total count of discoverable enemies.
        /// </summary>
        public int GetTotalEnemyCount()
        {
            return bestiaryData != null && bestiaryData.Entries != null
                ? bestiaryData.Entries.Length
                : 0;
        }

        /// <summary>
        /// Gets the count of discovered enemies.
        /// </summary>
        public int GetDiscoveredCount()
        {
            return _discoveredEnemies.Count;
        }

        private void LoadDiscoveredEnemies()
        {
            _discoveredEnemies.Clear();

            if (bestiaryData == null || bestiaryData.Entries == null) return;

            foreach (var entry in bestiaryData.Entries)
            {
                string key = SAVE_KEY_PREFIX + entry.enemyId;
                if (PlayerPrefs.GetInt(key, 0) == 1)
                {
                    _discoveredEnemies.Add(entry.enemyId);
                }
            }

            Debug.Log($"[Bestiary] Loaded {_discoveredEnemies.Count} discovered enemies");
        }

        private void SaveDiscoveredEnemy(string enemyId)
        {
            string key = SAVE_KEY_PREFIX + enemyId;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets all bestiary progress (for debugging).
        /// </summary>
        [ContextMenu("Reset Bestiary Progress")]
        public void ResetProgress()
        {
            if (bestiaryData == null || bestiaryData.Entries == null) return;

            foreach (var entry in bestiaryData.Entries)
            {
                string key = SAVE_KEY_PREFIX + entry.enemyId;
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            _discoveredEnemies.Clear();
            Debug.Log("[Bestiary] Progress reset");
        }

        /// <summary>
        /// Unlocks all bestiary entries (for debugging).
        /// </summary>
        [ContextMenu("Unlock All Entries")]
        public void UnlockAll()
        {
            if (bestiaryData == null || bestiaryData.Entries == null) return;

            foreach (var entry in bestiaryData.Entries)
            {
                DiscoverEnemy(entry.enemyId);
            }
        }
    }
}
