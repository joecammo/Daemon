using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(Button))]
public class EndTurnButton : MonoBehaviour
{
    [Header("References")]
    private Button endTurnButton;
    private EnergyManager energyManager;
    private CardGenerator cardGenerator;
    private HandLayoutAnimator handLayoutAnimator;
    
    [Header("Hand Settings")]
    public int maxHandSize = 7; // Maximum number of cards in hand

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
        
        // Find the CardGenerator
        cardGenerator = FindFirstObjectByType<CardGenerator>();
        if (cardGenerator == null)
        {
            Debug.LogError("EndTurnButton: No CardGenerator found in scene!");
            return;
        }
        
        // Find the HandLayoutAnimator
        handLayoutAnimator = FindFirstObjectByType<HandLayoutAnimator>();
        if (handLayoutAnimator == null)
        {
            Debug.LogError("EndTurnButton: No HandLayoutAnimator found in scene!");
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
        
        // Deal cards up to the maximum hand size
        DealCardsToMaxHandSize();
        
        // Here you could add additional end turn logic:
        // - Trigger enemy actions
        // - Update turn counter
        // - Play animations or sounds
    }
    
    // Deal cards to the player's hand up to the maximum hand size
    private void DealCardsToMaxHandSize()
    {
        if (handLayoutAnimator == null || handLayoutAnimator.cardParent == null || cardGenerator == null)
        {
            Debug.LogError("Cannot deal cards: Missing required components");
            return;
        }
        
        // Count current cards in hand
        int currentHandSize = 0;
        List<Transform> currentCards = new List<Transform>();
        
        for (int i = 0; i < handLayoutAnimator.cardParent.childCount; i++)
        {
            Transform child = handLayoutAnimator.cardParent.GetChild(i);
            if (child != null && child.gameObject.activeSelf && child.GetComponent<CardDisplay>() != null)
            {
                currentHandSize++;
                currentCards.Add(child);
            }
        }
        
        Debug.Log($"Current hand size: {currentHandSize}, Max hand size: {maxHandSize}");
        
        // If hand is already at or above max size, do nothing
        if (currentHandSize >= maxHandSize)
        {
            Debug.Log("Hand is already at maximum size. No cards dealt.");
            return;
        }
        
        // Calculate how many cards to deal
        int cardsToDeal = maxHandSize - currentHandSize;
        Debug.Log($"Dealing {cardsToDeal} cards to reach max hand size");
        
        // Call the card generator to deal the cards
        StartCoroutine(DealCardsCoroutine(cardsToDeal));
    }
    
    private IEnumerator DealCardsCoroutine(int cardsToDeal)
    {
        // Wait a moment before dealing cards for better visual flow
        yield return new WaitForSeconds(0.5f);
        
        // Use the CardGenerator to deal cards from the player's deck
        // This will randomly select cards from the PlayerDeck CSV data
        // and add them to the player's hand up to the maximum hand size
        
        // Generate the cards using our updated CardGenerator implementation
        cardGenerator.DealCardsFromDeck(cardsToDeal);
        
        Debug.Log($"[EndTurnButton] Dealt {cardsToDeal} cards to player's hand");
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
