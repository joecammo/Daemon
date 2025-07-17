using UnityEditor;
using UnityEngine;

public class AddCollidersToCards : MonoBehaviour
{
    [MenuItem("Tools/Add BoxCollider2D To All Cards In Parent")]
    static void AddColliders()
    {
        if (Selection.activeTransform == null)
        {
            Debug.LogError("Please select the card parent GameObject in the Hierarchy.");
            return;
        }

        int count = 0;
        foreach (Transform child in Selection.activeTransform)
        {
            if (child.GetComponent<BoxCollider2D>() == null)
            {
                child.gameObject.AddComponent<BoxCollider2D>();
                count++;
            }
        }
        Debug.Log($"Added BoxCollider2D to {count} cards under {Selection.activeTransform.name}.");
    }
}
