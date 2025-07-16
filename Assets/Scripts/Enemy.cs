using UnityEngine;

public class Enemy : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Material baseMaterial;     // Material with no outline
    public Material outlineMaterial;  // Material with outline
    public Color highlightColor = Color.red;
    public float outlineWidth = 2f; // Set to visible value for outline

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && baseMaterial != null)
        {
            // Set the default material to base (no outline)
            spriteRenderer.material = baseMaterial;
        }
    }

    // Called when a card is hovered over this enemy
    public void Highlight(bool on)
    {
        if (spriteRenderer != null)
        {
            if (on && outlineMaterial != null)
            {
                spriteRenderer.material = outlineMaterial;
                spriteRenderer.material.SetColor("_OutlineColor", highlightColor);
                spriteRenderer.material.SetFloat("_OutlineThickness", outlineWidth);
            }
            else if (!on && baseMaterial != null)
            {
                spriteRenderer.material = baseMaterial;
            }
        }
    }

    // Stub for taking damage
    public void TakeDamage(int amount)
    {
        Debug.Log($"{gameObject.name} took {amount} damage!");
        // TODO: Implement health/damage system
    }
}
