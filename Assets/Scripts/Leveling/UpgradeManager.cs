using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Player;

namespace Labyrinth.Leveling
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private List<Upgrade> _upgradePool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeUpgradePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializeUpgradePool()
        {
            _upgradePool = new List<Upgrade>
            {
                new Upgrade(UpgradeType.Speed, "Swift Feet", "+1 Movement Speed", 1f, Color.cyan),
                new Upgrade(UpgradeType.Vision, "Eagle Eye", "+2 Vision Range", 2f, Color.yellow),
                new Upgrade(UpgradeType.Heal, "Restoration", "Restore 1 HP", 1f, Color.green)
            };
        }

        public List<Upgrade> GetRandomUpgrades(int count)
        {
            var result = new List<Upgrade>();
            var available = new List<Upgrade>(_upgradePool);

            // Shuffle and take 'count' upgrades
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = Random.Range(0, available.Count);
                result.Add(available[index]);
                available.RemoveAt(index);
            }

            // If we need more upgrades than unique ones, allow duplicates
            while (result.Count < count && _upgradePool.Count > 0)
            {
                int index = Random.Range(0, _upgradePool.Count);
                result.Add(_upgradePool[index]);
            }

            return result;
        }

        public void ApplyUpgrade(Upgrade upgrade)
        {
            var levelSystem = PlayerLevelSystem.Instance;
            if (levelSystem == null) return;

            switch (upgrade.Type)
            {
                case UpgradeType.Speed:
                    levelSystem.ApplyPermanentSpeedBonus(upgrade.Value);
                    break;

                case UpgradeType.Vision:
                    levelSystem.ApplyPermanentVisionBonus(upgrade.Value);
                    break;

                case UpgradeType.Heal:
                    var playerHealth = FindObjectOfType<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.Heal((int)upgrade.Value);
                    }
                    break;
            }
        }
    }
}
