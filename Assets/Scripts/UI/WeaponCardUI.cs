using UnityEngine;

public class WeaponCardUI : ItemCardUI
{
    public override void Bind(ItemSO item)
    {
        base.Bind(item);

        if (item == null)
            return;

        if (item.type != ItemType.Weapon)
            Debug.LogWarning($"WeaponCardUI received non-weapon item '{item.itemName}'.");
    }
}
