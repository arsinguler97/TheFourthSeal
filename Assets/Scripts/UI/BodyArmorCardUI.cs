using UnityEngine;

public class BodyArmorCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item == null)
            return;

        bool isBodyArmor = item.type == ItemType.Equipment && item.equipmentSubtype == EquipmentSubtype.BodyArmor;
        if (!isBodyArmor)
            Debug.LogWarning($"BodyArmorCardUI received non-body-armor item '{item.itemName}'.");
    }
}
