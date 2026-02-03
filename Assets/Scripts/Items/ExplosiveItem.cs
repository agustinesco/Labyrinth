using UnityEngine;

namespace Labyrinth.Items
{
    public class ExplosiveItem : BaseItem
    {
        [SerializeField] private int explosionRange = 2;
        [SerializeField] private int damage = 2;

        public override ItemType ItemType => ItemType.Explosive;

        public override InventoryItem CreateInventoryItem()
        {
            // EffectValue stores range, Duration stores damage
            return new InventoryItem(ItemType.Explosive, itemIcon, explosionRange, damage);
        }
    }
}
