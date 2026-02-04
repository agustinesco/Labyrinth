using UnityEngine;

namespace Labyrinth.Items
{
    public class EchoStoneItem : BaseItem
    {
        [SerializeField] private int uses = 2;
        [SerializeField] private float revealRadius = 12f;
        [SerializeField] private float revealDuration = 3f;

        public override ItemType ItemType => ItemType.EchoStone;
        public override bool IsStorable => true;

        public float RevealRadius => revealRadius;
        public float RevealDuration => revealDuration;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(
                ItemType.EchoStone,
                itemIcon,
                revealRadius,     // EffectValue = reveal radius
                revealDuration,   // Duration = reveal duration
                uses              // Number of uses
            );
        }
    }
}
