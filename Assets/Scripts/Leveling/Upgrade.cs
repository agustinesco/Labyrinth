using UnityEngine;

namespace Labyrinth.Leveling
{
    public enum UpgradeType
    {
        Speed,
        Vision,
        Heal,
        WallHugger,
        ShadowBlend,
        DeepPockets
    }

    public class Upgrade
    {
        public UpgradeType Type { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public float Value { get; private set; }
        public Color CardColor { get; private set; }
        public Sprite Icon { get; private set; }

        public Upgrade(UpgradeType type, string displayName, string description, float value, Color cardColor, Sprite icon = null)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
            Value = value;
            CardColor = cardColor;
            Icon = icon;
        }
    }
}
