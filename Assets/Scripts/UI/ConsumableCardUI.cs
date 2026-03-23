using UnityEngine;

public class ConsumableCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item == null)
            return;

        if (item.type != ItemType.Consumables)
            Debug.LogWarning($"ConsumableCardUI received non-consumable item '{item.itemName}'.");
    }
}
