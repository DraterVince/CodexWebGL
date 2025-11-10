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
        StartCoroutine(Randomize());
    }

    public IEnumerator Randomize()
    {
        if (counter < 0 || counter >= cardContainer.Count || counter >= cardDisplayContainer.Count)
        {
            Debug.LogWarning($"CardManager counter {counter} is out of range. Cannot randomize cards.");
            yield break;
        }

        chosenCards = new List<Item>(cardContainer[counter].cards);

        for (int i = 0; i < cardDisplayContainer[counter].cardDisplay.Count; i++)
        {
            int rand = Random.Range(0, chosenCards.Count);

            cardDisplayContainer[counter].cardDisplay[i].cardName = chosenCards[rand].cardName;
            cardDisplayContainer[counter].cardDisplay[i].transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = chosenCards[rand].cardName;
            cardDisplayContainer[counter].cardDisplay[i].cardDesign.sprite = chosenCards[rand].artwork;

            yield return new WaitForSeconds(0.15f);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.SetActive(true);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.localScale = Vector3.one;

            chosenCards.RemoveAt(rand);
        }
    }

    public void ResetCards()
    {
        chosenCards.Clear();

        if (counter < 0 || counter >= cardDisplayContainer.Count)
        {
            Debug.LogWarning($"CardManager counter {counter} is out of range. CardDisplayContainer count: {cardDisplayContainer.Count}");
            return;
        }

        // Reset cards in the display container (grid area)
        for (int i = 0; i < cardDisplayContainer[counter].cardDisplay.Count; i++)
        {
            cardDisplayContainer[counter].cardDisplay[i].gameObject.SetActive(false);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.SetParent(grid.transform);
            cardDisplayContainer[counter].cardDisplay[i].gameObject.transform.localScale = Vector3.one;
        }

        // Also check and clear ANY cards stuck in PlayedCard holders
        // Search for all possible PlayedCard holders in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cardsCleared = 0;

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
