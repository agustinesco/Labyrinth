using UnityEngine;

namespace Labyrinth.Items
{
    /// <summary>
    /// Represents an item stored in the player's inventory.
    /// </summary>
    public class InventoryItem
    {
        public ItemType Type { get; private set; }
        public Sprite Icon { get; private set; }
        public float EffectValue { get; private set; }
        public float Duration { get; private set; }

        public InventoryItem(ItemType type, Sprite icon, float effectValue, float duration)
        {
            Type = type;
            Icon = icon;
            EffectValue = effectValue;
            Duration = duration;
        }
    }
}
