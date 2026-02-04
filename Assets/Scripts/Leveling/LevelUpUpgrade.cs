using UnityEngine;

namespace Labyrinth.Leveling
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Labyrinth/Level Up Upgrade", order = 1)]
    public class LevelUpUpgrade : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color cardTint = Color.white;

        [Header("Effect")]
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private float effectValue = 1f;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color CardTint => cardTint;
        public UpgradeType UpgradeType => upgradeType;
        public float EffectValue => effectValue;

        /// <summary>
        /// Apply this upgrade's effect to the player.
        /// </summary>
        public void ApplyEffect()
        {
            var levelSystem = PlayerLevelSystem.Instance;
            if (levelSystem == null)
            {
                Debug.LogWarning($"LevelUpUpgrade: Cannot apply {displayName} - PlayerLevelSystem not found");
                return;
            }

            switch (upgradeType)
            {
                case UpgradeType.Speed:
                    levelSystem.ApplyPermanentSpeedBonus(effectValue);
                    Debug.Log($"Applied Speed upgrade: +{effectValue}");
                    break;

                case UpgradeType.Vision:
                    levelSystem.ApplyPermanentVisionBonus(effectValue);
                    Debug.Log($"Applied Vision upgrade: +{effectValue}");
                    break;

                case UpgradeType.Heal:
                    var playerHealth = Object.FindFirstObjectByType<Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.Heal((int)effectValue);
                        Debug.Log($"Applied Heal upgrade: +{effectValue} HP");
                    }
                    break;
            }
        }
    }
}
