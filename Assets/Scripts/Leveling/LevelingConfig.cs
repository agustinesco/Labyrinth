using UnityEngine;

namespace Labyrinth.Leveling
{
    /// <summary>
    /// Configuration for the leveling system XP requirements.
    /// Supports both formula-based scaling and custom XP values per level.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelingConfig", menuName = "Labyrinth/Leveling Config", order = 5)]
    public class LevelingConfig : ScriptableObject
    {
        [Header("XP Calculation Mode")]
        [SerializeField, Tooltip("If true, uses the formula. If false, uses custom XP values array.")]
        private bool useFormula = true;

        [Header("Formula-Based XP (when useFormula = true)")]
        [SerializeField, Tooltip("Base XP required to reach level 2")]
        private int baseXP = 5;

        [SerializeField, Tooltip("How XP requirements scale with level")]
        private ScalingType scalingType = ScalingType.Linear;

        [SerializeField, Tooltip("For Linear: XP added per level. For Exponential: multiplier per level.")]
        private float scalingFactor = 5f;

        [Header("Custom XP Values (when useFormula = false)")]
        [SerializeField, Tooltip("XP required to reach each level (index 0 = level 2, index 1 = level 3, etc.)")]
        private int[] customXPPerLevel = new int[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 75 };

        [Header("Level Cap")]
        [SerializeField, Tooltip("Maximum level the player can reach (0 = no cap)")]
        private int maxLevel = 0;

        public enum ScalingType
        {
            Linear,         // baseXP + (level-1) * scalingFactor
            Exponential,    // baseXP * scalingFactor^(level-1)
            Quadratic       // baseXP + scalingFactor * (level-1)^2
        }

        /// <summary>
        /// Gets the XP required to reach the specified level.
        /// </summary>
        /// <param name="currentLevel">The player's current level.</param>
        /// <returns>XP required to reach the next level.</returns>
        public int GetXPForLevel(int currentLevel)
        {
            if (useFormula)
            {
                return CalculateXPByFormula(currentLevel);
            }
            else
            {
                return GetCustomXPForLevel(currentLevel);
            }
        }

        private int CalculateXPByFormula(int currentLevel)
        {
            switch (scalingType)
            {
                case ScalingType.Linear:
                    // Level 1->2: baseXP, Level 2->3: baseXP + scalingFactor, etc.
                    return baseXP + Mathf.RoundToInt((currentLevel - 1) * scalingFactor);

                case ScalingType.Exponential:
                    // Level 1->2: baseXP, Level 2->3: baseXP * factor, Level 3->4: baseXP * factor^2, etc.
                    return Mathf.RoundToInt(baseXP * Mathf.Pow(scalingFactor, currentLevel - 1));

                case ScalingType.Quadratic:
                    // Level 1->2: baseXP, Level 2->3: baseXP + factor*1, Level 3->4: baseXP + factor*4, etc.
                    return baseXP + Mathf.RoundToInt(scalingFactor * (currentLevel - 1) * (currentLevel - 1));

                default:
                    return baseXP;
            }
        }

        private int GetCustomXPForLevel(int currentLevel)
        {
            // currentLevel 1 needs customXPPerLevel[0] to reach level 2
            int index = currentLevel - 1;

            if (customXPPerLevel == null || customXPPerLevel.Length == 0)
            {
                Debug.LogWarning("[LevelingConfig] Custom XP array is empty, using default value of 10");
                return 10;
            }

            // If beyond the array, use the last value
            if (index >= customXPPerLevel.Length)
            {
                return customXPPerLevel[customXPPerLevel.Length - 1];
            }

            return customXPPerLevel[index];
        }

        /// <summary>
        /// Gets the maximum level (0 = no cap).
        /// </summary>
        public int MaxLevel => maxLevel;

        /// <summary>
        /// Checks if the player can level up from the current level.
        /// </summary>
        public bool CanLevelUp(int currentLevel)
        {
            if (maxLevel <= 0) return true;
            return currentLevel < maxLevel;
        }

        /// <summary>
        /// Gets a preview of XP requirements for multiple levels (useful for UI).
        /// </summary>
        public int[] GetXPPreview(int fromLevel, int count)
        {
            int[] preview = new int[count];
            for (int i = 0; i < count; i++)
            {
                preview[i] = GetXPForLevel(fromLevel + i);
            }
            return preview;
        }

#if UNITY_EDITOR
        [ContextMenu("Preview XP Requirements (Levels 1-20)")]
        private void PreviewXPRequirements()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("XP Requirements Preview:");
            sb.AppendLine("------------------------");

            int totalXP = 0;
            for (int level = 1; level <= 20; level++)
            {
                int xpNeeded = GetXPForLevel(level);
                totalXP += xpNeeded;
                sb.AppendLine($"Level {level} -> {level + 1}: {xpNeeded} XP (Total: {totalXP})");

                if (maxLevel > 0 && level >= maxLevel)
                {
                    sb.AppendLine($"[MAX LEVEL REACHED]");
                    break;
                }
            }

            Debug.Log(sb.ToString());
        }
#endif
    }
}
