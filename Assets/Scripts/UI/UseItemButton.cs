using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Labyrinth.Player;

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

        [Header("Queue Slots")]
        [SerializeField] private Image queueSlot1Background;
        [SerializeField] private Image queueSlot1Icon;
        [SerializeField] private Image queueSlot2Background;
        [SerializeField] private Image queueSlot2Icon;

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
