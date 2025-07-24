using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to any prefab in the scene that should be hidden at runtime.
/// It will disable the GameObject immediately when the scene starts.
/// </summary>
public class HidePrefabInScene : MonoBehaviour
{
    [Tooltip("Set to true to also disable the parent GameObject")]
    public bool disableParentToo = false;
    
    [Tooltip("Name of the GameObject to find and hide (if different from this one)")]
    public string targetGameObjectName = "";
    
    // This executes before Awake on any other scripts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod()
    {
        Debug.Log("[HidePrefabInScene] Starting to search for Card objects in scene");
        
        // Try multiple ways to find Card objects in the scene
        
        // Method 1: Exact name match
        GameObject cardInScene = GameObject.Find("Card");
        if (cardInScene != null)
        {
            Debug.Log($"[HidePrefabInScene] Found Card in scene - hiding it");
            cardInScene.SetActive(false);
            
            // Also hide its parent if it has one
            if (cardInScene.transform.parent != null)
            {
                Debug.Log($"[HidePrefabInScene] Also hiding Card's parent: {cardInScene.transform.parent.gameObject.name}");
                cardInScene.transform.parent.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[HidePrefabInScene] No exact 'Card' GameObject found, trying partial name match");
        }
        
        // Method 2: Find all GameObjects in scene and check if name contains "Card"
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Card") && !obj.name.Contains("Clone") && !obj.name.Contains("Parent") && !obj.name.Contains("Zone"))
            {
                Debug.Log($"[HidePrefabInScene] Found Card-like object: {obj.name} - hiding it");
                obj.SetActive(false);
                
                // Also hide its parent if it has one
                if (obj.transform.parent != null)
                {
                    Debug.Log($"[HidePrefabInScene] Also hiding {obj.name}'s parent: {obj.transform.parent.gameObject.name}");
                    obj.transform.parent.gameObject.SetActive(false);
                }
            }
        }
        
        // Find all instances of this script in the scene
        HidePrefabInScene[] prefabHiders = Object.FindObjectsOfType<HidePrefabInScene>();
        
        foreach (var hider in prefabHiders)
        {
            if (hider != null && hider.gameObject != null)
            {
                Debug.Log($"[HidePrefabInScene] Hiding prefab: {hider.gameObject.name}");
                
                // If a target name is specified, find and hide that object
                if (!string.IsNullOrEmpty(hider.targetGameObjectName))
                {
                    GameObject targetObj = GameObject.Find(hider.targetGameObjectName);
                    if (targetObj != null)
                    {
                        Debug.Log($"[HidePrefabInScene] Hiding target: {targetObj.name}");
                        targetObj.SetActive(false);
                    }
                }
                
                // If set to disable parent too
                if (hider.disableParentToo && hider.transform.parent != null)
                {
                    hider.transform.parent.gameObject.SetActive(false);
                    Debug.Log($"[HidePrefabInScene] Also hiding parent: {hider.transform.parent.gameObject.name}");
                }
                
                // Hide this GameObject
                hider.gameObject.SetActive(false);
            }
        }
    }
    
    void Awake()
    {
        // Backup method in case the static method doesn't catch it
        Debug.Log($"[HidePrefabInScene] Awake on {gameObject.name} - hiding it");
        
        // Force disable this GameObject - even if it's static
        gameObject.SetActive(false);
        
        // Try to disable the renderer components directly
        DisableAllRenderers(gameObject);
        
        if (disableParentToo && transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
            // Also disable parent's renderers
            DisableAllRenderers(transform.parent.gameObject);
        }
        
        // If a target name is specified, find and hide that object
        if (!string.IsNullOrEmpty(targetGameObjectName))
        {
            GameObject targetObj = GameObject.Find(targetGameObjectName);
            if (targetObj != null)
            {
                Debug.Log($"[HidePrefabInScene] Hiding target: {targetObj.name}");
                targetObj.SetActive(false);
                // Also disable its renderers
                DisableAllRenderers(targetObj);
            }
        }
        
        // Try to find the Card object specifically and ensure it's hidden
        GameObject cardObj = GameObject.Find("Card");
        if (cardObj != null)
        {
            Debug.Log($"[HidePrefabInScene] Found Card object in Awake - forcing it hidden");
            cardObj.SetActive(false);
            DisableAllRenderers(cardObj);
        }
    }
    
    // Helper method to disable all renderers on a GameObject
    private void DisableAllRenderers(GameObject obj)
    {
        if (obj == null) return;
        
        // Disable all renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled renderer on {renderer.gameObject.name}");
        }
        
        // Disable all Canvas Renderers (UI elements)
        UnityEngine.UI.Graphic[] graphics = obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled UI graphic on {graphic.gameObject.name}");
        }
        
        // Disable any Canvas components
        Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            canvas.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled Canvas on {canvas.gameObject.name}");
        }
    }
}
