using UnityEngine;

namespace Labyrinth.Leveling
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Labyrinth/Level Up Upgrade", order = 1)]
    public class LevelUpUpgrade : ScriptableObject
    {
        [Header("Configuration")]
        [SerializeField, Tooltip("When false, this upgrade won't appear as an option when leveling up")]
        private bool isActive = true;

        [SerializeField, HideInInspector]
        private int assetVersion = 0;
        private const int CurrentAssetVersion = 1;

        private void OnEnable()
        {
            // Migration for assets created before isActive field was added
            if (assetVersion < CurrentAssetVersion)
            {
                isActive = true; // Default to active for legacy assets
                assetVersion = CurrentAssetVersion;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        [Header("Display")]
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color cardTint = Color.white;

        [Header("Effect")]
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private float effectValue = 1f;

        public bool IsActive => isActive;
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

                case UpgradeType.WallHugger:
                    levelSystem.ApplyWallHuggerBonus(effectValue);
                    Debug.Log($"Applied Wall Hugger upgrade: +{effectValue * 100}% speed near walls");
                    break;

                case UpgradeType.ShadowBlend:
                    levelSystem.ApplyShadowBlendUpgrade();
                    Debug.Log($"Applied Shadow Blend upgrade: Level {levelSystem.ShadowBlendLevel}");
                    break;

                case UpgradeType.DeepPockets:
                    levelSystem.ApplyDeepPocketsUpgrade();
                    Debug.Log($"Applied Deep Pockets upgrade: +1 inventory slot (total extra: {levelSystem.ExtraInventorySlots})");
                    break;
            }
        }
    }
}
