using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Labyrinth.Player;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    /// <summary>
    /// Button to use the first item in the player's inventory.
    /// Also manages queue slots showing upcoming items.
    /// </summary>
    public class UseItemButton : MonoBehaviour, IPointerDownHandler
    {
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(0.3f, 0.6f, 0.3f, 0.8f);
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        [Header("Queue Slots")]
        [SerializeField] private Image queueSlot1Background;
        [SerializeField] private Image queueSlot1Icon;
        [SerializeField] private Image queueSlot2Background;
        [SerializeField] private Image queueSlot2Icon;

        [Header("Locked Slot (4th slot - unlocked by Deep Pockets)")]
        [SerializeField] private Image queueSlot3Background;
        [SerializeField] private Image queueSlot3Icon;
        [SerializeField] private GameObject queueSlot3LockOverlay;

        private Image _buttonImage;
        private Image _iconImage;
        private PlayerInventory _inventory;
        private bool _initialized;

        private void Start()
        {
            InitializeReferences();
            FindInventory();
            UpdateButtonState();
        }

        private void InitializeReferences()
        {
            if (_initialized) return;

            _buttonImage = GetComponent<Image>();

            // Find icon image (first child with Image component named ButtonIcon)
            var iconTransform = transform.Find("ButtonIcon");
            if (iconTransform != null)
            {
                _iconImage = iconTransform.GetComponent<Image>();
            }

            _initialized = true;

            // Add tap handlers to queue slots for swapping
            AddSlotTapHandler(queueSlot1Background, 1);
            AddSlotTapHandler(queueSlot2Background, 2);
            AddSlotTapHandler(queueSlot3Background, 3);
        }

        private void AddSlotTapHandler(Image slotBackground, int itemIndex)
        {
            if (slotBackground == null) return;

            var trigger = slotBackground.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener((_) => OnQueueSlotTapped(itemIndex));
            trigger.triggers.Add(entry);
        }

        private void OnQueueSlotTapped(int itemIndex)
        {
            if (_inventory != null && _inventory.ItemCount > itemIndex)
            {
                _inventory.SwapWithFirst(itemIndex);
            }
        }

        private void FindInventory()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _inventory = player.GetComponent<PlayerInventory>();
                if (_inventory != null)
                {
                    _inventory.OnInventoryChanged += UpdateButtonState;
                }
            }
        }

        private void Update()
        {
            if (_inventory == null)
            {
                FindInventory();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_inventory != null && _inventory.ItemCount > 0)
            {
                _inventory.UseFirstItem();
            }
        }

        private void UpdateButtonState()
        {
            if (!_initialized)
            {
                InitializeReferences();
            }

            bool hasItems = _inventory != null && _inventory.ItemCount > 0;

            if (_buttonImage != null)
            {
                _buttonImage.color = hasItems ? activeColor : inactiveColor;
            }

            if (_iconImage != null)
            {
                if (hasItems)
                {
                    _iconImage.sprite = _inventory.Items[0].Icon;
                    _iconImage.color = Color.white;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }

            // Update queue slots
            UpdateQueueSlot(queueSlot1Background, queueSlot1Icon, 1);
            UpdateQueueSlot(queueSlot2Background, queueSlot2Icon, 2);

            // Update locked slot (4th slot - requires Deep Pockets upgrade)
            UpdateLockedSlot();
        }

        private void UpdateQueueSlot(Image background, Image icon, int itemIndex)
        {
            if (background == null) return;

            bool hasItem = _inventory != null && _inventory.ItemCount > itemIndex;

            if (hasItem)
            {
                background.color = activeColor;
                if (icon != null)
                {
                    icon.sprite = _inventory.Items[itemIndex].Icon;
                    icon.color = Color.white;
                    icon.enabled = true;
                }
            }
            else
            {
                background.color = inactiveColor;
                if (icon != null)
                {
                    icon.enabled = false;
                }
            }
        }

        private void UpdateLockedSlot()
        {
            if (queueSlot3Background == null) return;

            // Check if the slot is unlocked (Deep Pockets upgrade grants extra inventory slots)
            bool isUnlocked = PlayerLevelSystem.Instance != null && PlayerLevelSystem.Instance.ExtraInventorySlots >= 1;

            if (isUnlocked)
            {
                // Slot is unlocked - behave like a normal queue slot
                if (queueSlot3LockOverlay != null)
                {
                    queueSlot3LockOverlay.SetActive(false);
                }

                bool hasItem = _inventory != null && _inventory.ItemCount > 3;

                if (hasItem)
                {
                    queueSlot3Background.color = activeColor;
                    if (queueSlot3Icon != null)
                    {
                        queueSlot3Icon.sprite = _inventory.Items[3].Icon;
                        queueSlot3Icon.color = Color.white;
                        queueSlot3Icon.enabled = true;
                    }
                }
                else
                {
                    queueSlot3Background.color = inactiveColor;
                    if (queueSlot3Icon != null)
                    {
                        queueSlot3Icon.enabled = false;
                    }
                }
            }
            else
            {
                // Slot is locked - show lock overlay
                queueSlot3Background.color = lockedColor;

                if (queueSlot3Icon != null)
                {
                    queueSlot3Icon.enabled = false;
                }

                if (queueSlot3LockOverlay != null)
                {
                    queueSlot3LockOverlay.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= UpdateButtonState;
            }
        }

        public void SetInventory(PlayerInventory inventory)
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= UpdateButtonState;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += UpdateButtonState;
            }

            UpdateButtonState();
        }
    }
}
