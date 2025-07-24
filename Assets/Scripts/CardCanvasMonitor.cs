using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monitors the CardCanvas GameObject and logs when it gets deactivated
/// </summary>
public class CardCanvasMonitor : MonoBehaviour
{
    private bool wasActive = true;
    private string deactivationStack = "";
    
    void Awake()
    {
        Debug.Log($"[CardCanvasMonitor] Initialized on {gameObject.name}");
        wasActive = gameObject.activeInHierarchy;
    }
    
    void OnEnable()
    {
        Debug.Log($"[CardCanvasMonitor] {gameObject.name} was ENABLED");
        wasActive = true;
    }
    
    void OnDisable()
    {
        // Get the stack trace to identify what's disabling this object
        deactivationStack = System.Environment.StackTrace;
        Debug.LogError($"[CardCanvasMonitor] {gameObject.name} was DISABLED! Stack trace:\n{deactivationStack}");
    }
    
    void Update()
    {
        // Check if the active state changed
        bool isActive = gameObject.activeInHierarchy;
        if (wasActive && !isActive)
        {
            Debug.LogError($"[CardCanvasMonitor] {gameObject.name} was deactivated during Update! Stack trace:\n{System.Environment.StackTrace}");
        }
        else if (!wasActive && isActive)
        {
            Debug.Log($"[CardCanvasMonitor] {gameObject.name} was activated during Update");
        }
        
        wasActive = isActive;
    }
    
    // Force the CardCanvas to stay active
    void LateUpdate()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[CardCanvasMonitor] Forcing {gameObject.name} to be active in LateUpdate");
            gameObject.SetActive(true);
        }
    }
}
