using UnityEngine;

namespace Labyrinth.Items
{
    public class WispItem : BaseItem
    {
        public override ItemType ItemType => ItemType.Wisp;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Wisp, itemIcon, 0f, 0f);
        }
    }
}
