using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Note: For IPointerClickHandler to work with 2D objects, you need an EventSystem in your scene and a Physics2DRaycaster on your camera.
public class CardSelector : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // Card type definition
    public enum CardType { Skill, Attack }
    [Header("Card Type")]
    public CardType cardType = CardType.Skill;
    private Vector3 layoutPosition;
    private Quaternion layoutRotation;
    private Vector3 originalScale;
    private bool isPopped = false;
    
    // Public properties to expose private fields for debugging
    public Vector3 OriginalScale { get { return originalScale; } }
    public bool IsPopped { get { return isPopped; } }
    
    // Method to force reset scale from outside the class
    public void ForceResetScale(Vector3 newScale)
    {
        originalScale = newScale;
        desiredScale = newScale;
        transform.localScale = newScale;
        Debug.Log($"[CardSelector] ForceResetScale called on {gameObject.name}: originalScale and desiredScale set to {newScale}");
    }
    private HandLayoutAnimator handLayout;
    private int defaultSortingOrder = 0;
    private Coroutine popCoroutine = null;
    private Quaternion originalRotation;
    
    [Header("Pop Animation")]
    public float popAnimationSpeed = 10f;

    // Card data
    private string cardTypeText;
    private int cardCost;

    // Drag-and-drop fields
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // Target highlighting and card mechanics
    [Header("Card Play Mechanics")]
    public float scaleSpeed = 10f;
    private Vector3 desiredScale;
    private GameObject highlightedTarget = null;
    private Material originalMaterial = null;
    private Material highlightMaterial = null;

    private EnergyManager energyManager;

    void Awake()
    {
        mainCamera = Camera.main;
        energyManager = FindFirstObjectByType<EnergyManager>();
        
        // CRITICAL DEBUG: Log scale in Awake
        Debug.Log($"[CardSelector] {gameObject.name} AWAKE - Scale check: transform.localScale={transform.localScale}, RectTransform scale={GetComponent<RectTransform>()?.localScale}");
    }

    void Start()
    {
        // CRITICAL DEBUG: Log scale at the very start of CardSelector initialization
        Debug.Log($"[CardSelector] {gameObject.name} START - INITIAL SCALE CHECK: transform.localScale={transform.localScale}, RectTransform scale={GetComponent<RectTransform>()?.localScale}");
        
        // CRITICAL FIX: Ensure transform scale is never zero
        if (transform.localScale == Vector3.zero)
        {
            Debug.LogWarning($"[CardSelector] {gameObject.name} START - transform.localScale is ZERO! Setting to Vector3.one");
            transform.localScale = Vector3.one;
        }
        
        // Store original scale and rotation for popping effect
        originalScale = transform.localScale;
        
        // CRITICAL FIX: Ensure originalScale is never zero
        if (originalScale == Vector3.zero || originalScale.magnitude < 0.01f)
        {
            Debug.LogWarning($"[CardSelector] {gameObject.name} START - originalScale is ZERO or very small! Setting to Vector3.one");
            originalScale = Vector3.one;
        }
        
        // Initialize desiredScale to originalScale to prevent scaling to zero
        desiredScale = originalScale;
        originalRotation = transform.rotation;
        
        // CRITICAL DEBUG: Log scale values after setting
        Debug.Log($"[CardSelector] {gameObject.name} START - AFTER SETTING: originalScale={originalScale}, desiredScale={desiredScale}");

        // Find HandLayoutAnimator in the scene that has our parent as its cardParent
        if (transform.parent != null)
        {
            // Find all HandLayoutAnimators in the scene
            HandLayoutAnimator[] layoutAnimators = FindObjectsByType<HandLayoutAnimator>(FindObjectsSortMode.None);
            foreach (var layoutAnimator in layoutAnimators)
            {
                // Check if this HandLayoutAnimator's cardParent is our parent
                if (layoutAnimator.cardParent == transform.parent)
                {
                    handLayout = layoutAnimator;
                    Debug.Log("Found HandLayoutAnimator that references our parent as cardParent.");
                    break;
                }
            }
            
            // If we still didn't find it, try to find any HandLayoutAnimator
            if (handLayout == null)
            {
                Debug.LogWarning("No HandLayoutAnimator found with our parent as cardParent! Looking for any HandLayoutAnimator.");
                handLayout = FindFirstObjectByType<HandLayoutAnimator>();
                
                if (handLayout == null)
                {
                    Debug.LogError("No HandLayoutAnimator found in scene! Card layout will not work.");
                }
                else
                {
                    Debug.LogWarning("Found HandLayoutAnimator but it doesn't reference our parent. Setting its cardParent to our parent.");
                    handLayout.cardParent = transform.parent;
                    
                    // Trigger layout after setting cardParent with a delay
                    // to ensure all cards have initialized
                    StartCoroutine(DelayedLayoutTrigger());
                }
            }
        }
        else
        {
            Debug.LogError("Card has no parent transform! Card layout will not work.");
            handLayout = FindFirstObjectByType<HandLayoutAnimator>();
        }

        // Get card data from CardDisplay
        CardDisplay cardDisplay = GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardTypeText = cardDisplay.typeText?.text;
            if (cardDisplay.costText != null && int.TryParse(cardDisplay.costText.text, out int cost))
            {
                cardCost = cost;
            }
            else
            {
                cardCost = 1; // Default cost if parsing fails
                Debug.LogWarning("Could not parse card cost, defaulting to 1");
            }
        }
        else
        {
            Debug.LogWarning("No CardDisplay component found on card!");
            cardTypeText = "";
            cardCost = 1;
        }
    }

    void Update()
    {
        // CRITICAL DEBUG: Check if scale is zero before lerping
        if (transform.localScale == Vector3.zero)
        {
            Debug.LogWarning($"[CardSelector] {gameObject.name} UPDATE - Scale is ZERO before lerp! desiredScale={desiredScale}, originalScale={originalScale}");
            // Force scale to be non-zero
            transform.localScale = Vector3.one;
        }
        
        // CRITICAL DEBUG: Check if desiredScale is zero
        if (desiredScale == Vector3.zero)
        {
            Debug.LogWarning($"[CardSelector] {gameObject.name} UPDATE - desiredScale is ZERO! Setting to originalScale={originalScale}");
            // Force desiredScale to be non-zero
            desiredScale = originalScale.magnitude > 0 ? originalScale : Vector3.one;
        }
        
        // Smoothly interpolate scale
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * scaleSpeed);
        
        // CRITICAL DEBUG: Log scale after lerp if it's zero
        if (transform.localScale.magnitude < 0.01f)
        {
            Debug.LogError($"[CardSelector] {gameObject.name} UPDATE - Scale is nearly ZERO after lerp! transform.localScale={transform.localScale}, desiredScale={desiredScale}");
            // Emergency fix - force scale to be non-zero
            transform.localScale = Vector3.one;
        }
    }

    // Called by HandLayoutAnimator after layout animation
    public void SetLayoutPosition(Vector3 pos, Quaternion rot)
    {
        layoutPosition = pos;
        layoutRotation = rot;
        
        // Only update originalScale during initial setup to prevent accumulation
        // After that, we want to preserve the true original scale
        if (originalScale == Vector3.zero)
        {
            originalScale = transform.localScale;
            desiredScale = originalScale; // Ensure desiredScale is initialized correctly
        }
        
        // Store the default sorting order
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            defaultSortingOrder = sr.sortingOrder;
    }

    void OnMouseDown()
    {
        Debug.Log("Clicked (OnMouseDown): " + gameObject.name);
        if (handLayout != null)
        {
            handLayout.OnCardClicked(this);
        }
    }

    void OnMouseOver()
    {
        Debug.Log("Mouse is over: " + gameObject.name);
    }

    // For robust click detection (works with mouse, trackpad, and touch)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked (OnPointerClick): " + gameObject.name);
        if (handLayout != null)
        {
            handLayout.OnCardClicked(this);
        }
    }

    public void PopOut(float popScale)
    {
        if (isPopped) return;
        isPopped = true;
        
        // Ensure we're using the correct original scale
        if (originalScale == Vector3.zero)
        {
            originalScale = transform.localScale;
            Debug.LogWarning($"Card {name} had zero originalScale in PopOut! Setting to current scale: {originalScale}");
        }
        
        // Set desired scale based on original scale (not current scale)
        desiredScale = originalScale * popScale;
        Debug.Log($"PopOut: {name} originalScale={originalScale}, desiredScale={desiredScale}");
    }

    public void ResetPop()
    {
        if (!isPopped) return;
        isPopped = false;
        
        // Ensure we have a valid original scale
        if (originalScale == Vector3.zero)
        {
            Debug.LogWarning($"Card {name} had zero originalScale in ResetPop! Using Vector3.one as fallback.");
            originalScale = Vector3.one;
        }
        
        // Force reset scale to original immediately to prevent scale accumulation
        transform.localScale = originalScale;
        desiredScale = originalScale;
        
        Debug.Log($"ResetPop: {name} scale reset to originalScale={originalScale}");
        
        // Animate back to layout position and original rotation
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop(layoutPosition, layoutRotation, originalScale));
        
        // Reset sorting order
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = defaultSortingOrder; // Restore original order
    }

    IEnumerator AnimatePop(Vector3 targetPos, Quaternion targetRot, Vector3 targetScale)
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;
        
        // Ensure target scale is valid
        if (targetScale == Vector3.zero)
        {
            Debug.LogError($"Card {name} AnimatePop received zero targetScale! Using originalScale instead.");
            targetScale = originalScale != Vector3.zero ? originalScale : Vector3.one;
        }
        
        Debug.Log($"AnimatePop: {name} animating from scale {startScale} to {targetScale}");
        
        while (t < 1f)
        {
            t += Time.deltaTime * popAnimationSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        // Ensure final values are exactly as specified
        transform.position = targetPos;
        transform.rotation = targetRot;
        transform.localScale = targetScale;
        
        // Make sure desiredScale matches our final scale to prevent further changes
        desiredScale = targetScale;
        
        Debug.Log($"AnimatePop: {name} animation complete, final scale={transform.localScale}");
    }
// EventSystem drag-and-drop handlers
public void OnPointerDown(PointerEventData eventData)
{
    if (!CanDrag()) return;
    dragOffset = transform.position - GetPointerWorldPosition(eventData);
}

public void OnBeginDrag(PointerEventData eventData)
{
    if (!CanDrag()) return;
    isDragging = true;
    // Notify handLayout to reset any popped card (unless it's this one)
    if (handLayout != null)
    {
        handLayout.OnCardDragStart(this);
    }
    // Bring to front visually
    var sr = GetComponent<SpriteRenderer>();
    if (sr) sr.sortingOrder = 200;
}

public void OnDrag(PointerEventData eventData)
{
    if (!isDragging) return;
    transform.position = GetPointerWorldPosition(eventData) + dragOffset;

    // Find any valid target under the card (Enemy or Friendly)
    GameObject targetUnderPointer = null;
    
    // Use EventSystem to find UI objects under pointer
    List<RaycastResult> results = new List<RaycastResult>();
    EventSystem.current.RaycastAll(eventData, results);
    
    foreach (var result in results)
    {
        if (result.gameObject != gameObject && 
            (result.gameObject.CompareTag("Enemy") || result.gameObject.CompareTag("Friendly")))
        {
            targetUnderPointer = result.gameObject;
            break;
        }
    }
    
    // Also check for 2D colliders (non-UI objects)
    if (targetUnderPointer == null)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && 
                (hit.CompareTag("Enemy") || hit.CompareTag("Friendly")))
            {
                targetUnderPointer = hit.gameObject;
                break;
            }
        }
    }
    
    // Handle target highlighting
    if (highlightedTarget != targetUnderPointer)
    {
        // Remove highlight from previous target
        if (highlightedTarget != null)
        {
            Renderer renderer = highlightedTarget.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
            }
            
            // Also check for UI Image
            UnityEngine.UI.Image image = highlightedTarget.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.white; // Reset to default color
            }
        }
        
        // Apply highlight to new target
        highlightedTarget = targetUnderPointer;
        
        if (highlightedTarget != null)
        {
            Renderer renderer = highlightedTarget.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                renderer.material = highlightMaterial;
            }
            
            // Also handle UI Image
            UnityEngine.UI.Image image = highlightedTarget.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.red; // Highlight color
            }
        }
    }
}



public void OnEndDrag(PointerEventData eventData)
{
    isDragging = false;
    // Restore original sorting order
    var sr = GetComponent<SpriteRenderer>();
    if (sr) sr.sortingOrder = defaultSortingOrder;

    // Clear any highlighting
    if (highlightedTarget != null)
    {
        // Remove highlight from target
        Renderer renderer = highlightedTarget.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
        
        // Also check for UI Image
        UnityEngine.UI.Image image = highlightedTarget.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = Color.white; // Reset to default color
        }
        
        // Check if the card type is valid for the target type
        bool isValidTargetForCardType = false;
        
        if (highlightedTarget.CompareTag("Enemy") && cardTypeText == "Attack")
        {
            isValidTargetForCardType = true;
        }
        else if (highlightedTarget.CompareTag("Friendly") && cardTypeText == "Skill")
        {
            isValidTargetForCardType = true;
        }
        
        // If dropped on a valid target (Enemy for Attack cards or Friendly for Skill cards)
        if (isValidTargetForCardType)
        {
            Debug.Log($"Card '{gameObject.name}' ({cardTypeText}) played on {highlightedTarget.name} with cost {cardCost}");
            
            // Check if we have enough energy
            if (EnergyManager.Instance != null && EnergyManager.Instance.CanSpendEnergy(cardCost))
            {
                // Spend energy
                EnergyManager.Instance.SpendEnergy(cardCost);
                
                // Store reference to hand layout and transform before destroying this card
                HandLayoutAnimator layoutRef = handLayout;
                Transform cardTransform = transform;
                
                // Tell the hand layout to remove this card and re-layout the remaining cards
                // This must be done BEFORE destroying the card
                if (layoutRef != null)
                {
                    layoutRef.RemoveCardAndReLayout(cardTransform);
                }
                
                // Destroy the card
                Destroy(gameObject);
                
                // Clear reference and return early
                highlightedTarget = null;
                return;
            }
            else
            {
                Debug.Log($"Not enough energy to play card! Required: {cardCost}");
                // Not enough energy, return card to hand
            }
        }
        else if (highlightedTarget.CompareTag("Enemy") || highlightedTarget.CompareTag("Friendly"))
        {
            Debug.Log($"Invalid target type for card type {cardTypeText}. Cannot play {cardTypeText} on {highlightedTarget.tag}");
            // Invalid target type for card type, return card to hand
        }
        
        // Clear reference
        highlightedTarget = null;
    }

    // Card wasn't dropped on a valid target, animate it back to hand
    Debug.Log($"Card '{gameObject.name}' not played on valid target. Returning to hand.");
    
    // Always reset popped state when returning to hand
    isPopped = false;
    desiredScale = originalScale;
    
    // Make sure the card selector knows this card is no longer popped
    if (handLayout != null)
    {
        // If this was the popped card, clear it
        if (handLayout.currentlyPoppedCard == this)
        {
            handLayout.currentlyPoppedCard = null;
        }
        
        // Explicitly call ResetPop to ensure proper reset
        ResetPop();
    }
    
    // Ensure the transform is reset to normal scale immediately
    transform.localScale = originalScale;
    
    // Animate back to layout position and original rotation
    if (popCoroutine != null) StopCoroutine(popCoroutine);
    popCoroutine = StartCoroutine(AnimatePop(layoutPosition, layoutRotation, originalScale));
    
    // Re-layout the hand to ensure all cards are in correct positions
    // Use AnimatePopEffectOnly instead of LayoutCards to avoid full re-deal animation
    if (handLayout != null)
    {
        // Force a pop effect animation to ensure all cards are properly positioned
        handLayout.StartCoroutine(handLayout.AnimatePopEffectOnly());
    }
}

private IEnumerator DelayedLayoutTrigger()
{
    // Wait for end of frame to ensure all cards are initialized
    yield return new WaitForEndOfFrame();
    
    // Wait a bit more to be safe
    yield return new WaitForSeconds(0.1f);
    
    Debug.Log($"Card {gameObject.name}: Triggering delayed layout");
    if (handLayout != null)
    {
        // IMPORTANT: Do not trigger any layout or animation here
        // This was causing cards to disappear by triggering a new animation
        // before the previous one completed
        Debug.Log($"Card {gameObject.name}: DelayedLayoutTrigger - NOT triggering any new animations to prevent disappearance");
        
        // Instead, just ensure this card is visible and has the correct scale
        gameObject.SetActive(true);
        transform.localScale = originalScale;
    }
}

// Helper method to get the world position of the pointer
Vector3 GetPointerWorldPosition(PointerEventData eventData)
{
    Camera cam = mainCamera != null ? mainCamera : Camera.main;
    Vector3 screenPos = eventData.position;
    screenPos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
    return cam.ScreenToWorldPoint(screenPos);
}

bool CanDrag()
{
    // Allow drag as long as not already dragging
    return !isDragging;
}
}
