using UnityEngine;
using UnityEngine.UI;

public class CardVisual : MonoBehaviour
{
    public Image image;                // UI Image on this card
    public Sprite[] possibleSprites;   // Put card fronts here in the prefab

    [HideInInspector] public int value; // 1–13

    public int Value => value;         // <--- added

    // Called by the game to set this card
    public void Setup(int cardValue)
    {
        value = cardValue;

        if (possibleSprites != null && possibleSprites.Length > 0)
        {
            // Map 1..13 to sprite index 0..12
            int index = Mathf.Clamp(cardValue - 1, 0, possibleSprites.Length - 1);
            image.sprite = possibleSprites[index];
        }
    }
}
