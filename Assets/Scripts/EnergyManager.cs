using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    [Header("Energy Settings")]
    public int maxEnergy = 6;
    public int currentEnergy = 6;

    [Header("Energy UI References")]
    public GameObject[] emptyEnergyObjects;  // Empty_1, Empty_2, etc.
    public GameObject[] usedEnergyObjects;   // Used_1, Used_2, etc.
    public GameObject[] availableEnergyObjects; // Available_1, Available_2, etc.

    private static EnergyManager _instance;
    public static EnergyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EnergyManager>();
                if (_instance == null)
                {
                    Debug.LogError("No EnergyManager found in scene!");
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        // Find energy objects if not assigned in inspector
        if (emptyEnergyObjects == null || emptyEnergyObjects.Length == 0)
        {
            emptyEnergyObjects = new GameObject[maxEnergy];
            for (int i = 0; i < maxEnergy; i++)
            {
                emptyEnergyObjects[i] = transform.Find($"Empty_{i+1}")?.gameObject;
            }
        }
        
        if (usedEnergyObjects == null || usedEnergyObjects.Length == 0)
        {
            usedEnergyObjects = new GameObject[maxEnergy];
            for (int i = 0; i < maxEnergy; i++)
            {
                usedEnergyObjects[i] = transform.Find($"Used_{i+1}")?.gameObject;
            }
        }
        
        if (availableEnergyObjects == null || availableEnergyObjects.Length == 0)
        {
            availableEnergyObjects = new GameObject[maxEnergy];
            for (int i = 0; i < maxEnergy; i++)
            {
                availableEnergyObjects[i] = transform.Find($"Available_{i+1}")?.gameObject;
            }
        }
        
        // Initialize energy UI
        UpdateEnergyUI();
    }

    public void UpdateEnergyUI()
    {
        // Make all objects invisible first
        for (int i = 0; i < maxEnergy; i++)
        {
            if (i < emptyEnergyObjects.Length) emptyEnergyObjects[i].SetActive(false);
            if (i < usedEnergyObjects.Length) usedEnergyObjects[i].SetActive(false);
            if (i < availableEnergyObjects.Length) availableEnergyObjects[i].SetActive(false);
        }

        // Show appropriate objects based on current energy
        // Reversed order: start from the highest index (right side) and work backwards
        // This makes the first energy used be the first in the UI (right-most)
        for (int i = 0; i < maxEnergy; i++)
        {
            // Convert to reversed index (maxEnergy-1-i) to process right-to-left
            int displayIndex = maxEnergy - 1 - i;
            
            if (i < currentEnergy)
            {
                // Available energy
                if (displayIndex < availableEnergyObjects.Length) availableEnergyObjects[displayIndex].SetActive(true);
            }
            else
            {
                // Used energy
                if (displayIndex < usedEnergyObjects.Length) usedEnergyObjects[displayIndex].SetActive(true);
            }
        }
    }

    public bool CanSpendEnergy(int cost)
    {
        return cost <= currentEnergy;
    }

    public bool SpendEnergy(int cost)
    {
        if (!CanSpendEnergy(cost))
        {
            Debug.Log($"Not enough energy! Required: {cost}, Available: {currentEnergy}");
            return false;
        }

        currentEnergy -= cost;
        UpdateEnergyUI();
        Debug.Log($"Spent {cost} energy. Remaining: {currentEnergy}");
        return true;
    }

    public void RefillEnergy()
    {
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        Debug.Log($"Energy refilled to maximum: {currentEnergy}");
    }
    
    // Call this at the start of each player's turn
    public void StartTurn()
    {
        RefillEnergy();
        Debug.Log("New turn started, energy refilled.");
    }
}
