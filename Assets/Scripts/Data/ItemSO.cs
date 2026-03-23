using UnityEngine;

public enum ItemType
{
    Weapon,
    Equipment,
    Consumables
}

public enum EquipmentSubtype
{
    None,
    Helmet,
    BodyArmor,
    Gloves,
    Boots,
    Shield
}

public enum WeaponHandedness
{
    None,
    OneHanded,
    TwoHanded
}

public enum LoadoutSlotType
{
    None,
    Helmet,
    BodyArmor,
    Gloves,
    Boots,
    Shield,
    Weapon,
    Consumable,
    Spare
}

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObjects/ItemDefinition")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public ItemType type;
    public EquipmentSubtype equipmentSubtype;
    public WeaponHandedness weaponHandedness = WeaponHandedness.None;
    public int consumableHealAmount;
    public ItemStatBlockData stats;
    public Sprite icon;
    public Sprite card;

    public LoadoutSlotType GetDefaultLoadoutSlot()
    {
        switch (type)
        {
            case ItemType.Weapon:
                return LoadoutSlotType.Weapon;
            case ItemType.Consumables:
                return LoadoutSlotType.Consumable;
            case ItemType.Equipment:
                switch (equipmentSubtype)
                {
                    case EquipmentSubtype.Helmet:
                        return LoadoutSlotType.Helmet;
                    case EquipmentSubtype.BodyArmor:
                        return LoadoutSlotType.BodyArmor;
                    case EquipmentSubtype.Gloves:
                        return LoadoutSlotType.Gloves;
                    case EquipmentSubtype.Boots:
                        return LoadoutSlotType.Boots;
                    case EquipmentSubtype.Shield:
                        return LoadoutSlotType.Shield;
                }
                break;
        }

        return LoadoutSlotType.None;
    }

    public bool CanEquipToSlot(LoadoutSlotType slotType)
    {
        if (slotType == LoadoutSlotType.Spare)
            return true;

        return GetDefaultLoadoutSlot() == slotType;
    }
}
