using UnityEngine;

namespace Labyrinth.Items
{
    /// <summary>
    /// Item that creates a silk string trap between two walls.
    /// Enemies passing through get snared briefly.
    /// </summary>
    public class SilkWormItem : BaseItem
    {
        [SerializeField] private float snareDuration = 2f;
        [SerializeField] private int maxTraps = 3;
        [SerializeField] private float lifetime = 30f;

        public override ItemType ItemType => ItemType.SilkWorm;

        public override InventoryItem CreateInventoryItem()
        {
            // EffectValue = snare duration, Duration = lifetime
            // Uses = maxTraps (stored in EffectValue as secondary data via constructor)
            return new InventoryItem(ItemType.SilkWorm, itemIcon, snareDuration, lifetime);
        }

        public float SnareDuration => snareDuration;
        public int MaxTraps => maxTraps;
        public float Lifetime => lifetime;
    }
}
