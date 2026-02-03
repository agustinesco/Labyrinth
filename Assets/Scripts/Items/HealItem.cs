using UnityEngine;

namespace Labyrinth.Items
{
    public class HealItem : BaseItem
    {
        [SerializeField] private int healAmount = 1;

        public override ItemType ItemType => ItemType.Heal;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Heal, itemIcon, healAmount, 0f);
        }
    }
}
