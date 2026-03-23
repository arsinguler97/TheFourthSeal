using UnityEngine;

public class BootsCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item != null && (item.type != ItemType.Equipment || item.equipmentSubtype != EquipmentSubtype.Boots))
            Debug.LogWarning($"BootsCardUI received incompatible item '{item.itemName}'.");
    }
}
