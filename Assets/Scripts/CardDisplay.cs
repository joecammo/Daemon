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
    }
}
