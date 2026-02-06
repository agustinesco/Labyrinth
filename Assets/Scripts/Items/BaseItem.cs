using UnityEngine;
using Labyrinth.Player;
using Labyrinth.Progression;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(VisibilityAwareEntity))]
    public abstract class BaseItem : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.1f;

        [Header("Sprites")]
        [SerializeField] protected Sprite itemIcon;
        [SerializeField, Tooltip("Sprite shown in the world (e.g., chest). If null, uses SpriteRenderer's current sprite.")]
        protected Sprite worldSprite;

        private Vector3 _startPosition;
        private float _bobOffset;

        /// <summary>
        /// The type of this item for inventory purposes.
        /// </summary>
        public abstract ItemType ItemType { get; }

        /// <summary>
        /// Whether this item should be stored in inventory (true) or used immediately (false).
        /// </summary>
        public virtual bool IsStorable => true;

        /// <summary>
        /// Creates an InventoryItem representation of this item.
        /// </summary>
        public abstract InventoryItem CreateInventoryItem();

        /// <summary>
        /// Sets the item icon for inventory display. Call this before Start() for dynamically created items.
        /// </summary>
        public void SetItemIcon(Sprite icon)
        {
            itemIcon = icon;
        }

        protected virtual void Start()
        {
            _startPosition = transform.position;
            GetComponent<Collider2D>().isTrigger = true;

            var sr = GetComponent<SpriteRenderer>();

            // Apply world sprite if set
            if (worldSprite != null && sr != null)
            {
                sr.sprite = worldSprite;
            }

            // Auto-assign icon from SpriteRenderer if not set
            if (itemIcon == null && sr != null)
            {
                itemIcon = sr.sprite;
            }
        }

        protected virtual void Update()
        {
            if (!enableBobbing) return;

            // Bobbing animation
            _bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = _startPosition + Vector3.up * _bobOffset;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if (IsStorable)
                {
                    // Try to add to inventory
                    var inventory = other.GetComponent<PlayerInventory>();
                    if (inventory != null && !inventory.IsFull)
                    {
                        var invItem = CreateInventoryItem();
                        if (inventory.TryAddItem(invItem))
                        {
                            ObjectiveTracker.Instance?.OnItemCollected(ItemType);
                            Destroy(gameObject);
                        }
                    }
                    // If inventory full, don't pick up - item stays in world
                }
                else
                {
                    // Use immediately (like Key)
                    OnCollected(other.gameObject);
                    ObjectiveTracker.Instance?.OnItemCollected(ItemType);
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Called when the item is collected and used immediately (non-storable items).
        /// </summary>
        protected virtual void OnCollected(GameObject player) { }
    }
}