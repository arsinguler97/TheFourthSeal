using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceCanvas : MonoBehaviour
{
    [SerializeField] private Image image;
    [Header("Fallback Dice")]
    [SerializeField] private GameObject overflowDiceRoot;
    [SerializeField] private Image overflowDiceImage;
    [SerializeField] private TMP_Text overflowDiceText;

    public Image GetImage()
    {
        return image;
    }

    public void ShowSprite(Sprite sprite)
    {
        if (image != null)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;
        }

        if (overflowDiceRoot != null)
            overflowDiceRoot.SetActive(false);
    }

    public void ShowOverflowResult(int value)
    {
        if (image != null)
            image.gameObject.SetActive(false);

        if (overflowDiceRoot != null)
            overflowDiceRoot.SetActive(true);

        if (overflowDiceText != null)
            overflowDiceText.text = value.ToString();
    }

    public void ResetDisplay()
    {
        if (image != null)
            image.gameObject.SetActive(true);

        if (overflowDiceRoot != null)
            overflowDiceRoot.SetActive(false);
    }
}
