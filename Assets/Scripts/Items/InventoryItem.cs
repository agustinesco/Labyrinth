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

        /// <summary>
        /// Number of uses remaining for multi-use items. -1 means single use (consumed on use).
        /// </summary>
        public int UsesRemaining { get; private set; }

        /// <summary>
        /// Extra sprite for items that need it (e.g., light floor sprite for light items).
        /// </summary>
        public Sprite ExtraSprite { get; private set; }

        public InventoryItem(ItemType type, Sprite icon, float effectValue, float duration, int uses = -1, Sprite extraSprite = null)
        {
            Type = type;
            Icon = icon;
            EffectValue = effectValue;
            Duration = duration;
            UsesRemaining = uses;
            ExtraSprite = extraSprite;
        }

        /// <summary>
        /// Consumes one use of this item. Returns true if the item should be removed from inventory.
        /// </summary>
        public bool ConsumeUse()
        {
            if (UsesRemaining == -1)
            {
                // Single use item - always remove
                return true;
            }

            UsesRemaining--;
            return UsesRemaining <= 0;
        }

        /// <summary>
        /// Returns true if this is a multi-use item.
        /// </summary>
        public bool IsMultiUse => UsesRemaining > 0;
    }
}
