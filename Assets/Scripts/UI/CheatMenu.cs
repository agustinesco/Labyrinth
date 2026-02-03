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
        [SerializeField] private Button xpButton;
        [SerializeField] private int xpAmountPerClick = 1;

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

            if (xpButton != null)
                xpButton.onClick.AddListener(AddXP);

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
            Sprite icon = CreateItemIcon(type);

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

        private Sprite CreateItemIcon(ItemType type)
        {
            var texture = new Texture2D(32, 32);
            Color color = type switch
            {
                ItemType.Speed => Color.cyan,
                ItemType.Light => Color.yellow,
                ItemType.Heal => Color.green,
                ItemType.Explosive => new Color(1f, 0.5f, 0f),
                _ => Color.white
            };

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
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
            if (xpButton != null)
                xpButton.onClick.RemoveAllListeners();
        }
    }
}
