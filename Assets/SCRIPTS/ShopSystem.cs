using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class ShopItem
{
    [Header("Shop Item Info")]
    public string itemName;
    public int itemPrice;
    public Sprite itemIcon;

    [Header("Linked GameObjects / Effects")]
    public GameObject itemObjectToActivate;
    public ParticleSystem purchaseEffectPrefab;

    [HideInInspector] public bool isPurchased = false;
}

public class ShopSystem : MonoBehaviour
{
    [Header("UI Dependencies")]
    public ColliderHighlighter highlighter;
    public UIManager uiManager;

    [Header("Player Currency")]
    public int currentCurrency = 1000;

    [Header("Shop Configuration")]
    public List<ShopItem> availableItems;
    public Transform effectSpawnPoint;

    private const string MONEY_KEY = "PlayerMoney";
    private ColliderHighlighter[] allPlots; // now automatically filled

    void Start()
    {
        // ✅ Auto-find all plots in scene
        allPlots = FindObjectsOfType<ColliderHighlighter>();

        // --- Load Player Money ---
        if (PlayerPrefs.HasKey(MONEY_KEY))
            currentCurrency = PlayerPrefs.GetInt(MONEY_KEY);
        else
        {
            PlayerPrefs.SetInt(MONEY_KEY, currentCurrency);
            PlayerPrefs.Save();
        }

        // --- Load purchased items ---
        LoadPurchasedItems();

        // --- Disable any plots that were already built ---
        DisableBuiltPlots();

        uiManager?.UpdateCurrencyDisplay();

        if (effectSpawnPoint == null)
            effectSpawnPoint = transform;
    }

    void DisableBuiltPlots()
    {
        foreach (ColliderHighlighter plot in allPlots)
        {
            string id = plot.plotID;
            if (PlayerPrefs.GetInt(id + "_Built", 0) == 1)
            {
                Collider col = plot.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                if (plot.plotRenderer != null)
                    plot.plotRenderer.material.color = Color.gray;

                Debug.Log($"Disabled built plot: {id}");
            }
        }
    }

    public bool BuyItem(string itemName)
    {
        ShopItem itemToBuy = availableItems.Find(i =>
            i.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));

        if (itemToBuy == null)
        {
            Debug.LogError($"Shop: Item '{itemName}' not found!");
            return false;
        }

        if (itemToBuy.isPurchased)
        {
            Debug.Log($"'{itemName}' already owned.");
            return true;
        }

        if (currentCurrency < itemToBuy.itemPrice)
        {
            Debug.LogWarning($"Not enough money! Need {itemToBuy.itemPrice}, have {currentCurrency}");
            highlighter?.ShowNotEnoughMoneyError();
            return false;
        }

        // --- Purchase ---
        currentCurrency -= itemToBuy.itemPrice;
        itemToBuy.isPurchased = true;

        if (itemToBuy.itemObjectToActivate != null)
        {
            itemToBuy.itemObjectToActivate.SetActive(true);

            // ✅ Save built state if it has ColliderHighlighter
            ColliderHighlighter plot = itemToBuy.itemObjectToActivate.GetComponent<ColliderHighlighter>();
            if (plot != null)
            {
                PlayerPrefs.SetInt(plot.plotID + "_Built", 1);
                PlayerPrefs.Save();
            }
        }

        PlayPurchaseEffect(itemToBuy.purchaseEffectPrefab);
        SaveMoney();
        SavePurchasedItem(itemToBuy.itemName);

        DisableBuiltPlots(); // update immediately
        uiManager?.UpdateCurrencyDisplay();

        Debug.Log($"✅ Purchased {itemName}! Remaining money: {currentCurrency}");
        return true;
    }

    public void SaveMoney()
    {
        PlayerPrefs.SetInt(MONEY_KEY, currentCurrency);
        PlayerPrefs.Save();
    }

    public void SavePurchasedItems()
    {
        foreach (var item in availableItems)
        {
            PlayerPrefs.SetInt($"ItemPurchased_{item.itemName}", item.isPurchased ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    public void LoadPurchasedItems()
    {
        foreach (var item in availableItems)
        {
            string key = GetItemKey(item.itemName);
            int value = PlayerPrefs.GetInt(key, 0);
            item.isPurchased = value == 1;

            if (item.itemObjectToActivate != null)
                item.itemObjectToActivate.SetActive(item.isPurchased);
        }
    }

    private void SavePurchasedItem(string itemName)
    {
        string key = GetItemKey(itemName);
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    private string GetItemKey(string itemName)
    {
        return $"ShopItem_{itemName}_Purchased";
    }

    private void PlayPurchaseEffect(ParticleSystem prefab)
    {
        if (prefab == null) return;

        ParticleSystem effect = Instantiate(prefab, effectSpawnPoint.position, Quaternion.identity);
        effect.Play();
        Destroy(effect.gameObject, effect.main.duration);
    }

    public int GetItemPrice(string itemName)
    {
        ShopItem item = availableItems.Find(i =>
            i.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));
        return item != null ? item.itemPrice : 0;
    }
}
