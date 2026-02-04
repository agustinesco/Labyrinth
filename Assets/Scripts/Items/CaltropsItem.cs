using UnityEngine;

namespace Labyrinth.Items
{
    public class CaltropsItem : BaseItem
    {
        [SerializeField] private int uses = 3;
        [SerializeField] private float speedMultiplier = 0.4f;
        [SerializeField] private float slowDuration = 4f;
        [SerializeField] private int playerDamage = 1;

        public override ItemType ItemType => ItemType.Caltrops;
        public override bool IsStorable => true;

        public float SpeedMultiplier => speedMultiplier;
        public float SlowDuration => slowDuration;
        public int PlayerDamage => playerDamage;

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(
                ItemType.Caltrops,
                itemIcon,
                speedMultiplier,  // EffectValue = speed multiplier
                slowDuration,     // Duration = slow duration
                uses              // Number of uses
            );
        }
    }
}
