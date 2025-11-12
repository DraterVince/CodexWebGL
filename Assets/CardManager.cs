using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public int counter;
    public List<CardListContainer> cardContainer = new List<CardListContainer>();
    public List<CardDisplayContainer> cardDisplayContainer = new List<CardDisplayContainer>();

    private List<Item> chosenCards = new List<Item>();
    private bool isRandomizing = false; // Prevent multiple simultaneous randomizations
    private Coroutine currentRandomizeCoroutine = null; // Track active coroutine

    [System.Serializable]
    public class CardListContainer
    {
        public List<Item> cards = new List<Item>();
    }
    [System.Serializable]
    public class CardDisplayContainer
    {
        public List<CardDisplay> cardDisplay = new List<CardDisplay>();
    }

    public GameObject grid;
    private void Start()
    {
        Time.timeScale = 1f;
        
        // In multiplayer mode, don't auto-randomize
        // The turn system will handle randomization via RPC_SyncAllCounters
        bool isMultiplayer = Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom;
        
        if (!isMultiplayer)
        {
            currentRandomizeCoroutine = StartCoroutine(Randomize());
        }
        else
        {
            Debug.Log("[CardManager] Multiplayer mode detected - skipping auto-randomization (turn system will handle it)");
        }
    }

    /// <summary>
    /// Force stop any ongoing randomization (useful for multiplayer sync)
    /// </summary>
    public void CancelRandomization()
    {
        if (isRandomizing && currentRandomizeCoroutine != null)
        {
            Debug.Log("[CardManager] Canceling ongoing randomization");
            StopCoroutine(currentRandomizeCoroutine);
            isRandomizing = false;
            currentRandomizeCoroutine = null;
        }
    }
    
    /// <summary>
    /// Start randomization and track the coroutine (for multiplayer)
    /// </summary>
    public void StartRandomization()
    {
        currentRandomizeCoroutine = StartCoroutine(Randomize());
    }
    
    public IEnumerator Randomize()
    {
        // Prevent multiple simultaneous randomizations
        if (isRandomizing)
        {
            Debug.LogWarning($"[CardManager] Randomize already in progress - skipping duplicate call");
            yield break;
        }
        
        isRandomizing = true;
        
        Debug.Log($"[CardManager] ===== RANDOMIZE STARTED =====");
        Debug.Log($"[CardManager] Counter: {counter}");
        Debug.Log($"[CardManager] cardContainer.Count: {cardContainer.Count}");
        Debug.Log($"[CardManager] cardDisplayContainer.Count: {cardDisplayContainer.Count}");
        
        if (counter < 0 || counter >= cardContainer.Count || counter >= cardDisplayContainer.Count)
        {
            Debug.LogWarning($"CardManager counter {counter} is out of range. Cannot randomize cards.");
            isRandomizing = false;
            currentRandomizeCoroutine = null;
            yield break;
        }

        Debug.Log($"[CardManager] cardContainer[{counter}].cards.Count: {cardContainer[counter].cards.Count}");
        Debug.Log($"[CardManager] cardDisplayContainer[{counter}].cardDisplay.Count: {cardDisplayContainer[counter].cardDisplay.Count}");
        
        // Use all cards from the container - ensure each card object is only used once
        chosenCards = new List<Item>();
        HashSet<Item> seenCardObjects = new HashSet<Item>(); // Track by object reference, not name
        
        // Log all cards in container and filter out duplicate object references
        Debug.Log($"[CardManager] === CARD CONTAINER ANALYSIS ===");
        Debug.Log($"[CardManager] cardContainer[{counter}].cards.Count: {cardContainer[counter].cards.Count}");
        for (int idx = 0; idx < cardContainer[counter].cards.Count; idx++)
        {
            var card = cardContainer[counter].cards[idx];
            if (card != null)
            {
                // Only add if we haven't seen this exact card object before
                if (seenCardObjects.Add(card))
                {
                    Debug.Log($"[CardManager]   Card[{idx}]: {card.cardName} (Item: {card.name}) - ADDED");
                    chosenCards.Add(card);
                }
                else
                {
                    Debug.LogWarning($"[CardManager]   Card[{idx}]: {card.cardName} (Item: {card.name}) - DUPLICATE OBJECT REFERENCE, SKIPPED");
                }
            }
            else
            {
                Debug.LogWarning($"[CardManager]   Card[{idx}]: NULL!");
            }
        }
        Debug.Log($"[CardManager] === END ANALYSIS ===");
        
        Debug.Log($"[CardManager] chosenCards populated with {chosenCards.Count} unique card objects (from {cardContainer[counter].cards.Count} total)");
        
        // Check if we have enough cards
        int cardsNeeded = cardDisplayContainer[counter].cardDisplay.Count;
        if (chosenCards.Count < cardsNeeded)
        {
            Debug.LogError($"[CardManager] NOT ENOUGH UNIQUE CARDS! Need {cardsNeeded}, but only have {chosenCards.Count} unique card objects in cardContainer[{counter}]");
            isRandomizing = false;
            currentRandomizeCoroutine = null;
            yield break;
        }
        
        // Track selected card objects to ensure no duplicates
        HashSet<Item> selectedCardObjects = new HashSet<Item>();
        
        for (int i = 0; i < cardDisplayContainer[counter].cardDisplay.Count; i++)
        {
            if (chosenCards.Count == 0)
            {
                Debug.LogError($"[CardManager] Ran out of cards at index {i}! This shouldn't happen.");
                break;
            }
            
            Debug.Log($"[CardManager] Randomizing card {i + 1}/{cardDisplayContainer[counter].cardDisplay.Count}");
            Debug.Log($"[CardManager]   Card GameObject: {cardDisplayContainer[counter].cardDisplay[i].gameObject.name}");
            Debug.Log($"[CardManager]   Card Parent: {cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.parent?.name ?? "NULL"}");
            Debug.Log($"[CardManager]   Card Active: {cardDisplayContainer[counter].cardDisplay[i].gameObject.activeSelf}");
            Debug.Log($"[CardManager]   Cards remaining in pool: {chosenCards.Count}");
            
            int rand = Random.Range(0, chosenCards.Count);
            Item selectedCard = chosenCards[rand];
            string selectedCardName = selectedCard.cardName;
            
            // Verify we haven't selected this card object before (shouldn't happen with RemoveAt, but double-check)
            if (selectedCardObjects.Contains(selectedCard))
            {
                Debug.LogError($"[CardManager] CRITICAL ERROR: Card object '{selectedCardName}' was already selected! This indicates a bug in the randomization logic.");
                // Skip this card and try the next one
                chosenCards.RemoveAt(rand);
                i--; // Retry this index
                continue;
            }
            selectedCardObjects.Add(selectedCard);

            cardDisplayContainer[counter].cardDisplay[i].cardName = selectedCardName;
            cardDisplayContainer[counter].cardDisplay[i].transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = selectedCardName;
            cardDisplayContainer[counter].cardDisplay[i].cardDesign.sprite = selectedCard.artwork;

            yield return new WaitForSeconds(0.15f);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.SetActive(true);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.localScale = Vector3.one;
            
            Debug.Log($"[CardManager]   ✓ Card activated: {selectedCardName} (Object: {selectedCard.name}, Index: {rand})");

            // Remove the selected card from the pool so it can't be selected again
            chosenCards.RemoveAt(rand);
        }
        
        List<string> selectedNames = new List<string>();
        foreach (var card in selectedCardObjects)
        {
            selectedNames.Add(card.cardName);
        }
        Debug.Log($"[CardManager] Final selected card names: {string.Join(", ", selectedNames)}");
        
        isRandomizing = false;
        currentRandomizeCoroutine = null;
        Debug.Log($"[CardManager] ===== RANDOMIZE COMPLETE ({cardDisplayContainer[counter].cardDisplay.Count} cards activated) =====");
    }

    public void ResetCards()
    {
        Debug.Log($"[CardManager] ===== RESET CARDS STARTED =====");
        Debug.Log($"[CardManager] Counter: {counter}");
        
        chosenCards.Clear();

        if (counter < 0 || counter >= cardDisplayContainer.Count)
        {
            Debug.LogWarning($"CardManager counter {counter} is out of range. CardDisplayContainer count: {cardDisplayContainer.Count}");
            return;
        }

        Debug.Log($"[CardManager] cardDisplayContainer[{counter}].cardDisplay.Count: {cardDisplayContainer[counter].cardDisplay.Count}");
        
        // Reset cards in the display container (grid area)
        for (int i = 0; i < cardDisplayContainer[counter].cardDisplay.Count; i++)
        {
            GameObject cardObj = cardDisplayContainer[counter].cardDisplay[i].gameObject;
            Debug.Log($"[CardManager] Resetting card {i}: {cardObj.name} (Parent: {cardObj.transform.parent?.name ?? "NULL"}, Active: {cardObj.activeSelf})");
            
            // Clear card data to prevent stale values
            cardDisplayContainer[counter].cardDisplay[i].cardName = "";
            
            // Clear text display
            try
            {
                var textComponent = cardDisplayContainer[counter].cardDisplay[i].transform.GetChild(0)?.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CardManager] Could not clear text for card {i}: {ex.Message}");
            }
            
            cardDisplayContainer[counter].cardDisplay[i].gameObject.SetActive(false);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.SetParent(grid.transform);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.localScale = Vector3.one;
            
            Debug.Log($"[CardManager]   ✓ Moved to grid, deactivated, data cleared");
        }

        // Also check and clear ANY cards stuck in PlayedCard holders
        // Search for all possible PlayedCard holders in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cardsCleared = 0;

        Debug.Log($"[CardManager] Searching for PlayedCard holders...");
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "PlayedCard" && obj.transform.childCount > 0)
            {
                Debug.Log($"[CardManager] Found PlayedCard holder '{obj.name}' with {obj.transform.childCount} cards at path: {GetGameObjectPath(obj)}");

                // Move all children back to grid
                while (obj.transform.childCount > 0)
                {
                    Transform child = obj.transform.GetChild(0);
                    Debug.Log($"[CardManager] Moving card '{child.name}' back to grid");
                    child.SetParent(grid.transform);
                    child.gameObject.SetActive(false);
                    child.localScale = Vector3.one;
                    cardsCleared++;
                }
            }
        }

        if (cardsCleared > 0)
        {
            Debug.Log($"[CardManager] Successfully cleared {cardsCleared} cards from PlayedCard holders");
        }
        else
        {
            Debug.Log($"[CardManager] No cards found in PlayedCard holders");
        }
        
        Debug.Log($"[CardManager] ===== RESET CARDS COMPLETE =====");
    }

    /// <summary>
    /// Helper method to get full hierarchy path of a GameObject for debugging
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
