using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandLayoutAnimator : MonoBehaviour
{
    [Header("Card Pop Settings")]
    public float popYOffset = 1.5f; // How much to move up when popped
    public float popScaleMultiplier = 1.2f; // How much to scale up when popped

    public CardSelector currentlyPoppedCard = null;

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
        // CRITICAL FIX: Check dealFromTransform for RectTransform
        if (dealFromTransform != null)
        {
            Debug.Log($"[HandLayoutAnimator] Start: dealFromTransform ({dealFromTransform.name}) exists");
            
            // If PullDeck doesn't have a RectTransform, add one
            if (dealFromTransform.GetComponent<RectTransform>() == null)
            {
                Debug.LogWarning($"[HandLayoutAnimator] Start: Adding RectTransform to {dealFromTransform.name}");
                dealFromTransform.gameObject.AddComponent<RectTransform>();
            }
        }
        
        if (cardParent != null)
        {
            Debug.Log("HandLayoutAnimator Start: cardParent is set, laying out cards");
            // Add a small delay before initial layout to ensure all cards are properly initialized
            StartCoroutine(DelayedInitialLayout());
        }
        else
        {
            Debug.LogWarning("HandLayoutAnimator Start: cardParent is null, cannot layout cards");
        }
    }
    
    // Delay initial layout to ensure all cards are properly initialized
    private IEnumerator DelayedInitialLayout()
    {
        yield return new WaitForSeconds(0.5f); // Increased delay to ensure all cards are initialized
        
        // CRITICAL FIX: Ensure the original Card prefab is hidden
        GameObject originalCard = GameObject.Find("Card");
        if (originalCard != null && originalCard.name == "Card" && !originalCard.name.Contains("Clone"))
        {
            Debug.Log("[HandLayoutAnimator] Found original Card prefab - ensuring it stays hidden");
            originalCard.SetActive(false);
            
            // Also hide its parent if it has one
            if (originalCard.transform.parent != null)
            {
                Debug.Log($"[HandLayoutAnimator] Also hiding Card's parent: {originalCard.transform.parent.gameObject.name}");
                originalCard.transform.parent.gameObject.SetActive(false);
            }
        }
        
        // CRITICAL DEBUG: Check dealFromTransform for RectTransform
        if (dealFromTransform != null)
        {
            Debug.Log($"[HandLayoutAnimator] dealFromTransform ({dealFromTransform.name}) has RectTransform: {dealFromTransform.GetComponent<RectTransform>() != null}");
        }
        
        // CRITICAL DEBUG: Log all card scales BEFORE layout
        Debug.Log("[HandLayoutAnimator] CHECKING ALL CARD SCALES BEFORE LAYOUT");
        if (cardParent != null)
        {
            for (int i = 0; i < cardParent.childCount; i++)
            {
                Transform child = cardParent.GetChild(i);
                
                // Skip the original Card prefab
                if (child.name == "Card" && !child.name.Contains("Clone"))
                {
                    Debug.Log($"[HandLayoutAnimator] Skipping original Card prefab in layout");
                    child.gameObject.SetActive(false);
                    continue;
                }
                
                RectTransform rt = child.GetComponent<RectTransform>();
                Debug.Log($"[HandLayoutAnimator] Card {child.name} BEFORE LAYOUT: transform.scale={child.localScale}, RectTransform.scale={rt?.localScale}");
            }
        }
        
        LayoutCards();
        
        // CRITICAL DEBUG: Log all card scales AFTER layout
        yield return new WaitForSeconds(0.1f); // Wait a bit for layout to complete
        Debug.Log("[HandLayoutAnimator] CHECKING ALL CARD SCALES AFTER LAYOUT");
        if (cardParent != null)
        {
            for (int i = 0; i < cardParent.childCount; i++)
            {
                Transform child = cardParent.GetChild(i);
                
                // Skip the original Card prefab
                if (child.name == "Card" && !child.name.Contains("Clone"))
                {
                    Debug.Log($"[HandLayoutAnimator] Keeping original Card prefab hidden after layout");
                    child.gameObject.SetActive(false);
                    continue;
                }
                
                RectTransform rt = child.GetComponent<RectTransform>();
                Debug.Log($"[HandLayoutAnimator] Card {child.name} AFTER LAYOUT: transform.scale={child.localScale}, RectTransform.scale={rt?.localScale}");
            }
        }
    }

    public void LayoutCards()
    {
        StartCoroutine(AnimateCardsInSequence());
    }

    IEnumerator AnimateCardsInSequence()
    {
        // Safety check for cardParent
        if (cardParent == null)
        {
            Debug.LogError("AnimateCardsInSequence: cardParent is null! Cannot layout cards.");
            yield break;
        }
        
        // CRITICAL FIX: Ensure the original Card prefab is hidden
        GameObject originalCard = GameObject.Find("Card");
        if (originalCard != null && originalCard.name == "Card" && !originalCard.name.Contains("Clone"))
        {
            Debug.Log("[HandLayoutAnimator] AnimateCardsInSequence: Found original Card prefab - ensuring it stays hidden");
            originalCard.SetActive(false);
            
            // Also hide its parent if it has one
            if (originalCard.transform.parent != null)
            {
                Debug.Log($"[HandLayoutAnimator] AnimateCardsInSequence: Also hiding Card's parent: {originalCard.transform.parent.gameObject.name}");
                originalCard.transform.parent.gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"AnimateCardsInSequence: Starting layout with {cardParent.childCount} children in cardParent");
        
        // Get all cards under the parent, sort by name
        List<Transform> cards = new List<Transform>();
        for (int i = 0; i < cardParent.childCount; i++)
        {
            Transform child = cardParent.GetChild(i);
            if (child != null && child.GetComponent<CardDisplay>() != null) // Only animate real cards
            {
                cards.Add(child);
                Debug.Log($"AnimateCardsInSequence: Added card {child.name} to layout list");
            }
        }
        
        if (cards.Count == 0)
        {
            Debug.LogWarning("AnimateCardsInSequence: No cards found to layout!");
            yield break;
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
        
        // Get card width for better spacing calculation
        float cardWidth = 200f; // Default card width
        if (cards.Count > 0 && cards[0].GetComponent<RectTransform>() != null)
        {
            cardWidth = cards[0].GetComponent<RectTransform>().rect.width;
            Debug.Log($"Using actual card width: {cardWidth}");
        }
        
        if (cardCount == 1) {
            // Single card is centered
            finalAnchoredPositions.Add(Vector2.zero);
            finalWorldPositions.Add(worldCenter);
        } else {
            // Calculate spacing based on available width and card count
            // Use more compact spacing to match the clicked state
            float availableWidth = width - cardWidth; // Account for card width
            float minSpacing = cardWidth * 0.25f; // Reduced minimum spacing for more compact layout
            float maxSpacing = cardWidth * 0.5f; // Reduced maximum spacing for more compact layout
            
            // Calculate ideal spacing that fits all cards within the width
            float idealSpacing = availableWidth / (cardCount - 1);
            float usedSpacing = Mathf.Clamp(idealSpacing, minSpacing, maxSpacing);
            
            // Recalculate total width with the used spacing
            float totalWidth = usedSpacing * (cardCount - 1);
            
            // Center the entire group by starting at negative half of total width
            float xStart = -totalWidth / 2f;
            Debug.Log($"Card layout: count={cardCount}, width={width}, cardWidth={cardWidth}, spacing={usedSpacing}, totalWidth={totalWidth}, xStart={xStart}");
            
            for (int i = 0; i < cardCount; i++) {
                float x = xStart + i * usedSpacing;
                finalAnchoredPositions.Add(new Vector2(x, 0));
                finalWorldPositions.Add(worldCenter + new Vector3(x, 0, 0));
                Debug.Log($"Card {i} position: {x}");
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
                    // CRITICAL FIX: Add safety check for dealFromTransform's RectTransform
                    RectTransform dealFromRect = dealFromTransform.GetComponent<RectTransform>();
                    if (dealFromRect != null)
                    {
                        try
                        {
                            Vector2 worldDealFromPos = dealFromRect.TransformPoint(dealFromRect.anchoredPosition);
                            startPos = rt.parent.InverseTransformPoint(worldDealFromPos);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Error transforming dealFromRect position: {e.Message}. Using fallback.");
                            startPos = rt.parent.InverseTransformPoint(dealFromTransform.position);
                        }
                    }
                    else
                    {
                        // If dealFromTransform doesn't have a RectTransform, just use its world position
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
        // Animate to new positions
        float t = 0f;
        float duration = 0.2f;
        // Capture starting positions
        Vector2[] startAnchored = new Vector2[cards.Count];
        Vector3[] startWorld = new Vector3[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            if (isUI)
                startAnchored[i] = cards[i].GetComponent<RectTransform>().anchoredPosition;
            else
                startWorld[i] = cards[i].position;
        }
        // Remove any null or destroyed cards before starting animation
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i] == null)
            {
                cards.RemoveAt(i);
                finalAnchoredPositions.RemoveAt(i);
                finalWorldPositions.RemoveAt(i);
                if (i < startAnchored.Length) startAnchored = startAnchored.Where((val, idx) => idx != i).ToArray();
                if (i < startWorld.Length) startWorld = startWorld.Where((val, idx) => idx != i).ToArray();
            }
        }
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            for (int i = 0; i < cards.Count; i++)
            {
                // Skip null or destroyed cards
                if (cards[i] == null) continue;

                bool isPopped = (currentlyPoppedCard != null && cards[i].GetComponent<CardSelector>() == currentlyPoppedCard);
                Vector2 targetPos = finalAnchoredPositions[i];
                Vector3 targetScale = Vector3.one;
                if (isPopped)
                {
                    targetPos.y += popYOffset;
                    targetScale = Vector3.one * popScaleMultiplier;
                }
                if (isUI)
                {
                    var rt = cards[i].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = Vector2.Lerp(startAnchored[i], targetPos, lerp);
                        rt.localScale = Vector3.Lerp(rt.localScale, targetScale, lerp);
                    }
                }
                else
                {
                    Vector3 worldTarget = finalWorldPositions[i];
                    if (isPopped)
                        worldTarget.y += popYOffset;
                    cards[i].position = Vector3.Lerp(startWorld[i], worldTarget, lerp);
                    cards[i].localScale = Vector3.Lerp(cards[i].localScale, targetScale, lerp);
                }
            }
            yield return null;
        }
        // Animate each card to its final position, one by one, from dealFromTransform
        for (int i = 0; i < cards.Count; i++)
        {
            // Skip null or destroyed cards
            if (cards[i] == null) continue;
            
            Transform card = cards[i];
            if (card == null || !card.gameObject) continue; // Skip if card or its gameObject is null
            
            card.gameObject.SetActive(true); // Reveal card just before animating
            if (isUI)
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                if (rt == null) continue; // Skip if RectTransform is null
                
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
                
                // Make sure the card still exists before starting the coroutine
                if (card != null && card.gameObject != null)
                {
                    yield return StartCoroutine(MoveToPositionFan(card, finalAnchoredPositions[i], 0f));
                    
                    // Check again after coroutine completes
                    if (card != null && card.gameObject != null)
                    {
                        Debug.Log($"[AnimateCardsInSequence] Finished animating {card.name} to {finalAnchoredPositions[i]}");
                    }
                }
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
        
        // Log the state of all cards after animation completes
        Debug.Log("===== ANIMATION SEQUENCE COMPLETE - CHECKING FINAL CARD STATES =====");
        for (int i = 0; i < cardParent.childCount; i++)
        {
            Transform child = cardParent.GetChild(i);
            if (child != null)
            {
                Debug.Log($"Card {child.name} final state: Active={child.gameObject.activeSelf}, Scale={child.localScale}, Position={child.position}");
                
                // Force cards to stay visible and maintain scale
                child.gameObject.SetActive(true);
                
                // Ensure CardSelector has correct original scale
                CardSelector selector = child.GetComponent<CardSelector>();
                if (selector != null)
                {
                    Debug.Log($"Card {child.name} selector originalScale={selector.OriginalScale}, isPopped={selector.IsPopped}");
                }
            }
        }
        
        // Schedule another check after a short delay to see if something changes the cards
        StartCoroutine(CheckCardStatesAfterDelay());
        
        yield break;
    }
    
    // Check card states after a delay to catch any post-animation changes
    IEnumerator CheckCardStatesAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("===== DELAYED CHECK - CARD STATES AFTER 0.5 SECONDS =====");
        for (int i = 0; i < cardParent.childCount; i++)
        {
            Transform child = cardParent.GetChild(i);
            if (child != null)
            {
                Debug.Log($"Card {child.name} delayed state: Active={child.gameObject.activeSelf}, Scale={child.localScale}, Position={child.position}");
            }
        }
    }

    // Overload for UI: target is Vector2 anchoredPosition
    IEnumerator MoveToPositionFan(Transform card, Vector2 targetAnchoredPos, float targetRot)
    {
        // Ensure the card is active/visible throughout the animation
        card.gameObject.SetActive(true);
        
        // Get the original scale from CardSelector if available, otherwise use current scale
        Vector3 originalScale = card.GetComponent<CardSelector>() != null ? card.GetComponent<CardSelector>().OriginalScale : card.localScale;
        
        // Store the desired scale for this card
        Vector3 finalScale = originalScale;
        if (card.GetComponent<CardSelector>() != null && card.GetComponent<CardSelector>().IsPopped)
        {
            finalScale = originalScale * popScaleMultiplier;
        }
        
        // Check if card is null or destroyed
        if (card == null || !card.gameObject)
        {
            Debug.LogWarning("MoveToPositionFan: Card is null or destroyed before animation starts");
            yield break;
        }
        
        Debug.Log($"MoveToPositionFan: Starting animation for {card.name} to position {targetAnchoredPos}");
        
        Quaternion startRot = card.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRot);
        RectTransform rt = card.GetComponent<RectTransform>();
        Vector3 targetPos = Vector3.zero; // Always defined
        
        // Use the finalScale we calculated earlier instead of recalculating
        Debug.Log($"MoveToPositionFan: Using finalScale for {card.name}: {finalScale}");
        
        if (rt != null)
        {
            Vector2 startPos = rt.anchoredPosition;
            Debug.Log($"MoveToPositionFan: {card.name} starting at {startPos}, target {targetAnchoredPos}");
            
            // Animation variables
            float uiT = 0f;
            float uiMaxTime = 1.0f; // Maximum animation time (1 second)
            float uiElapsedTime = 0f;
            
            while (uiT < 1f && uiElapsedTime < uiMaxTime && card != null && card.gameObject != null)
            {
                // Ensure card remains active throughout animation
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                    Debug.Log($"MoveToPositionFan: Re-activated {card.name} during animation");
                }
                
                float deltaTime = Time.deltaTime;
                uiElapsedTime += deltaTime;
                uiT += deltaTime * animationSpeed;
                uiT = Mathf.Clamp01(uiT); // Ensure t stays between 0 and 1
                
                // Check if card is still valid before updating
                if (card == null || !card.gameObject)
                {
                    Debug.LogWarning("MoveToPositionFan: Card became null during animation");
                    yield break;
                }
                
                // Maintain the final scale during animation
                card.localScale = finalScale;
                
                rt.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, uiT);
                card.rotation = Quaternion.Slerp(startRot, endRot, uiT);
                
                Debug.Log($"MoveToPositionFan: {card.name} animating t={uiT}, pos={rt.anchoredPosition}, scale={card.localScale}");
                yield return null;
            }
            
            // Final position and rotation
            if (card != null && card.gameObject != null && rt != null)
            {
                // Ensure card is active at end of animation
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                    Debug.Log($"MoveToPositionFan: Re-activated {card.name} at end of animation");
                }
                
                rt.anchoredPosition = targetAnchoredPos;
                card.rotation = endRot;
                card.localScale = finalScale; // Ensure final scale is correct
                
                // Update the layout position in CardSelector
                CardSelector cardSelector = card.GetComponent<CardSelector>();
                if (cardSelector != null)
                {
                    cardSelector.SetLayoutPosition(targetAnchoredPos, endRot);
                }
                
                Debug.Log($"MoveToPositionFan: {card.name} animation complete at {rt.anchoredPosition}, scale={card.localScale}");
            }
        }
        else
        {
            // Fallback for sprites: use world position
            Vector3 startPos = card.position;
            
            // Get the CardSelector component for scale reference
            CardSelector selectorNonUI = card.GetComponent<CardSelector>();
            
            // Get the original scale from CardSelector if available, otherwise use current scale
            Vector3 originalScaleNonUI = selectorNonUI != null ? selectorNonUI.OriginalScale : card.localScale;
            
            // Store the desired scale for this card
            Vector3 finalScaleNonUI = originalScaleNonUI;
            if (selectorNonUI != null && selectorNonUI.IsPopped)
            {
                finalScaleNonUI = originalScaleNonUI * popScaleMultiplier;
            }
            
            Debug.Log($"MoveToPositionFan (non-UI): Using finalScale for {card.name}: {finalScaleNonUI}");
            
            targetPos = new Vector3(targetAnchoredPos.x, targetAnchoredPos.y, card.position.z);
            
            Debug.Log($"MoveToPositionFan (non-UI): {card.name} starting at {startPos}, target {targetPos}");
            
            float animT = 0f;
            float animMaxTime = 1.0f; // Maximum animation time (1 second)
            float animElapsedTime = 0f;
            
            while (animT < 1f && animElapsedTime < animMaxTime && card != null && card.gameObject != null)
            {
                // Ensure card remains active throughout animation
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                    Debug.Log($"MoveToPositionFan (non-UI): Re-activated {card.name} during animation");
                }
                
                float deltaTime = Time.deltaTime;
                animElapsedTime += deltaTime;
                animT += deltaTime * animationSpeed;
                animT = Mathf.Clamp01(animT); // Ensure t stays between 0 and 1
                
                // Check if card is still valid before updating
                if (card == null || !card.gameObject)
                {
                    Debug.LogWarning("MoveToPositionFan (non-UI): Card became null during animation");
                    yield break;
                }
                
                // Maintain the final scale during animation
                card.localScale = finalScaleNonUI;
                
                card.position = Vector3.Lerp(startPos, targetPos, animT);
                card.rotation = Quaternion.Slerp(startRot, endRot, animT);
                
                Debug.Log($"MoveToPositionFan (non-UI): {card.name} animating t={animT}, pos={card.position}, scale={card.localScale}");
                yield return null;
            }
            
            // Final position and rotation
            if (card != null && card.gameObject != null)
            {
                // Ensure card is active at end of animation
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                    Debug.Log($"MoveToPositionFan (non-UI): Re-activated {card.name} at end of animation");
                }
                
                card.position = targetPos;
                card.rotation = endRot;
                card.localScale = finalScaleNonUI; // Ensure final scale is correct
                
                // Update the layout position in CardSelector
                if (selectorNonUI != null)
                {
                    selectorNonUI.SetLayoutPosition(targetPos, endRot);
                }
                
                Debug.Log($"MoveToPositionFan (non-UI): {card.name} animation complete at {card.position}, scale={card.localScale}");
            }
        }
        // Update the layout position for popping
        CardSelector selectorMove = card.GetComponent<CardSelector>();
        if (selectorMove != null)
        {
            if (rt != null)
            {
                selectorMove.SetLayoutPosition(targetAnchoredPos, card.rotation);
            }
            else
            {
                selectorMove.SetLayoutPosition(targetPos, card.rotation);
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
        card.PopOut(popScaleMultiplier);
        StartCoroutine(AnimatePopEffectOnly()); // Only animate pop effect, not full deal
    }

    // Only animate the pop effect for the current hand, do not re-deal or animate from dealFromTransform
    public IEnumerator AnimatePopEffectOnly()
    {
        // Safety check for cardParent
        if (cardParent == null)
        {
            Debug.LogError("CardParent is null in AnimatePopEffectOnly!");
            yield break;
        }
        
        Debug.Log("AnimatePopEffectOnly: Starting pop effect animation");
        
        // Gather all UI hand cards and their selectors
        List<RectTransform> cardRects = new List<RectTransform>();
        List<CardSelector> selectors = new List<CardSelector>();
        List<Vector3> originalScales = new List<Vector3>(); // Store original scales
        
        for (int i = 0; i < cardParent.childCount; i++)
        {
            if (i >= cardParent.childCount) break; // Safety check in case children count changes during iteration
            
            Transform child = cardParent.GetChild(i);
            if (child == null) continue; // Skip null children
            
            RectTransform rt = child.GetComponent<RectTransform>();
            CardDisplay display = child.GetComponent<CardDisplay>();
            CardSelector selector = child.GetComponent<CardSelector>();
            if (rt != null && display != null && selector != null)
            {
                // Ensure the card is active/visible
                if (!child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                    Debug.Log($"AnimatePopEffectOnly: Activated previously inactive card {child.name}");
                }
                
                cardRects.Add(rt);
                selectors.Add(selector);
                originalScales.Add(child.localScale); // Store original scale
                Debug.Log($"AnimatePopEffectOnly: Added card {child.name} with scale {child.localScale}");
            }
        }
        int cardCount = cardRects.Count;
        if (cardCount == 0)
        {
            Debug.LogWarning("AnimatePopEffectOnly: No cards found to animate");
            yield break;
        }

        // Sort by card number (hand order)
        for (int i = 0; i < cardCount - 1; i++)
        {
            // Skip null or destroyed objects
            if (cardRects[i] == null || !cardRects[i].gameObject) continue;
            
            for (int j = i + 1; j < cardCount; j++)
            {
                // Skip null or destroyed objects
                if (cardRects[j] == null || !cardRects[j].gameObject) continue;
                
                if (ExtractCardNumber(cardRects[j].gameObject.name) < ExtractCardNumber(cardRects[i].gameObject.name))
                {
                    // Swap
                    var tempRt = cardRects[i];
                    cardRects[i] = cardRects[j];
                    cardRects[j] = tempRt;
                    var tempSel = selectors[i];
                    selectors[i] = selectors[j];
                    selectors[j] = tempSel;
                    var tempScale = originalScales[i];
                    originalScales[i] = originalScales[j];
                    originalScales[j] = tempScale;
                }
            }
        }
        
        // Remove any null or destroyed cards before animation
        for (int i = cardCount - 1; i >= 0; i--)
        {
            if (cardRects[i] == null || !cardRects[i].gameObject)
            {
                cardRects.RemoveAt(i);
                selectors.RemoveAt(i);
                originalScales.RemoveAt(i);
                cardCount--;
            }
        }
        
        // If all cards were removed, exit
        if (cardCount == 0)
        {
            Debug.LogWarning("AnimatePopEffectOnly: All cards were removed or null");
            yield break;
        }

        // Calculate logical slot X positions (evenly spaced, centered)
        RectTransform parentRect = cardParent.GetComponent<RectTransform>();
        float width = parentRect != null ? parentRect.rect.width : zoneWidth;
        // Get card width for better spacing calculation
        float cardWidth = 200f; // Default card width
        if (cardCount > 0 && cardRects[0] != null)
        {
            cardWidth = cardRects[0].rect.width;
            Debug.Log($"AnimatePopEffectOnly: Using actual card width: {cardWidth}");
        }
        
        // Use more compact spacing to match the initial layout
        float availableWidth = width - cardWidth; // Account for card width
        float minSpacing = cardWidth * 0.25f; // Reduced minimum spacing for more compact layout
        float maxSpacing = cardWidth * 0.5f; // Reduced maximum spacing for more compact layout
        
        // Calculate ideal spacing that fits all cards within the width
        float idealSpacing = cardCount > 1 ? availableWidth / (cardCount - 1) : 0f;
        float usedSpacing = Mathf.Clamp(idealSpacing, minSpacing, maxSpacing);
        float totalWidth = usedSpacing * (cardCount - 1);
        float xStart = -totalWidth / 2f;

        Debug.Log($"AnimatePopEffectOnly: Calculated layout with {cardCount} cards, width={width}, spacing={usedSpacing}");

        // Capture starting positions
        Vector2[] startAnchored = new Vector2[cardCount];
        for (int i = 0; i < cardCount; i++)
            startAnchored[i] = cardRects[i].anchoredPosition;

        // Calculate target positions based on logical slot positions
        Vector2[] targetAnchored = new Vector2[cardCount];
        Vector3[] targetScales = new Vector3[cardCount];
        
        for (int i = 0; i < cardCount; i++)
        {
            // Calculate logical slot position (this is the key fix)
            float x = xStart + i * usedSpacing;
            float y = 0f; // Base Y position for all cards
            
            // Check if this card should be popped
            bool isPopped = (currentlyPoppedCard != null && selectors[i] == currentlyPoppedCard);
            
            // Apply pop effect if needed
            if (isPopped)
            {
                y += popYOffset; // Add pop offset to Y position
                targetScales[i] = originalScales[i] * popScaleMultiplier; // Apply pop scale
                Debug.Log($"AnimatePopEffectOnly: Card {cardRects[i].name} is popped, Y={y}, Scale={targetScales[i]}");
            }
            else
            {
                targetScales[i] = originalScales[i]; // Use original scale for non-popped cards
                Debug.Log($"AnimatePopEffectOnly: Card {cardRects[i].name} is not popped, Y={y}, Scale={targetScales[i]}");
            }
            
            targetAnchored[i] = new Vector2(x, y);
        }

        // Animate
        float t = 0f;
        float duration = 0.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            
            for (int i = 0; i < cardCount; i++)
            {
                // Skip null or destroyed RectTransforms
                if (cardRects[i] == null || !cardRects[i].gameObject) continue;
                
                // Ensure the card is active/visible during animation
                if (!cardRects[i].gameObject.activeSelf)
                {
                    cardRects[i].gameObject.SetActive(true);
                }
                
                // Apply position animation
                cardRects[i].anchoredPosition = Vector2.Lerp(startAnchored[i], targetAnchored[i], lerp);
                
                // Apply scale animation
                cardRects[i].localScale = Vector3.Lerp(cardRects[i].localScale, targetScales[i], lerp);
            }
            yield return null;
        }
        
        // Snap to final positions and scales (with null checks)
        for (int i = 0; i < cardCount; i++)
        {
            // Skip null or destroyed RectTransforms
            if (cardRects[i] == null || !cardRects[i].gameObject) continue;
            
            // Ensure the card is active/visible at the end
            if (!cardRects[i].gameObject.activeSelf)
            {
                cardRects[i].gameObject.SetActive(true);
                Debug.Log($"AnimatePopEffectOnly: Activated card {cardRects[i].name} at end of animation");
            }
            
            // Apply final position and scale
            cardRects[i].anchoredPosition = targetAnchored[i];
            cardRects[i].localScale = targetScales[i];
            
            // Update the layout position in CardSelector for future reference
            if (selectors[i] != null)
            {
                selectors[i].SetLayoutPosition(targetAnchored[i], cardRects[i].rotation);
            }
            
            Debug.Log($"AnimatePopEffectOnly: Final position for {cardRects[i].name}: {targetAnchored[i]}, scale: {targetScales[i]}");
        }
    }

    // Called when a card starts being dragged
    public void OnCardDragStart(CardSelector draggingCard)
    {
        if (currentlyPoppedCard != null && currentlyPoppedCard != draggingCard)
        {
            currentlyPoppedCard.ResetPop();
            currentlyPoppedCard = null;
            StartCoroutine(AnimatePopEffectOnly());
        }
    }
    
    // Called when a card is played/removed from the hand
    // This method ensures only the remaining cards are re-laid out without re-dealing
    public void RemoveCardAndReLayout(Transform cardToRemove)
    {
        Debug.Log($"RemoveCardAndReLayout called for card: {cardToRemove.name}");
        
        // Make sure we don't try to re-layout a card that's being removed
        if (currentlyPoppedCard != null && currentlyPoppedCard.transform == cardToRemove)
        {
            currentlyPoppedCard = null;
        }
        
        // Start the re-layout coroutine after a short delay to ensure the card is fully removed
        StartCoroutine(DelayedReLayout());
    }
    
    private IEnumerator DelayedReLayout()
    {
        // Small delay to ensure any card destruction has completed
        yield return new WaitForSeconds(0.1f);
        
        // Use AnimatePopEffectOnly which is designed to only re-layout existing cards
        // without re-dealing them from the deck
        StartCoroutine(AnimatePopEffectOnly());
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

