using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Note: For IPointerClickHandler to work with 2D objects, you need an EventSystem in your scene and a Physics2DRaycaster on your camera.
public class CardSelector : MonoBehaviour, IPointerClickHandler
{
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

    void Start()
    {
        layoutPosition = transform.position;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
        handLayout = FindFirstObjectByType<HandLayoutAnimator>();
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

}
