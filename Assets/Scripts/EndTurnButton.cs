using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    private Button endTurnButton;
    private EnergyManager energyManager;

    void Start()
    {
        // Get the Button component
        endTurnButton = GetComponent<Button>();
        if (endTurnButton == null)
        {
            Debug.LogError("EndTurnButton: No Button component found!");
            return;
        }

        // Find the EnergyManager
        energyManager = EnergyManager.Instance;
        if (energyManager == null)
        {
            Debug.LogError("EndTurnButton: No EnergyManager found in scene!");
            return;
        }

        // Add click listener
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        
        Debug.Log("EndTurnButton initialized successfully.");
    }

    void OnEndTurnClicked()
    {
        Debug.Log("End Turn button clicked!");
        
        // Call the StartTurn method which refills energy
        energyManager.StartTurn();
        
        // Here you could add additional end turn logic:
        // - Draw new cards
        // - Trigger enemy actions
        // - Update turn counter
        // - Play animations or sounds
    }

    void OnDestroy()
    {
        // Clean up listener when object is destroyed
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }
    }
}
