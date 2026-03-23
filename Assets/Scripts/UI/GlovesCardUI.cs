using UnityEngine;

public class GlovesCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item != null && (item.type != ItemType.Equipment || item.equipmentSubtype != EquipmentSubtype.Gloves))
            Debug.LogWarning($"GlovesCardUI received incompatible item '{item.itemName}'.");
    }
}
