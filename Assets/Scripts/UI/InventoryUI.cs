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
        [Header("Slot Sprites")]
        [SerializeField] private Sprite containerSprite;

        [Header("Colors")]
        [SerializeField] private Color emptySlotColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color filledSlotColor = Color.white;

        [Header("Uses Counter")]
        [SerializeField] private int usesCounterFontSize = 14;
        [SerializeField] private Color usesCounterColor = Color.white;
        [SerializeField] private Vector2 usesCounterOffset = new Vector2(-5f, 5f);

        private Image[] _slotBackgrounds;
        private Image[] _slotIcons;
        private Text[] _usesCounters;
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
            var counters = new System.Collections.Generic.List<Text>();

            foreach (Transform child in transform)
            {
                var bgImage = child.GetComponent<Image>();
                if (bgImage != null)
                {
                    // Set the container sprite as background
                    if (containerSprite != null)
                    {
                        bgImage.sprite = containerSprite;
                        bgImage.type = Image.Type.Simple;
                    }

                    backgrounds.Add(bgImage);

                    // Find or create icon image (child of the slot)
                    Image iconImage = null;
                    if (child.childCount > 0)
                    {
                        iconImage = child.GetChild(0).GetComponent<Image>();
                    }

                    // Create icon if it doesn't exist
                    if (iconImage == null)
                    {
                        iconImage = CreateIconImage(child);
                    }
                    else
                    {
                        // Ensure existing icon is centered
                        var iconRect = iconImage.GetComponent<RectTransform>();
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.zero;
                        iconRect.sizeDelta = new Vector2(24f, 24f);
                    }

                    icons.Add(iconImage);

                    // Create uses counter text for this slot
                    var counterText = CreateUsesCounter(child);
                    counters.Add(counterText);
                }
            }

            _slotBackgrounds = backgrounds.ToArray();
            _slotIcons = icons.ToArray();
            _usesCounters = counters.ToArray();
            _initialized = true;
        }

        private Image CreateIconImage(Transform parent)
        {
            var iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(parent, false);

            var rectTransform = iconObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(24f, 24f);

            var image = iconObj.AddComponent<Image>();
            image.preserveAspect = true;

            return image;
        }

        private Text CreateUsesCounter(Transform slotTransform)
        {
            // Create a new GameObject for the counter
            var counterObj = new GameObject("UsesCounter");
            counterObj.transform.SetParent(slotTransform, false);

            // Add RectTransform and position at bottom-right
            var rectTransform = counterObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = usesCounterOffset;
            rectTransform.sizeDelta = new Vector2(30f, 20f);

            // Add Text component
            var text = counterObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = usesCounterFontSize;
            text.color = usesCounterColor;
            text.alignment = TextAnchor.LowerRight;
            text.fontStyle = FontStyle.Bold;

            // Add outline for better visibility
            var outline = counterObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            // Start hidden
            counterObj.SetActive(false);

            return text;
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

                    // Show uses counter for multi-use items
                    if (_usesCounters != null && i < _usesCounters.Length)
                    {
                        if (item.IsMultiUse)
                        {
                            _usesCounters[i].text = item.UsesRemaining.ToString();
                            _usesCounters[i].gameObject.SetActive(true);
                        }
                        else
                        {
                            _usesCounters[i].gameObject.SetActive(false);
                        }
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

                    // Hide uses counter for empty slots
                    if (_usesCounters != null && i < _usesCounters.Length)
                    {
                        _usesCounters[i].gameObject.SetActive(false);
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
