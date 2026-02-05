using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Items;
using Labyrinth.Player;
using Labyrinth.Leveling;
using Labyrinth.Visibility;

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
        [SerializeField] private Button invisibilityItemButton;
        [SerializeField] private Button wispItemButton;
        [SerializeField] private Button gliderItemButton;
        [SerializeField] private Button tunnelItemButton;
        [SerializeField] private Button silkWormItemButton;
        [SerializeField] private Button xpButton;
        [SerializeField] private Button noClipButton;
        [SerializeField] private Button revealMapButton;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private int xpAmountPerClick = 1;

        [Header("Item Prefabs")]
        [SerializeField] private GameObject speedItemPrefab;
        [SerializeField] private GameObject lightItemPrefab;
        [SerializeField] private GameObject healItemPrefab;
        [SerializeField] private GameObject explosiveItemPrefab;
        [SerializeField] private GameObject pebblesItemPrefab;
        [SerializeField] private GameObject invisibilityItemPrefab;
        [SerializeField] private GameObject wispItemPrefab;
        [SerializeField] private GameObject gliderItemPrefab;
        [SerializeField] private GameObject tunnelItemPrefab;
        [SerializeField] private GameObject silkWormItemPrefab;

        private bool _isOpen;

        private void Start()
        {
            // Get button from this GameObject if not set
            if (toggleButton == null)
                toggleButton = GetComponent<Button>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleMenu);

            if (speedItemButton != null)
                speedItemButton.onClick.AddListener(() => GiveItemFromPrefab(speedItemPrefab));

            if (lightItemButton != null)
                lightItemButton.onClick.AddListener(() => GiveItemFromPrefab(lightItemPrefab));

            if (healItemButton != null)
                healItemButton.onClick.AddListener(() => GiveItemFromPrefab(healItemPrefab));

            if (explosiveItemButton != null)
                explosiveItemButton.onClick.AddListener(() => GiveItemFromPrefab(explosiveItemPrefab));

            if (pebblesItemButton != null)
                pebblesItemButton.onClick.AddListener(() => GiveItemFromPrefab(pebblesItemPrefab));

            if (invisibilityItemButton != null)
                invisibilityItemButton.onClick.AddListener(() => GiveItemFromPrefab(invisibilityItemPrefab));

            if (wispItemButton != null)
                wispItemButton.onClick.AddListener(() => GiveItemFromPrefab(wispItemPrefab));

            if (gliderItemButton != null)
                gliderItemButton.onClick.AddListener(() => GiveItemFromPrefab(gliderItemPrefab));

            if (tunnelItemButton != null)
                tunnelItemButton.onClick.AddListener(() => GiveItemFromPrefab(tunnelItemPrefab));

            if (silkWormItemButton != null)
                silkWormItemButton.onClick.AddListener(() => GiveItemFromPrefab(silkWormItemPrefab));

            if (xpButton != null)
                xpButton.onClick.AddListener(AddXP);

            if (noClipButton != null)
                noClipButton.onClick.AddListener(ToggleNoClip);

            if (revealMapButton != null)
                revealMapButton.onClick.AddListener(RevealMap);

            if (levelUpButton != null)
                levelUpButton.onClick.AddListener(ForceLevelUp);

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

        private void GiveItemFromPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("CheatMenu: Item prefab not assigned!");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || inventory.IsFull) return;

            // Instantiate prefab temporarily to get the InventoryItem
            var tempObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            tempObj.SetActive(false); // Disable to prevent any Start/Awake side effects

            var baseItem = tempObj.GetComponent<BaseItem>();
            if (baseItem != null)
            {
                var inventoryItem = baseItem.CreateInventoryItem();
                inventory.TryAddItem(inventoryItem);
            }
            else
            {
                Debug.LogWarning($"CheatMenu: Prefab {prefab.name} doesn't have a BaseItem component!");
            }

            // Clean up the temporary object
            Destroy(tempObj);
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

        private void RevealMap()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RevealEntireMap();
            }
        }

        private void ForceLevelUp()
        {
            if (PlayerLevelSystem.Instance != null)
            {
                int xpNeeded = PlayerLevelSystem.Instance.XPForNextLevel - PlayerLevelSystem.Instance.CurrentXP;
                PlayerLevelSystem.Instance.AddXP(xpNeeded);
            }
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
            if (invisibilityItemButton != null)
                invisibilityItemButton.onClick.RemoveAllListeners();
            if (wispItemButton != null)
                wispItemButton.onClick.RemoveAllListeners();
            if (gliderItemButton != null)
                gliderItemButton.onClick.RemoveAllListeners();
            if (tunnelItemButton != null)
                tunnelItemButton.onClick.RemoveAllListeners();
            if (silkWormItemButton != null)
                silkWormItemButton.onClick.RemoveAllListeners();
            if (xpButton != null)
                xpButton.onClick.RemoveAllListeners();
            if (noClipButton != null)
                noClipButton.onClick.RemoveAllListeners();
            if (revealMapButton != null)
                revealMapButton.onClick.RemoveAllListeners();
            if (levelUpButton != null)
                levelUpButton.onClick.RemoveAllListeners();
        }
    }
}
