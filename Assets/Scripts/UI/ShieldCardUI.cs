using UnityEngine;

public class ShieldCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item != null && (item.type != ItemType.Equipment || item.equipmentSubtype != EquipmentSubtype.Shield))
            Debug.LogWarning($"ShieldCardUI received incompatible item '{item.itemName}'.");
    }
}
