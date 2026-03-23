using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCardUI : MonoBehaviour
{
    [SerializeField] protected GameObject cardRoot;
    [SerializeField] protected TMP_Text nameText;
    [SerializeField] protected Image portraitImage;
    [SerializeField] protected TMP_Text descriptionText;
    [SerializeField] protected TMP_Text attackText;
    [SerializeField] protected TMP_Text defenceText;
    [SerializeField] protected TMP_Text speedText;
    [SerializeField] protected TMP_Text strengthText;
    [SerializeField] protected TMP_Text rangeText;
    [SerializeField] protected TMP_Text actionPointsText;

    public virtual void Bind(ItemSO item)
    {
        bool hasItem = item != null;
        if (cardRoot != null)
            cardRoot.SetActive(hasItem);

        if (!hasItem)
            return;

        if (nameText != null)
            nameText.text = item.itemName;

        if (portraitImage != null)
        {
            Sprite sprite = item.card != null ? item.card : item.icon;
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        if (descriptionText != null)
            descriptionText.text = item.itemDescription;

        BindStatText(attackText, item.stats != null ? item.stats.attack : 0);
        BindStatText(defenceText, item.stats != null ? item.stats.defence : 0);
        BindStatText(speedText, item.stats != null ? item.stats.speed : 0);
        BindStatText(strengthText, item.stats != null ? item.stats.strength : 0);
        BindStatText(rangeText, item.stats != null ? item.stats.range : 0);
        BindStatText(actionPointsText, item.stats != null ? item.stats.actionPoints : 0);
    }

    protected void BindStatText(TMP_Text targetText, int value)
    {
        if (targetText == null)
            return;

        targetText.text = value != 0 ? value.ToString() : string.Empty;
    }
}
