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
        [SerializeField] private Button eagleEyeItemButton;
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
        [SerializeField] private GameObject eagleEyeItemPrefab;

        [Header("Layout Settings")]
        [SerializeField] private float buttonHeight = 30f;
        [SerializeField] private float panelHeight = 300f;

        private bool _isOpen;

        private void Awake()
        {
            SetupScrollView();
        }

        private void SetupScrollView()
        {
            if (menuPanel == null) return;

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;

            // Set panel height
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, panelHeight);

            // Clean up any existing broken scroll setup
            ScrollRect existingScrollRect = menuPanel.GetComponent<ScrollRect>();
            if (existingScrollRect != null)
            {
                // Check if it's properly configured
                if (existingScrollRect.viewport != null && existingScrollRect.content != null)
                {
                    // Already properly set up, just ensure button heights and text colors
                    SetButtonHeights(existingScrollRect.content);
                    SetButtonTextColors(existingScrollRect.content);
                    return;
                }
                // Broken setup, remove it
                Destroy(existingScrollRect);
            }

            // Remove any orphaned Viewport/Content objects
            Transform existingViewport = menuPanel.transform.Find("Viewport");
            if (existingViewport != null) Destroy(existingViewport.gameObject);
            Transform existingContent = menuPanel.transform.Find("Content");
            if (existingContent != null) Destroy(existingContent.gameObject);

            // Get the existing VerticalLayoutGroup (will be removed after setup)
            VerticalLayoutGroup existingLayout = menuPanel.GetComponent<VerticalLayoutGroup>();

            // Create Viewport
            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(menuPanel.transform, false);
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportGO.GetComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f); // Nearly transparent but needed for mask

            Mask viewportMask = viewportGO.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Create Content
            GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = contentGO.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentGO.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Collect all button children (exclude Viewport)
            System.Collections.Generic.List<Transform> buttonsToMove = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < menuPanel.transform.childCount; i++)
            {
                Transform child = menuPanel.transform.GetChild(i);
                if (child.gameObject != viewportGO && child.name != "Viewport" && child.name != "Content")
                {
                    buttonsToMove.Add(child);
                }
            }

            // Move buttons to Content and set fixed height
            foreach (Transform child in buttonsToMove)
            {
                // Set fixed height via LayoutElement
                LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = child.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = buttonHeight;
                layoutElement.minHeight = buttonHeight;

                child.SetParent(contentGO.transform, false);
            }

            // Remove old VerticalLayoutGroup from panel
            if (existingLayout != null)
            {
                Destroy(existingLayout);
            }

            // Ensure all button text is black
            SetButtonTextColors(contentRect);

            // Add ScrollRect to panel
            ScrollRect scrollRect = menuPanel.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
        }

        private void SetButtonHeights(RectTransform content)
        {
            if (content == null) return;

            foreach (Transform child in content)
            {
                LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = child.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.preferredHeight = buttonHeight;
                layoutElement.minHeight = buttonHeight;
            }
        }

        private void SetButtonTextColors(Transform parent)
        {
            if (parent == null) return;

            foreach (Transform child in parent)
            {
                var tmpText = child.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.color = Color.black;
                    continue;
                }
                var legacyText = child.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.color = Color.black;
                }
            }
        }

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

            if (eagleEyeItemButton != null)
                eagleEyeItemButton.onClick.AddListener(() => GiveItemFromPrefab(eagleEyeItemPrefab));

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
            if (eagleEyeItemButton != null)
                eagleEyeItemButton.onClick.RemoveAllListeners();
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
