using UnityEngine;
using Labyrinth.Leveling;

namespace Labyrinth.Items
{
    public class XPItem : BaseItem
    {
        [SerializeField] private int xpAmount = 1;

        public override ItemType ItemType => ItemType.XP;

        // XP is not storable - collected immediately
        public override bool IsStorable => false;

        public override InventoryItem CreateInventoryItem()
        {
            // XP is never stored
            return null;
        }

        protected override void OnCollected(GameObject player)
        {
            PlayerLevelSystem.Instance?.AddXP(xpAmount);
        }
    }
}
