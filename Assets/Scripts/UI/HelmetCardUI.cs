using UnityEngine;

public class HelmetCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item != null && (item.type != ItemType.Equipment || item.equipmentSubtype != EquipmentSubtype.Helmet))
            Debug.LogWarning($"HelmetCardUI received incompatible item '{item.itemName}'.");
    }
}
