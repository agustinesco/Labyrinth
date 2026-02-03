using UnityEngine;

namespace Labyrinth.Items
{
    public class SpeedItem : BaseItem
    {
        [SerializeField] private float speedBonus = 3f;
        [SerializeField] private float duration = 8f;

        public override ItemType ItemType => ItemType.Speed;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Speed, itemIcon, speedBonus, duration);
        }
    }
}