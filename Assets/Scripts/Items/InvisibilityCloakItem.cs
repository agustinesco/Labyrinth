using UnityEngine;

namespace Labyrinth.Items
{
    public class InvisibilityCloakItem : BaseItem
    {
        [SerializeField] private float invisibilityDuration = 5f;

        public override ItemType ItemType => ItemType.Invisibility;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(ItemType.Invisibility, itemIcon, 0f, invisibilityDuration);
        }
    }
}
