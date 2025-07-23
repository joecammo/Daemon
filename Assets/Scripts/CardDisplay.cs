using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text costText;
    public TMP_Text typeText;
    public TMP_Text effectText;
    public Image backgroundImage;

    // Call this to update the card visuals
    public void SetCard(string title, string cost, string type, string effect, Color affinityColor)
    {
        if (titleText) titleText.text = title;
        if (costText) costText.text = cost;
        if (typeText) typeText.text = type;
        if (effectText) effectText.text = effect;
        if (backgroundImage) backgroundImage.color = affinityColor;
        
        // Debug card visibility
        Debug.Log($"[CardDisplay] Setting card: {title}, Cost: {cost}, Type: {type}");
        Debug.Log($"[CardDisplay] UI Components - TitleText: {(titleText != null ? "Found" : "Missing")}, CostText: {(costText != null ? "Found" : "Missing")}, TypeText: {(typeText != null ? "Found" : "Missing")}, EffectText: {(effectText != null ? "Found" : "Missing")}, BackgroundImage: {(backgroundImage != null ? "Found" : "Missing")}");
        
        // Check if any Canvas or CanvasGroup is affecting visibility
        Canvas canvas = GetComponentInParent<Canvas>();
        CanvasGroup canvasGroup = GetComponentInParent<CanvasGroup>();
        Debug.Log($"[CardDisplay] Parent Canvas: {(canvas != null ? $"Found - WorldSpace: {canvas.renderMode == RenderMode.WorldSpace}" : "Missing")}, CanvasGroup: {(canvasGroup != null ? $"Found - Alpha: {canvasGroup.alpha}" : "Missing")}");
        
        // Force card to be visible
        if (backgroundImage) {
            backgroundImage.enabled = true;
            // Make sure color has non-zero alpha
            Color color = backgroundImage.color;
            if (color.a < 0.1f) {
                color.a = 1.0f;
                backgroundImage.color = color;
                Debug.Log($"[CardDisplay] Fixed background image alpha: {color}");
            }
        }
        
        // CRITICAL FIX: Force RectTransform scale to (1,1,1)
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) {
            // Log current scale before changing
            Debug.Log($"[CardDisplay] Card {title} RectTransform scale BEFORE fix: {rt.localScale}");
            
            // Force scale to (1,1,1)
            rt.localScale = Vector3.one;
            
            // Log scale after changing
            Debug.Log($"[CardDisplay] Card {title} RectTransform scale AFTER fix: {rt.localScale}");
        } else {
            Debug.LogError($"[CardDisplay] Card {title} has no RectTransform component!");
        }
        
        // Also force transform scale to (1,1,1) as a backup
        transform.localScale = Vector3.one;
        Debug.Log($"[CardDisplay] Card {title} transform scale set to: {transform.localScale}");
        
        // Make sure the card is active
        gameObject.SetActive(true);
        Debug.Log($"[CardDisplay] Card {title} gameObject.activeSelf: {gameObject.activeSelf}");
    }
}
