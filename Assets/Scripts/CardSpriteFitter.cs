using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CardSpriteFitter : MonoBehaviour
{
    public Vector2 targetScale = new Vector2(268f, 378f); // Desired transform scale

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 spriteSize = sr.sprite.bounds.size;
            Vector3 newScale = transform.localScale;
            newScale.x = targetScale.x / spriteSize.x;
            newScale.y = targetScale.y / spriteSize.y;
            transform.localScale = newScale;
        }
    }
}
