using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandLayoutAnimator : MonoBehaviour
{
    [Header("Card Pop Settings")]
    public float popYOffset = 1.5f; // How much to move up when popped
    public float popScaleMultiplier = 1.2f; // How much to scale up when popped

    private CardSelector currentlyPoppedCard = null;

    [Header("Card Layout Settings")]
    public Transform cardParent; // Parent object containing all card GameObjects
    public float spacing = 2.0f; // Space between cards in world units
    public float animationSpeed = 5.0f; // Higher = faster
    public float delayBetweenCards = 0.5f; // Delay between each card's movement start (in seconds)
    [Header("Fan Layout Settings")]
    public float fanAngle = 30f; // Total arc in degrees for the fan
    public float fanRadius = 1.5f; // Radius of the fan arc (Y offset)

    [Header("Fan Center Override")]
    public Transform fanCenterOverride; // If set, fan/linear layout will center here

    [Header("Deal Animation Settings")]
    public Transform dealFromTransform; // Where cards animate from when dealt in (e.g., PullDeck or offscreen)

    [Header("Linear Layout Settings")]
    public float zoneWidth = 8.0f; // Fallback width if no RectTransform found

    void Start()
    {
        LayoutCards();
    }

    public void LayoutCards()
    {
        StartCoroutine(AnimateCardsInSequence());
    }

    IEnumerator AnimateCardsInSequence()
    {
        // Get all cards under the parent, sort by name
        List<Transform> cards = new List<Transform>();
        for (int i = 0; i < cardParent.childCount; i++)
        {
            Transform child = cardParent.GetChild(i);
            if (child.GetComponent<CardDisplay>() != null) // Only animate real cards
                cards.Add(child);
        }
        // Sort by card number extracted from the name (e.g., Card_10_Orange)
        cards = cards.OrderBy(t => ExtractCardNumber(t.name)).ToList();

        // For UI (RectTransform), use anchoredPosition in local space
        bool isUI = cards.Count > 0 && cards[0].GetComponent<RectTransform>() != null;
        Vector2 center = Vector2.zero; // anchoredPosition of parent is usually (0,0)

        // Hybrid: use 'spacing' field as minimum, clamp to fit zone width, always centered
        RectTransform parentRect = cardParent.GetComponent<RectTransform>();
        float width = parentRect != null ? parentRect.rect.width : zoneWidth;
        int cardCount = cards.Count;
        List<Vector2> finalAnchoredPositions = new List<Vector2>();
        List<Vector3> finalWorldPositions = new List<Vector3>();
        Vector3 worldCenter = fanCenterOverride != null ? fanCenterOverride.position : transform.position;
        if (cardCount == 1) {
            finalAnchoredPositions.Add(Vector2.zero);
            finalWorldPositions.Add(worldCenter);
        } else {
            float usedSpacing = Mathf.Min(spacing, width / (cardCount - 1));
            float totalWidth = usedSpacing * (cardCount - 1);
            float xStart = -totalWidth / 2f;
            for (int i = 0; i < cardCount; i++) {
                float x = xStart + i * usedSpacing;
                finalAnchoredPositions.Add(new Vector2(x, 0));
                finalWorldPositions.Add(worldCenter + new Vector3(x, 0, 0));
            }
        }
        // PRE-PASS: Set all cards to deal-from position and hide them
        for (int i = 0; i < cards.Count; i++)
        {
            Transform card = cards[i];
            card.gameObject.SetActive(false); // Hide initially
            if (isUI)
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                Vector2 startPos = Vector2.zero;
                if (dealFromTransform != null)
                {
                    RectTransform dealFromRect = dealFromTransform.GetComponent<RectTransform>();
                    if (dealFromRect != null)
                    {
                        Vector2 worldDealFromPos = dealFromRect.TransformPoint(dealFromRect.anchoredPosition);
                        startPos = rt.parent.InverseTransformPoint(worldDealFromPos);
                    }
                    else
                    {
                        startPos = rt.parent.InverseTransformPoint(dealFromTransform.position);
                    }
                    rt.anchoredPosition = startPos;
                }
            }
            else
            {
                if (dealFromTransform != null)
                    card.position = dealFromTransform.position;
            }
        }
        // Animate each card to its final position, one by one, from dealFromTransform
        for (int i = 0; i < cards.Count; i++)
        {
            Transform card = cards[i];
            card.gameObject.SetActive(true); // Reveal card just before animating
            if (isUI)
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                Vector2 startPos = rt.anchoredPosition;
                Debug.Log($"[AnimateCardsInSequence] Animating {card.name}: start {startPos} -> target {finalAnchoredPositions[i]}");
                if (dealFromTransform != null)
                {
                    RectTransform dealFromRect = dealFromTransform.GetComponent<RectTransform>();
                    if (dealFromRect != null)
                    {
                        // Convert world position of dealFromTransform to local anchoredPosition in parent
                        Vector2 worldDealFromPos = dealFromRect.TransformPoint(dealFromRect.anchoredPosition);
                        Vector2 localDealFromPos = rt.parent.InverseTransformPoint(worldDealFromPos);
                        rt.anchoredPosition = localDealFromPos;
                    }
                    else
                    {
                        // Fallback: use world position of dealFromTransform
                        Vector2 localDealFromPos = rt.parent.InverseTransformPoint(dealFromTransform.position);
                        rt.anchoredPosition = localDealFromPos;
                    }
                }
                yield return StartCoroutine(MoveToPositionFan(card, finalAnchoredPositions[i], 0f));
                Debug.Log($"[AnimateCardsInSequence] Finished animating {card.name} to {finalAnchoredPositions[i]}");
            }
            else
            {
                Vector3 startPos = card.position;
                Debug.Log($"[AnimateCardsInSequence] Animating {card.name}: start {startPos} -> target {finalWorldPositions[i]}");
                if (dealFromTransform != null)
                {
                    card.position = dealFromTransform.position;
                }
                yield return StartCoroutine(MoveToPositionFan(card, finalWorldPositions[i], 0f));
                Debug.Log($"[AnimateCardsInSequence] Finished animating {card.name} to {finalWorldPositions[i]}");
            }
            yield return new WaitForSeconds(delayBetweenCards);
        }
        yield break;
    }

    // Overload for UI: target is Vector2 anchoredPosition
    IEnumerator MoveToPositionFan(Transform card, Vector2 targetAnchoredPos, float targetRot)
    {
        Quaternion startRot = card.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRot);
        RectTransform rt = card.GetComponent<RectTransform>();
        Vector3 targetPos = Vector3.zero; // Always defined
        if (rt != null)
        {
            Vector2 startPos = rt.anchoredPosition;
            float t = 0f;
            while (t < 1f || Quaternion.Angle(card.rotation, endRot) > 0.5f)
            {
                t += Time.deltaTime * animationSpeed;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, t);
                card.rotation = Quaternion.Lerp(card.rotation, endRot, Time.deltaTime * animationSpeed);
                yield return null;
            }
            rt.anchoredPosition = targetAnchoredPos;
            card.rotation = endRot;
        }
        else
        {
            // Fallback for sprites: use world position
            Vector3 startPos = card.position;
            float t = 0f;
            targetPos = new Vector3(targetAnchoredPos.x, targetAnchoredPos.y, card.position.z);
            while (Vector3.Distance(card.position, targetPos) > 0.01f || Quaternion.Angle(card.rotation, endRot) > 0.5f)
            {
                t += Time.deltaTime * animationSpeed;
                card.position = Vector3.Lerp(startPos, targetPos, t);
                card.rotation = Quaternion.Lerp(card.rotation, endRot, Time.deltaTime * animationSpeed);
                yield return null;
            }
            card.position = targetPos;
            card.rotation = endRot;
        }
        // Update the layout position for popping
        CardSelector selector = card.GetComponent<CardSelector>();
        if (selector != null)
        {
            if (rt != null)
            {
                selector.SetLayoutPosition(targetAnchoredPos, card.rotation);
            }
            else
            {
                selector.SetLayoutPosition(targetPos, card.rotation);
            }
        }
        // Force collider to update to new position
        var box = card.GetComponent<BoxCollider2D>();
        var sr = card.GetComponent<SpriteRenderer>();
        if (box != null && sr != null)
        {
            Vector2 size = sr.bounds.size;
            Vector3 scale = card.lossyScale;
            if (scale.x != 0 && scale.y != 0)
                size = new Vector2(size.x / scale.x, size.y / scale.y);
            box.size = size;
            box.offset = Vector2.zero;
            box.enabled = false;
            box.enabled = true;
        }
    }

    IEnumerator MoveToPosition(Transform card, Vector3 target)
    {
        while (Vector3.Distance(card.position, target) > 0.01f)
        {
            card.position = Vector3.Lerp(card.position, target, Time.deltaTime * animationSpeed);
            yield return null;
        }
        card.position = target;
        // Update the layout position for popping
        CardSelector selector = card.GetComponent<CardSelector>();
        if (selector != null)
        {
            selector.SetLayoutPosition(target, card.rotation);
        }
        // Force collider to update to new position
        var box = card.GetComponent<BoxCollider2D>();
        var sr = card.GetComponent<SpriteRenderer>();
        if (box != null && sr != null)
        {
            // Adjust collider size to match sprite bounds (in local space)
            Vector2 size = sr.bounds.size;
            Vector3 scale = card.lossyScale;
            // Convert world size to local size
            if (scale.x != 0 && scale.y != 0)
                size = new Vector2(size.x / scale.x, size.y / scale.y);
            box.size = size;
            box.offset = Vector2.zero;
            box.enabled = false;
            box.enabled = true;
        }
    }

    // Called by CardSelector when a card is clicked
    public void OnCardClicked(CardSelector card)
    {
        if (currentlyPoppedCard == card) return;
        if (currentlyPoppedCard != null)
        {
            currentlyPoppedCard.ResetPop();
        }
        currentlyPoppedCard = card;
        Vector3 popOffset = new Vector3(0, popYOffset, 0);
        card.PopOut(popOffset, popScaleMultiplier);
    }

    // Called when a card starts being dragged
    public void OnCardDragStart(CardSelector draggingCard)
    {
        if (currentlyPoppedCard != null && currentlyPoppedCard != draggingCard)
        {
            currentlyPoppedCard.ResetPop();
            currentlyPoppedCard = null;
        }
    }

    // Helper method to extract the card number from its name
    int ExtractCardNumber(string cardName)
    {
        // Assumes card names are like "Card_1_White", "Card_10_Orange", etc.
        int num = 0;
        var parts = cardName.Split('_');
        if (parts.Length > 1)
        {
            int.TryParse(parts[1], out num);
        }
        return num;
    }
}
