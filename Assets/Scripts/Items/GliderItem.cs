using UnityEngine;

namespace Labyrinth.Items
{
    public class GliderItem : BaseItem
    {
        [SerializeField] private float duration = 3f;

        public override ItemType ItemType => ItemType.Glider;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Glider, itemIcon, 1f, duration);
        }
    }
}
