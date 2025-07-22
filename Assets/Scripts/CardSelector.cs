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
    private Coroutine popCoroutine = null;
    private Quaternion originalRotation;
    [Header("Pop Animation")]
    public float popAnimationSpeed = 10f;
    private HandLayoutAnimator handLayout;
    private int defaultSortingOrder = 0;

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


    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Start()
    {
        layoutPosition = transform.position;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        handLayout = FindObjectOfType<HandLayoutAnimator>();
        desiredScale = originalScale;
        
        // Create highlight material for targets
        highlightMaterial = new Material(Shader.Find("Sprites/Default"));
        if (highlightMaterial != null)
        {
            highlightMaterial.color = Color.red;
        }
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * scaleSpeed);
    }

    // Called by HandLayoutAnimator after layout animation
    public void SetLayoutPosition(Vector3 pos, Quaternion rot)
    {
        layoutPosition = pos;
        layoutRotation = rot;
        originalScale = transform.localScale;
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
        // Only set scale, let HandLayoutAnimator handle position
        desiredScale = originalScale * popScale;
    }

    public void ResetPop()
    {
        if (!isPopped) return;
        isPopped = false;
        // Animate back to layout position and original rotation
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop(layoutPosition, layoutRotation, originalScale));
        // Reset sorting order
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = defaultSortingOrder; // Restore original order
        desiredScale = originalScale;
    }

    IEnumerator AnimatePop(Vector3 targetPos, Quaternion targetRot, Vector3 targetScale)
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;
        while (t < 1f)
        {
            t += Time.deltaTime * popAnimationSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;
        transform.localScale = targetScale;
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
        
        // If dropped on a valid target (Enemy or Friendly), remove the card
        if (highlightedTarget.CompareTag("Enemy") || highlightedTarget.CompareTag("Friendly"))
        {
            Debug.Log($"Card '{gameObject.name}' played on {highlightedTarget.name}");
            
            // Store reference to hand layout before destroying this card
            HandLayoutAnimator layoutRef = handLayout;
            
            // Destroy the card
            Destroy(gameObject);
            
            // Re-layout the remaining cards in hand using AnimatePopEffectOnly instead of LayoutCards
            // This prevents the entire hand from disappearing and being re-dealt
            if (layoutRef != null)
            {
                layoutRef.StartCoroutine(layoutRef.AnimatePopEffectOnly());
            }
            
            // Clear reference and return early
            highlightedTarget = null;
            return;
        }
        
        // Clear reference
        highlightedTarget = null;
    }

    // Card wasn't dropped on a valid target, animate it back to hand
    Debug.Log($"Card '{gameObject.name}' not played on valid target. Returning to hand.");
    
    // Always reset popped state when returning to hand
    isPopped = false;
    desiredScale = originalScale;
    
    // Animate back to layout position and original rotation
    if (popCoroutine != null) StopCoroutine(popCoroutine);
    popCoroutine = StartCoroutine(AnimatePop(layoutPosition, layoutRotation, originalScale));
    
    // Make sure the card selector knows this card is no longer popped
    if (handLayout != null && handLayout.currentlyPoppedCard == this)
    {
        handLayout.currentlyPoppedCard = null;
    }
    
    // Re-layout the hand to ensure all cards are in correct positions
    // Use AnimatePopEffectOnly instead of LayoutCards to avoid full re-deal animation
    if (handLayout != null)
    {
        handLayout.StartCoroutine(handLayout.AnimatePopEffectOnly());
    }
}

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
