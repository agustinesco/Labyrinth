using UnityEngine;
using System.Collections.Generic;

namespace Labyrinth.Leveling
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Available Upgrades")]
        [SerializeField] private List<LevelUpUpgrade> availableUpgrades = new List<LevelUpUpgrade>();

        public IReadOnlyList<LevelUpUpgrade> AvailableUpgrades => availableUpgrades;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Get a random selection of upgrades to offer the player.
        /// </summary>
        public List<LevelUpUpgrade> GetRandomUpgrades(int count)
        {
            var result = new List<LevelUpUpgrade>();
            var available = new List<LevelUpUpgrade>(availableUpgrades);

            // Shuffle and take 'count' upgrades
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = Random.Range(0, available.Count);
                result.Add(available[index]);
                available.RemoveAt(index);
            }

            // If we need more upgrades than unique ones, allow duplicates
            while (result.Count < count && availableUpgrades.Count > 0)
            {
                int index = Random.Range(0, availableUpgrades.Count);
                result.Add(availableUpgrades[index]);
            }

            return result;
        }

        /// <summary>
        /// Apply the selected upgrade to the player.
        /// </summary>
        public void ApplyUpgrade(LevelUpUpgrade upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogWarning("UpgradeManager: Attempted to apply null upgrade");
                return;
            }

            upgrade.ApplyEffect();
        }
    }
}
