using UnityEngine;

namespace Labyrinth.Items
{
    /// <summary>
    /// Eagle Eye item that temporarily increases the player's vision range.
    /// </summary>
    public class EagleEyeItem : BaseItem
    {
        [SerializeField, Tooltip("Additional vision range while effect is active")]
        private float visionBonus = 4f;

        [SerializeField, Tooltip("Duration of the vision boost in seconds")]
        private float duration = 10f;

        public override ItemType ItemType => ItemType.EagleEye;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.EagleEye, itemIcon, visionBonus, duration);
        }
    }
}
