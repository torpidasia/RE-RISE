using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List<T>
using TMPro; // Required for TextMeshProUGUI (assuming UI uses TMPro)
using UnityEngine.UI; // Required for Button component reference
using UnityEngine.EventSystems; // NEW: Required for checking if UI is clicked
using UnityEngine.SceneManagement;

public class ColliderHighlighter : MonoBehaviour
{


    [Header("Plot Scenes List")]
    [Tooltip("Assign your plot scenes here in order: Plot_1, Plot_2, etc.")]
    public List<string> plotScenes = new List<string>();

    [Header("Plot Scene Linking")]
    [Tooltip("Name of the main scene (for returning after sub-levels).")]
    public string mainSceneName = "MainScene";

    [Tooltip("Base name for sub-scenes (e.g., 'Plot_' will load 'Plot_1', 'Plot_2', etc.).")]
    public string plotScenePrefix = "Plot_";

    [Header("Dependencies")]
    [Tooltip("The system holding the player's currency.")]
    public ShopSystem shopSystem;


    // NEW: Reference to the UIManager to update currency display texts
    [Tooltip("The main UI manager to call UpdateCurrencyDisplay on.")]
    public UIManager uiManager;

    [Header("Purchaseable Objects")]
    [Tooltip("List of all colliders that can be purchased/built upon. These are the only objects the script will interact with.")]
    public List<Collider> purchaseablePlots; // CHANGED: Now references the Collider component

    [Header("Highlight Prefab (e.g. outline, glow, etc.)")]
    public GameObject highlightPrefab;

    [Header("Highlight Positioning")]
    public float highlightYOffset = 0.05f;

    [Header("Action Button Controls")]
    [Tooltip("Reference to the RectTransform of the UI Button for 'Buy' or 'Build'.")]
    public RectTransform actionButtonTransform;

    [Tooltip("The Text component on the Action Button.")]
    public TextMeshProUGUI actionButtonText;
    [Header("Action Button Sprites")]
    [Tooltip("Sprite for the BUY state.")]
    public Sprite buySprite;

    [Tooltip("Sprite for the BUILD state.")]
    public Sprite buildSprite;

    // NEW: Reference for the dedicated 'Unselect' Button
    [Tooltip("Reference to the RectTransform of the dedicated 'Unselect' Button.")]
    public RectTransform unselectButtonTransform;

    [Tooltip("The Text component for showing 'Not Enough Money'.")]
    public TextMeshProUGUI errorMessageText;

    // The target position the button moves to when activated (Up position)
    public Vector2 buttonUpPosition = new Vector2(0, 50);
    // The starting/hidden position (Down position)
    public Vector2 buttonDownPosition = new Vector2(0, -200);
    // NEW: Separate position for the Unselect Button (e.g., further up/different position)
    public Vector2 unselectButtonUpPosition = new Vector2(120, 50);
    public Vector2 unselectButtonDownPosition = new Vector2(120, -200);


    // Private State Variables
    private GameObject currentHighlight;
    private Transform currentTarget;
    private bool isButtonVisible = false;
    private int currentCost = 0; // The cost of the currently selected collider

    private const string BUY_ACTION = "BUY";
    private const string BUILD_ACTION = "BUILD";
    public string plotID; // Example: "Plot_1", "Plot_2", etc.
    public Renderer plotRenderer;
    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        Debug.Log("=== Scenes in Build Settings ===");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            Debug.Log($"[{i}] {path}");
        }
    }
    void Start()
    {
        // Ensure dependencies are assigned
        if (shopSystem == null)
        {
            Debug.LogError("ColliderHighlighter: ShopSystem dependency is missing! Please assign it.");
        }
        if (uiManager == null)
        {
            Debug.LogError("ColliderHighlighter: UIManager dependency is missing! Please assign it.");
        }

        // --- NEW: Inject this Highlighter into the ShopSystem for error display ---
        // This assumes ShopSystem has a public variable or method to receive this reference.
        // if (shopSystem != null)
        // {
        //     shopSystem.highlighter = this; 
        // }
        // --------------------------------------------------------------------------

        // 1. Initialize Action Button (BUY/BUILD)
        if (actionButtonTransform != null)
        {
            actionButtonTransform.anchoredPosition = buttonDownPosition;
            actionButtonTransform.gameObject.SetActive(false);

            Button btn = actionButtonTransform.GetComponent<Button>();
            if (btn != null)
            {
                // Attach the purchase method to the button's OnClick event programmatically
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(AttemptPurchase);
            }
        }

        // 2. Initialize Unselect Button (FIX: Attaching DeselectObject(true) here)
        if (unselectButtonTransform != null)
        {
            unselectButtonTransform.anchoredPosition = unselectButtonDownPosition;
            unselectButtonTransform.gameObject.SetActive(false);

            Button unselectBtn = unselectButtonTransform.GetComponent<Button>();
            if (unselectBtn != null)
            {
                // Add the DeselectObject(true) call to the unselect button
                unselectBtn.onClick.RemoveAllListeners();
                // FIX: Use lambda to pass 'true' to DeselectObject
                unselectBtn.onClick.AddListener(() => DeselectObject(true));
            }
        }

        // 3. Initialize Error Message
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {

            // NEW FIX: Check if the pointer is currently over a UI GameObject (like a button).
            // If it is, we skip the world raycasting to prevent interference between UI clicks and world clicks.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // If a UI element was clicked, we exit the Update function immediately
                // to prevent the world raycast from running and re-selecting a plot.
                return;
            }

            // The logic below handles two cases:
            // 1. Clicks on a new collider (isButtonVisible = false, or currentTarget != newTarget).
            // 2. Clicks outside any collider (else block, runs DeselectObject(true)).

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Transform newTarget = hit.collider.transform;

                // CHECK: Ensure the hit object's collider is in the designated purchaseable plots list
                bool isValidPlot = purchaseablePlots != null && purchaseablePlots.Contains(hit.collider);

                if (!isValidPlot)
                {
                    // Clicked on a non-plot or a plot not in the list.
                    DeselectObject(true);
                    return;
                }
                // ✅ Play sound only for valid selectable plots
                if (uiManager != null)
                    uiManager.PlayButtonClickSound();

                // FIX: If the button IS visible AND the user clicked the SAME target again, 
                // we should block processing the click as a new selection action. 
                // This prevents the cleanup/re-init logic from running due to a UI click raycast.
                if (isButtonVisible && currentTarget == newTarget)
                {
                   
                    // User clicked the currently selected plot again while the button was up. 
                    // This is handled by the UI button click event, so we stop here.
                    return;
                }

                if (currentTarget != newTarget)
                {
                    // Clean up previous selection before proceeding
                    DeselectObject(false); // Clean up without hiding button yet

                    // --- 1. Highlight Logic ---
                    if (currentHighlight != null) Destroy(currentHighlight);

                    Vector3 highlightPosition = newTarget.position + Vector3.up * highlightYOffset;
                    currentHighlight = Instantiate(highlightPrefab, highlightPosition, Quaternion.identity);
                    currentHighlight.transform.SetParent(newTarget);
                    currentTarget = newTarget;

                    // --- 2. State Check and Button Logic ---
                    currentCost = GetCostFromCollider(newTarget);

                    // NEW CHECK: If the cost is 0, the plot is considered purchased/owned.
                    if (currentCost == 0)
                    {
                        // If BOUGHT (cost 0), always show BUILD
                        SetButtonState(0, true, true);
                    }
                    else if (shopSystem != null)
                    {
                        // If NOT BOUGHT, check affordability for BUY
                        bool isAffordable = shopSystem.currentCurrency >= currentCost;
                        SetButtonState(currentCost, isAffordable, false);
                    }
                    else
                    {
                        Debug.LogError("ShopSystem is null, cannot check currency.");
                    }
                }
            }
            else // Clicked nowhere/off-world
            {
                // This will only hide the button if it was visible
                DeselectObject(true);
            }
        }
    }



    public void MarkPlotCompleted(int plotIndex)
    {
        string key = $"Plot_{plotIndex + 1}_Success";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"✅ Saved success for {key}");
    }

    public bool IsPlotCompleted(int plotIndex)
    {
        string key = $"Plot_{plotIndex + 1}_Success";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }


    /// <summary>
    /// Attempts to read the cost from a TextMeshPro component.
    /// It looks on the target object (the plot with the collider) or anywhere in the target's children (the price text).
    /// </summary>
    private TextMeshPro GetCostTextComponent(Transform target, out int cost)
    {
        // Look on the target (plot 0) or its children (the text component).
        TextMeshPro costText = target.GetComponentInChildren<TextMeshPro>();

        if (costText != null)
        {
            if (int.TryParse(costText.text, out cost))
            {
                // Successful read
                return costText;
            }
            else
            {
                // Error 1: Component found, but text content is invalid (not a pure number)
                Debug.LogError($"Plot '{target.name}': TextMeshPro component found, but text is '{costText.text}' which is not a valid integer for purchase. It should contain only the number.");
            }
        }
        else
        {
            // Error 2: Component not found
            Debug.LogError($"Plot '{target.name}': No TextMeshPro component found on target or its children. Cannot read price or update purchase state.");
        }

        cost = 0; // Default to 0 if cost is not found or invalid
        return null;
    }

    // Helper function used by Update()
    private int GetCostFromCollider(Transform target)
    {
        // ✅ If the collider has the same name as the ShopItem
        if (shopSystem != null)
        {
            int price = shopSystem.GetItemPrice(target.name);
            if (price > 0) return price;
        }
        int cost;
        GetCostTextComponent(target, out cost);
        return cost;
    }


    /// <summary>
    /// Configures the action button and error message based on affordability and purchase state.
    /// </summary>
    private void SetButtonState(int cost, bool isAffordable, bool isPurchased)
    {
        // Hide old errors
        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);

        // Get button + its Image
        Button btn = actionButtonTransform?.GetComponent<Button>();
        Image btnImage = btn != null ? btn.GetComponent<Image>() : null;

        // Update text & sprite
        if (isPurchased)
        {
            if (actionButtonText != null)
                actionButtonText.text = BUILD_ACTION;

            if (btnImage != null && buildSprite != null)
                btnImage.sprite = buildSprite;
        }
        else
        {
            if (actionButtonText != null)
                actionButtonText.text = $"{BUY_ACTION} ({cost})";

            if (btnImage != null && buySprite != null)
                btnImage.sprite = buySprite;
        }

        // Show button & unselect button if not visible yet
        if (actionButtonTransform != null)
        {
            if (!isButtonVisible)
            {
                actionButtonTransform.gameObject.SetActive(true);
                StartCoroutine(MoveButton(actionButtonTransform, buttonUpPosition, 0.5f));

                if (unselectButtonTransform != null)
                {
                    unselectButtonTransform.gameObject.SetActive(true);
                    StartCoroutine(MoveButton(unselectButtonTransform, unselectButtonUpPosition, 0.5f));
                }
            }

            isButtonVisible = true;

            // Always allow clicks so AttemptPurchase can run
            if (btn != null)
                btn.interactable = true;
        }
    }


    // --- Purchase and Build Logic ---

    /// <summary>
    /// Called when the 'Buy' button is clicked. Attempts to deduct money and transition to 'Build'.
    /// </summary>
    public void AttemptPurchase()
    {
        uiManager.PlayButtonClickSound(); // 🔊 play click sound first
        if (shopSystem == null || currentTarget == null) return;

        // We must check if the Collider of the current target is in the list.
        Collider targetCollider = currentTarget.GetComponent<Collider>();

        // We ensure the currentTarget is a purchaseable plot before proceeding, 
        // using the list you provided.
        if (purchaseablePlots == null || targetCollider == null || !purchaseablePlots.Contains(targetCollider))
        {
            Debug.LogError($"Attempted to purchase non-purchaseable or unlisted object: {currentTarget.name}");
            DeselectObject(true);
            return;
        }

        // 1. Get the cost component and value
        int costValue;
        TextMeshPro costTextComponent = GetCostTextComponent(currentTarget, out costValue);

        // Check if item is already purchased (cost is 0), or if the component failed to load (cost is 0)
        // If costTextComponent is null, the costValue will be 0 due to the error logging above.
        if (costValue == 0)
        {
            // The item is already purchased or the component failed to load. If it's a failed load, 
            // the error message was already printed in GetCostTextComponent.

            if (actionButtonText != null && actionButtonText.text == BUILD_ACTION)
            {
                Debug.Log($"Triggering BUILD action for: {currentTarget.name}");

                // Try to determine which plot index we are building
                int plotIndex = purchaseablePlots.IndexOf(currentTarget.GetComponent<Collider>());
                if (plotIndex >= 0)
                {

                    // Save which plot is being built to PlayerPrefs so we can mark success later
                    PlayerPrefs.SetInt("CurrentPlotIndex", plotIndex);
                    PlayerPrefs.Save();

                    // ✅ Skip building again if already completed
                    if (IsPlotCompleted(plotIndex))
                    {
                        Debug.Log($"Plot {plotIndex + 1} already completed — skipping build.");
                        ShowNotEnoughMoneyError(); // or show "Already Built"
                        errorMessageText.text = "ALREADY BUILT!";
                        return;
                    }

                    // ✅ Load the correct scene from the list
                    if (plotIndex < plotScenes.Count && !string.IsNullOrEmpty(plotScenes[plotIndex]))
                    {
                        string sceneToLoad = plotScenes[plotIndex];
                        Debug.Log($"Loading plot scene: {sceneToLoad}");
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                    }
                    else
                    {
                        Debug.LogError($"No scene assigned for plot index {plotIndex}! Please set it in the Inspector.");
                    }
                }
                else
                {
                    Debug.LogError("Plot index not found in purchaseablePlots list!");
                }

                DeselectObject(true);
                return;
            }



            // If cost is 0 but we are NOT in the BUILD state, it means something went wrong 
            // (e.g., could not read the initial price, which defaults to 0). Stop here.
            return;
        }


        // 2. We are in the BUY state: Perform Affordability Check
        if (shopSystem.currentCurrency < costValue)
        {
            // Insufficient funds. Use the new public method to show the error
            ShowNotEnoughMoneyError();
            return; // Exit the function immediately
        }

        // 3. Purchase is Affordable: Proceed with purchase

        // Deduct the cost from the player's currency
        shopSystem.currentCurrency -= costValue;
        // ✅ Save to PlayerPrefs immediately after change
        shopSystem.SaveMoney();
        // Update all currency displays in the UI 
        if (uiManager != null)
        {
            uiManager.UpdateCurrencyDisplay();
        }

        // CRITICAL STEP: Set the cost text on the plot to "0" to mark it as purchased permanently
        if (costTextComponent != null)
        {
            // This confirms the GameObject whose TextMeshPro component is being modified
            Debug.Log($"Purchased! Changing text on component attached to GameObject: {costTextComponent.gameObject.name}");

            // This line changes the text on the SELECTED collider's component
            costTextComponent.text = "0";
            currentCost = 0; // Update script's local cost state too
        }

        // Use the successful cost value for accurate reporting.
        Debug.Log($"SUCCESS: Purchased Plot '{currentTarget.name}' for {costValue}. Remaining: {shopSystem.currentCurrency}");

        // 4. Update the button state to BUILD immediately
        // Pass 0 cost since it's now purchased
        SetButtonState(0, true, true);
    }


    // --- NEW PUBLIC METHOD FOR SHOP SYSTEM ---

    /// <summary>
    /// Public method to display the "NOT ENOUGH MONEY" error message.
    /// This is intended to be called by other scripts (like ShopSystem.cs).
    /// </summary>
    public void ShowNotEnoughMoneyError()
    {
        Debug.LogWarning("Purchase failed: Insufficient currency.");
        if (errorMessageText != null)
        {
            errorMessageText.text = "NOT ENOUGH MONEY";
            errorMessageText.gameObject.SetActive(true);
        }
    }

    // --- Cleanup and Animation ---

    /// <summary>
    /// Cleans up the highlight and hides the button.
    /// This is the public method that should be called by the dedicated UNSELECT button's OnClick event.
    /// </summary>
    /// <param name="hideButton">If true, the button is hidden via animation.</param>
    public void DeselectObject(bool hideButton)
    {
        uiManager.PlayButtonClickSound(); // 🔊 play click sound first
        // 1. Highlight Cleanup
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
        }
        currentTarget = null;
        // currentCost = 0; // We keep currentCost from resetting here, as it will be updated in the next Update() cycle anyway

        // 2. Hide error message
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }

        // 3. Button Deactivation Logic (Only if we are explicitly hiding the UI)
        if (hideButton && actionButtonTransform != null && isButtonVisible)
        {
            // Hide Action Button (with animation)
            StartCoroutine(MoveButton(actionButtonTransform, buttonDownPosition, 0.5f, true));

            // Hide Unselect Button (with animation)
            if (unselectButtonTransform != null)
            {
                StartCoroutine(MoveButton(unselectButtonTransform, unselectButtonDownPosition, 0.5f, true));
            }

            isButtonVisible = false;
        }
    }

    /// <summary>
    /// Coroutine to smoothly move a RectTransform to the target position.
    /// </summary>
    IEnumerator MoveButton(RectTransform rectTransform, Vector2 targetPosition, float duration, bool shouldDeactivate = false)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // Easing function for smoother movement (smooth step)
            t = t * t * (3f - 2f * t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null; // Wait until the next frame
        }

        // Ensure the button ends exactly at the target position
        rectTransform.anchoredPosition = targetPosition;

        // If this movement was meant to hide the button, deactivate the GameObject at the end
        if (shouldDeactivate)
        {
            rectTransform.gameObject.SetActive(false);
        }
    }
}
