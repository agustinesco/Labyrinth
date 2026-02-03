using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Items;
using Labyrinth.Player;
using Labyrinth.Leveling;

namespace Labyrinth.UI
{
    public class CheatMenu : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button speedItemButton;
        [SerializeField] private Button lightItemButton;
        [SerializeField] private Button healItemButton;
        [SerializeField] private Button explosiveItemButton;
        [SerializeField] private Button pebblesItemButton;
        [SerializeField] private Button xpButton;
        [SerializeField] private Button noClipButton;
        [SerializeField] private int xpAmountPerClick = 1;

        [Header("Item Sprites")]
        [SerializeField] private Sprite speedItemSprite;
        [SerializeField] private Sprite lightItemSprite;
        [SerializeField] private Sprite healItemSprite;
        [SerializeField] private Sprite explosiveItemSprite;
        [SerializeField] private Sprite pebblesItemSprite;

        private bool _isOpen;

        private void Start()
        {
            // Get button from this GameObject if not set
            if (toggleButton == null)
                toggleButton = GetComponent<Button>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleMenu);

            if (speedItemButton != null)
                speedItemButton.onClick.AddListener(() => SpawnItem(ItemType.Speed));

            if (lightItemButton != null)
                lightItemButton.onClick.AddListener(() => SpawnItem(ItemType.Light));

            if (healItemButton != null)
                healItemButton.onClick.AddListener(() => SpawnItem(ItemType.Heal));

            if (explosiveItemButton != null)
                explosiveItemButton.onClick.AddListener(() => SpawnItem(ItemType.Explosive));

            if (pebblesItemButton != null)
                pebblesItemButton.onClick.AddListener(() => SpawnItem(ItemType.Pebbles));

            if (xpButton != null)
                xpButton.onClick.AddListener(AddXP);

            if (noClipButton != null)
                noClipButton.onClick.AddListener(ToggleNoClip);

            // Start with menu closed
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        private void ToggleMenu()
        {
            _isOpen = !_isOpen;
            if (menuPanel != null)
                menuPanel.SetActive(_isOpen);
        }

        private void SpawnItem(ItemType type)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || inventory.IsFull) return;

            InventoryItem item = null;
            Sprite icon = GetItemIcon(type);

            switch (type)
            {
                case ItemType.Speed:
                    item = new InventoryItem(ItemType.Speed, icon, 3f, 8f);
                    break;
                case ItemType.Light:
                    item = new InventoryItem(ItemType.Light, icon, 4f, 10f);
                    break;
                case ItemType.Heal:
                    item = new InventoryItem(ItemType.Heal, icon, 1f, 0f);
                    break;
                case ItemType.Explosive:
                    item = new InventoryItem(ItemType.Explosive, icon, 2f, 2f);
                    break;
                case ItemType.Pebbles:
                    item = new InventoryItem(ItemType.Pebbles, icon, 0f, 0f, 3); // 3 uses
                    break;
            }

            if (item != null)
            {
                inventory.TryAddItem(item);
            }
        }

        private void AddXP()
        {
            if (PlayerLevelSystem.Instance != null)
            {
                PlayerLevelSystem.Instance.AddXP(xpAmountPerClick);
            }
        }

        private void ToggleNoClip()
        {
            if (NoClipManager.Instance != null)
            {
                NoClipManager.Instance.ToggleNoClip();
                UpdateNoClipButtonText();
            }
        }

        private void UpdateNoClipButtonText()
        {
            if (noClipButton != null)
            {
                var text = noClipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    bool isActive = NoClipManager.Instance != null && NoClipManager.Instance.IsNoClipActive;
                    text.text = isActive ? "NoClip: ON" : "NoClip: OFF";
                }
            }
        }

        private Sprite GetItemIcon(ItemType type)
        {
            // Return assigned sprite if available
            return type switch
            {
                ItemType.Speed when speedItemSprite != null => speedItemSprite,
                ItemType.Light when lightItemSprite != null => lightItemSprite,
                ItemType.Heal when healItemSprite != null => healItemSprite,
                ItemType.Explosive when explosiveItemSprite != null => explosiveItemSprite,
                ItemType.Pebbles when pebblesItemSprite != null => pebblesItemSprite,
                _ => CreateFallbackIcon(type)
            };
        }

        private Sprite CreateFallbackIcon(ItemType type)
        {
            var texture = new Texture2D(16, 16);
            Color color = type switch
            {
                ItemType.Speed => Color.cyan,
                ItemType.Light => Color.yellow,
                ItemType.Heal => Color.green,
                ItemType.Explosive => new Color(1f, 0.5f, 0f),
                ItemType.Pebbles => Color.gray,
                _ => Color.white
            };

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveAllListeners();
            if (speedItemButton != null)
                speedItemButton.onClick.RemoveAllListeners();
            if (lightItemButton != null)
                lightItemButton.onClick.RemoveAllListeners();
            if (healItemButton != null)
                healItemButton.onClick.RemoveAllListeners();
            if (explosiveItemButton != null)
                explosiveItemButton.onClick.RemoveAllListeners();
            if (pebblesItemButton != null)
                pebblesItemButton.onClick.RemoveAllListeners();
            if (xpButton != null)
                xpButton.onClick.RemoveAllListeners();
            if (noClipButton != null)
                noClipButton.onClick.RemoveAllListeners();
        }
    }
}
