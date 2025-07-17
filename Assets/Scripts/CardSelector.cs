using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // Action area logic
    [Header("Action Area Mechanics")]
    public float actionAreaScale = 0.7f;
    public float scaleSpeed = 10f;
    private bool inActionArea = false;
    private Vector3 desiredScale;
    private Collider2D actionAreaCollider;
    private Enemy hoveredEnemy = null;


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

        GameObject actionArea = GameObject.FindGameObjectWithTag("Action");
        if (actionArea != null)
            actionAreaCollider = actionArea.GetComponent<Collider2D>();
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

    public void PopOut(Vector3 popOffset, float popScale)
    {
        if (isPopped) return;
        isPopped = true;
        // Animate to popped position and vertical rotation
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop(layoutPosition + Vector3.up * popOffset.y, Quaternion.identity, originalScale * popScale));
        // Bring to front
        transform.SetAsLastSibling();
        // Set sorting order high so this card is visually on top
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = 100; // Pop to top
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

    // --- SkillDrop area detection (for Skill cards) ---
    if (actionAreaCollider != null)
    {
        bool nowInAction = actionAreaCollider.OverlapPoint(transform.position);
        if (nowInAction != inActionArea)
        {
            inActionArea = nowInAction;
            desiredScale = inActionArea ? originalScale * actionAreaScale : originalScale;
        }
    }

    // --- Attack card targeting ---
    if (cardType == CardType.Attack)
    {
        Enemy enemyUnderPointer = null;
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                enemyUnderPointer = hit.GetComponent<Enemy>();
                break;
            }
        }
        if (hoveredEnemy != enemyUnderPointer)
        {
            if (hoveredEnemy != null) hoveredEnemy.Highlight(false);
            hoveredEnemy = enemyUnderPointer;
            if (hoveredEnemy != null) hoveredEnemy.Highlight(true);
        }
    }
    else
    {
        // Not an attack card, ensure no enemy is highlighted
        if (hoveredEnemy != null)
        {
            hoveredEnemy.Highlight(false);
            hoveredEnemy = null;
        }
    }
}



public void OnEndDrag(PointerEventData eventData)
{
    isDragging = false;
    // Restore original sorting order
    var sr = GetComponent<SpriteRenderer>();
    if (sr) sr.sortingOrder = defaultSortingOrder;

    // --- SkillDrop logic ---
    if (inActionArea && cardType == CardType.Skill)
    {
        // For now, destroy card (later: send to discard)
        Destroy(gameObject);
        return;
    }

    // --- Attack card drop logic ---
    if (cardType == CardType.Attack)
    {
        if (hoveredEnemy != null)
        {
            hoveredEnemy.TakeDamage(1); // Stub: deal 1 damage
            hoveredEnemy.Highlight(false);
            hoveredEnemy = null;
            Destroy(gameObject);
            return;
        }
        else
        {
            Debug.Log($"Attack card '{gameObject.name}' not released over enemy. Returning to hand.");
        }
    }

    // Cleanup highlight if needed
    if (hoveredEnemy != null)
    {
        hoveredEnemy.Highlight(false);
        hoveredEnemy = null;
    }

    // If the card is still popped after dragging, reset it to hand layout
    if (isPopped)
    {
        ResetPop();
    }
    else
    {
        // Return to original scale if not popped
        desiredScale = originalScale;
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
