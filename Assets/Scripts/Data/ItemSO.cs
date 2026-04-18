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

public enum WeaponAttackStyle
{
    Melee,
    Ranged
}

public enum ItemTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
    Tier4 = 4
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
    public ItemTier tier = ItemTier.Tier1;
    public int cost = 0;
    public bool isCursed;
    public EquipmentSubtype equipmentSubtype;
    public WeaponHandedness weaponHandedness = WeaponHandedness.None;
    public WeaponAttackStyle weaponAttackStyle = WeaponAttackStyle.Melee;
    public bool grantsHealAction;
    public GameObject projectilePrefab;
    public bool cleavesAdjacentEnemies;
    public GameObject meleeAttackVfxPrefab;
    public StatusEffectSO attackHitStatusEffect;
    public StatusEffectSO rangedHitStatusEffect;
    [Range(0f, 1f)] public float selfStatusEffectChanceOnAttack;
    public StatusEffectSO selfStatusEffectOnAttack;
    public int consumableHealAmount;
    public ItemStatBlockData stats;
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
