using UnityEngine;

namespace Labyrinth.Items
{
    public class PebblesItem : BaseItem
    {
        [SerializeField] private int uses = 3;
        [SerializeField] private Color pebbleColor = new Color(0.5f, 0.5f, 0.5f); // Grey

        public override ItemType ItemType => ItemType.Pebbles;
        public override bool IsStorable => true;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(
                ItemType.Pebbles,
                itemIcon,
                0f,     // EffectValue not used
                0f,     // Duration not used
                uses    // Number of uses
            );
        }
    }
}
