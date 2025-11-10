using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CardManager cardManager;

    public string cardName;
    public Image cardDesign;

    [SerializeField] GameObject Card;

    private void Update()
    {
        gameObject.name = cardName;
    }
}