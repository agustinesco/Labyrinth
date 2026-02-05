using UnityEngine;

namespace Labyrinth.Items
{
    /// <summary>
    /// Item that creates a tunnel through a 1-tile thick wall.
    /// </summary>
    public class TunnelItem : BaseItem
    {
        public override ItemType ItemType => ItemType.Tunnel;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Tunnel, itemIcon, 0f, 0f);
        }
    }
}
