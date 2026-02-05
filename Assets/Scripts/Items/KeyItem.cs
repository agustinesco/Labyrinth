using UnityEngine;
using Labyrinth.Core;

namespace Labyrinth.Items
{
    public class KeyItem : BaseItem
    {
        public override ItemType ItemType => ItemType.Key;

        // Key is not storable - triggers win immediately
        public override bool IsStorable => false;

        public override InventoryItem CreateInventoryItem()
        {
            // Key is never stored, but we need to implement this
            return null;
        }

        protected override void OnCollected(GameObject player)
        {
            var tracker = Labyrinth.Progression.ObjectiveTracker.Instance;
            if (tracker != null)
            {
                tracker.OnKeyItemCollected();
            }
            else
            {
                // Fallback for when no progression system is active
                GameManager.Instance?.TriggerWin();
            }
        }
    }
}