using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandLayoutAnimator : MonoBehaviour
{
    [Header("Card Pop Settings")]
    public float popYOffset = 1.0f; // How much to move up when popped
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
        foreach (Transform child in cardParent)
        {
            cards.Add(child);
        }
        // Sort by card number extracted from the name (e.g., Card_10_Orange)
        cards = cards.OrderBy(t => ExtractCardNumber(t.name)).ToList();

        // Fan layout calculation
        float angleStep = (cards.Count > 1) ? fanAngle / (cards.Count - 1) : 0f;
        float startAngle = -fanAngle / 2f;
        Vector3 center = transform.position;

        for (int i = 0; i < cards.Count; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = Mathf.Deg2Rad * angle;
            // Original fan arc: X = sin(angle)*radius, Y = abs(cos(angle))*radius
            Vector3 offset = new Vector3(Mathf.Sin(rad) * fanRadius, Mathf.Abs(Mathf.Cos(rad)) * fanRadius, 0);
            Vector3 targetPos = center + offset + new Vector3((i - (cards.Count - 1) / 2f) * spacing, 0, 0);
            float targetRot = angle;
            // Set sortingOrder so last card is on top
            var sr = cards[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = i;
            StartCoroutine(MoveToPositionFan(cards[i], targetPos, targetRot));
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    IEnumerator MoveToPositionFan(Transform card, Vector3 target, float targetRot)
    {
        Quaternion startRot = card.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRot);
        while (Vector3.Distance(card.position, target) > 0.01f || Quaternion.Angle(card.rotation, endRot) > 0.5f)
        {
            card.position = Vector3.Lerp(card.position, target, Time.deltaTime * animationSpeed);
            card.rotation = Quaternion.Lerp(card.rotation, endRot, Time.deltaTime * animationSpeed);
            yield return null;
        }
        card.position = target;
        card.rotation = endRot;
        // Update the layout position for popping
       CardSelector selector = card.GetComponent<CardSelector>();
if (selector != null)
{
    selector.SetLayoutPosition(target, card.rotation); // <--- THIS IS CORRECT
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
