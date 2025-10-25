using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.UI; // Required for UI elements

public class UIManager : MonoBehaviour
{
    [Header("UI Sound Settings")]
    [Tooltip("Sound to play when any UI button is clicked.")]
    public AudioClip buttonClickSound;

    [Tooltip("Audio Source to play UI sounds.")]
    public AudioSource uiAudioSource;

    [Header("Dependencies")]
    [Tooltip("Drag the GameObject with the ShopSystem.cs script attached here.")]
    public ShopSystem shopSystem;

    [Header("Shop Panel Container")]
    [Tooltip("The parent GameObject containing the entire shop UI (Scroll View, etc.).")]
    public GameObject shopPanel;
    [Header("Settings Panel Container")]
    [Tooltip("The parent GameObject containing the settings UI.")]
    public GameObject settingsPanel;

    [Header("UI Elements")]
    [Tooltip("TextMeshPro text component used to display the player's current currency.")]
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI currencyText_store;
    [Header("Dynamic Shop Generation")]
    [Tooltip("The UI Prefab used for each shop item (must contain a Button and Text components).")]
    public GameObject shopItemPrefab;

    [Tooltip("The Content Transform inside the Scroll View where items will be parented.")]
    public Transform contentParent;

    void Start()
    {
        // Initial check for dependencies
        if (shopSystem == null)
        {
            Debug.LogError("UI Manager: ShopSystem reference is missing! Please assign it in the Inspector.");
            return;
        }

        // Ensure the shop panel is closed at the start
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // 1. Update the display immediately when the game starts
        UpdateCurrencyDisplay();

        // Note: GenerateShopItems() is now called in OpenShopPanel() to ensure data is fresh when the shop opens.
    }

    /// <summary>
    /// Updates the currency display text based on the value in ShopSystem.
    /// </summary>
    public void UpdateCurrencyDisplay()
    {
        if (currencyText != null && shopSystem != null)
        {
            // Always read from PlayerPrefs to ensure latest saved value
            int money = PlayerPrefs.GetInt("PlayerMoney", shopSystem.currentCurrency);
            currencyText.text = $"{money}";
            currencyText_store.text = $"{money}";
        }
    }

    /// <summary>
    /// Opens the main shop panel and refreshes the item display. (To be linked to the main Shop Button)
    /// </summary>
    public void OpenShopPanel()
    {
        PlayButtonClickSound(); // 🔊 play click sound first
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            // Refresh the items and currency every time the shop is opened
            UpdateCurrencyDisplay();
            GenerateShopItems();
        }
    }

    /// <summary>
    /// Closes the main shop panel. (To be linked to the "X" or Cross Button)
    /// </summary>
    public void CloseShopPanel()
    {
        PlayButtonClickSound(); // 🔊 play click sound first
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the settings panel.
    /// </summary>
    public void OpenSettingsPanel()
    {
        PlayButtonClickSound(); // 🔊 play click sound first
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Closes the settings panel.
    /// </summary>
    public void CloseSettingsPanel()
    {
        PlayButtonClickSound(); // 🔊 play click sound first
        if (settingsPanel != null)
        {
            
            settingsPanel.SetActive(false);
        }
    }
    /// <summary>
    /// Loops through all items in the ShopSystem and creates a UI card/button for each in the Scroll View.
    /// </summary>
    private void GenerateShopItems()
    {
        if (shopItemPrefab == null || contentParent == null)
        {
            Debug.LogError("UI Manager: Shop Item Prefab or Content Parent is missing. Cannot generate shop items.");
            return;
        }

        // Clear any existing items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Loop through the items in the ShopSystem
        foreach (var item in shopSystem.availableItems)
        {
            GameObject itemCard = Instantiate(shopItemPrefab, contentParent);

            // --- COMPONENT REFERENCES ---
            Button button = itemCard.GetComponent<Button>();
            TextMeshProUGUI nameText = itemCard.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI priceText = itemCard.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            Image iconImage = itemCard.transform.Find("ItemIcon")?.GetComponent<Image>(); // ✅ NEW

            // --- UPDATE TEXTS ---
            if (nameText != null)
                nameText.text = item.itemName;

            if (priceText != null)
                priceText.text = item.itemPrice.ToString();

            // --- UPDATE IMAGE ---
            if (iconImage != null && item.itemIcon != null)
            {
                iconImage.sprite = item.itemIcon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false; // Hide if no icon assigned
            }

            // --- BUTTON SETUP ---
            if (button != null)
            {
                string itemName = item.itemName;
                button.onClick.AddListener(() => HandlePurchase(itemName));
                button.interactable = !item.isPurchased;
            }
            else
            {
                Debug.LogError($"[Dynamic Gen]: Item prefab for {item.itemName} is missing a Button component!");
            }
        }
    }


    /// <summary>
    /// Handles the purchase attempt when an item button is clicked. This is the only button handler required.
    /// </summary>
    /// <param name="itemName">The name of the item to purchase.</param>
    public void HandlePurchase(string itemName)
    {
        PlayButtonClickSound(); // 🔊 play click sound first
        if (shopSystem != null)
        {
            // NEW DIAGNOSTIC: This will only fire if the button click event is successfully registered AND executed.
            Debug.Log($"[Click Fired]: Attempting to buy item: {itemName}");

            // Try to buy the item using the dynamic name
            bool success = shopSystem.BuyItem(itemName);

            // If the purchase succeeded, update the currency and refresh the item list display
            if (success)
            {
                UpdateCurrencyDisplay();
                // Refresh to update button interactability (e.g., mark it as sold out/dim it)
                GenerateShopItems();
            }
        }
    }

    /// <summary>
    /// Plays a UI button click sound (like Clash of Clans style).
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
        else
        {
            Debug.LogWarning("UIManager: Missing AudioSource or ButtonClickSound reference!");
        }
    }

}
