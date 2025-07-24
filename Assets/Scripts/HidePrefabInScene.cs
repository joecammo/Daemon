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
            
            // Also hide its parent if it has one, but NEVER hide CardCanvas
            if (cardInScene.transform.parent != null)
            {
                // CRITICAL FIX: Don't hide CardCanvas or any parent containing CardCanvas
                if (cardInScene.transform.parent.name != "CardCanvas" && 
                    !cardInScene.transform.parent.name.Contains("CardCanvas"))
                {
                    Debug.Log($"[HidePrefabInScene] Also hiding Card's parent: {cardInScene.transform.parent.gameObject.name}");
                    cardInScene.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log($"[HidePrefabInScene] NOT hiding Card's parent because it's CardCanvas: {cardInScene.transform.parent.gameObject.name}");
                }
            }
        }
        else
        {
            Debug.Log("[HidePrefabInScene] No exact 'Card' GameObject found, trying partial name match");
        }
        
        // Method 2: Find all GameObjects in scene and check if name contains "Card"
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            // CRITICAL FIX: Only hide the original Card prefab, not any clones or instantiated cards
            // Check for exact name match "Card" and ensure it's not a clone
            if (obj.name == "Card" && !obj.name.Contains("Clone") && !obj.name.Contains("Parent") && !obj.name.Contains("Zone"))
            {
                Debug.Log($"[HidePrefabInScene] Found Card prefab: {obj.name} - hiding it");
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
        HidePrefabInScene[] prefabHiders = Object.FindObjectsByType<HidePrefabInScene>(FindObjectsSortMode.None);
        
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
        // CRITICAL FIX: Only hide this specific GameObject if it's the original prefab, not a clone
        if (gameObject.name.Contains("Clone"))
        {
            Debug.Log($"[HidePrefabInScene] Awake on {gameObject.name} - NOT hiding it because it's a clone");
            // Remove this component from clones to prevent any issues
            Destroy(this);
            return;
        }
        
        // Backup method in case the static method doesn't catch it
        Debug.Log($"[HidePrefabInScene] Awake on {gameObject.name} - hiding it");
        
        // CRITICAL FIX: Don't disable if this is CardCanvas or part of CardCanvas
        if (gameObject.name == "CardCanvas" || gameObject.name.Contains("CardCanvas"))
        {
            Debug.Log($"[HidePrefabInScene] NOT hiding {gameObject.name} because it's CardCanvas");
            // Remove this component from CardCanvas to prevent any issues
            Destroy(this);
            return;
        }
        
        // Force disable this GameObject - even if it's static
        gameObject.SetActive(false);
        
        // Try to disable the renderer components directly
        DisableAllRenderers(gameObject);
        
        if (disableParentToo && transform.parent != null)
        {
            // CRITICAL FIX: Don't disable parent if it's CardCanvas
            if (transform.parent.name == "CardCanvas" || transform.parent.name.Contains("CardCanvas"))
            {
                Debug.Log($"[HidePrefabInScene] NOT hiding parent {transform.parent.name} because it's CardCanvas");
            }
            else
            {
                transform.parent.gameObject.SetActive(false);
                // Also disable parent's renderers
                DisableAllRenderers(transform.parent.gameObject);
            }
        }
        
        // If a target name is specified, find and hide that object
        if (!string.IsNullOrEmpty(targetGameObjectName))
        {
            GameObject targetObj = GameObject.Find(targetGameObjectName);
            if (targetObj != null && !targetObj.name.Contains("Clone"))
            {
                Debug.Log($"[HidePrefabInScene] Hiding target: {targetObj.name}");
                targetObj.SetActive(false);
                // Also disable its renderers
                DisableAllRenderers(targetObj);
            }
        }
        
        // Try to find the Card object specifically and ensure it's hidden
        // CRITICAL FIX: Only hide the original Card prefab, not clones
        GameObject cardObj = GameObject.Find("Card");
        if (cardObj != null && cardObj.name == "Card" && !cardObj.name.Contains("Clone"))
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
        
        // CRITICAL FIX: Don't disable renderers on clones or CardCanvas
        if (obj.name.Contains("Clone") || obj.name == "CardCanvas" || obj.name.Contains("CardCanvas"))
        {
            Debug.Log($"[HidePrefabInScene] NOT disabling renderers on {obj.name} because it's a clone or CardCanvas");
            return;
        }
        
        // Check if this object is a child of CardCanvas
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.name == "CardCanvas" || parent.name.Contains("CardCanvas"))
            {
                Debug.Log($"[HidePrefabInScene] NOT disabling renderers on {obj.name} because it's a child of CardCanvas");
                return;
            }
            parent = parent.parent;
        }
        
        // Disable all renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            // Skip clones
            if (renderer.gameObject.name.Contains("Clone")) continue;
            
            renderer.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled renderer on {renderer.gameObject.name}");
        }
        
        // Disable all Canvas Renderers (UI elements)
        UnityEngine.UI.Graphic[] graphics = obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var graphic in graphics)
        {
            // Skip clones
            if (graphic.gameObject.name.Contains("Clone")) continue;
            
            graphic.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled UI graphic on {graphic.gameObject.name}");
        }
        
        // Disable any Canvas components, but NEVER CardCanvas
        Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            // Skip clones and CardCanvas
            if (canvas.gameObject.name.Contains("Clone") || 
                canvas.gameObject.name == "CardCanvas" || 
                canvas.gameObject.name.Contains("CardCanvas"))
            {
                Debug.Log($"[HidePrefabInScene] NOT disabling Canvas on {canvas.gameObject.name} (protected)");
                continue;
            }
            
            // Check if this canvas is a child of CardCanvas
            bool isChildOfCardCanvas = false;
            Transform canvasParent = canvas.transform.parent;
            while (canvasParent != null)
            {
                if (canvasParent.name == "CardCanvas" || canvasParent.name.Contains("CardCanvas"))
                {
                    isChildOfCardCanvas = true;
                    break;
                }
                canvasParent = canvasParent.parent;
            }
            
            if (isChildOfCardCanvas)
            {
                Debug.Log($"[HidePrefabInScene] NOT disabling Canvas on {canvas.gameObject.name} (child of CardCanvas)");
                continue;
            }
            
            canvas.enabled = false;
            Debug.Log($"[HidePrefabInScene] Disabled Canvas on {canvas.gameObject.name}");
        }
    }
}
