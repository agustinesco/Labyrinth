using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Player;

namespace Labyrinth.UI
{
    /// <summary>
    /// Displays the player's inventory slots on the left side of the screen.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        private Image[] _slotBackgrounds;
        private Image[] _slotIcons;
        private PlayerInventory _inventory;
        private bool _initialized;

        private void Start()
        {
            InitializeSlots();
            FindInventory();
            UpdateDisplay();
        }

        private void InitializeSlots()
        {
            if (_initialized) return;

            // Find slot backgrounds (direct children with Image component)
            var backgrounds = new System.Collections.Generic.List<Image>();
            var icons = new System.Collections.Generic.List<Image>();

            foreach (Transform child in transform)
            {
                var bgImage = child.GetComponent<Image>();
                if (bgImage != null)
                {
                    backgrounds.Add(bgImage);

                    // Find icon image (child of the slot)
                    if (child.childCount > 0)
                    {
                        var iconImage = child.GetChild(0).GetComponent<Image>();
                        if (iconImage != null)
                        {
                            icons.Add(iconImage);
                        }
                    }
                }
            }

            _slotBackgrounds = backgrounds.ToArray();
            _slotIcons = icons.ToArray();
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
                    _inventory.OnInventoryChanged += UpdateDisplay;
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

        private void UpdateDisplay()
        {
            if (_slotIcons == null || _slotIcons.Length == 0)
            {
                InitializeSlots();
            }

            if (_slotIcons == null) return;

            for (int i = 0; i < _slotIcons.Length; i++)
            {
                if (_inventory != null && i < _inventory.ItemCount)
                {
                    var item = _inventory.Items[i];
                    _slotIcons[i].sprite = item.Icon;
                    _slotIcons[i].color = Color.white;
                    _slotIcons[i].enabled = true;

                    if (_slotBackgrounds != null && i < _slotBackgrounds.Length)
                    {
                        _slotBackgrounds[i].color = filledSlotColor;
                    }
                }
                else
                {
                    _slotIcons[i].sprite = null;
                    _slotIcons[i].enabled = false;

                    if (_slotBackgrounds != null && i < _slotBackgrounds.Length)
                    {
                        _slotBackgrounds[i].color = emptySlotColor;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= UpdateDisplay;
            }
        }

        public void SetInventory(PlayerInventory inventory)
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= UpdateDisplay;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += UpdateDisplay;
            }

            UpdateDisplay();
        }
    }
}
