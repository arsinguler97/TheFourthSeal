using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [SerializeField] List<ItemSO> rewardItemPool = new List<ItemSO>();

    readonly Dictionary<LoadoutSlotType, ItemSO> _equippedItems = new Dictionary<LoadoutSlotType, ItemSO>();
    bool _isLoadoutLockedForCurrentRoom;

    public event Action<LoadoutSlotType> OnLoadoutSlotChanged;
    public event Action<bool> OnLoadoutLockChanged;

    public bool IsLoadoutLockedForCurrentRoom => _isLoadoutLockedForCurrentRoom;
    public bool HasPendingLoadoutChoice => GetEquippedItem(LoadoutSlotType.Spare) != null;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSlots();
        ResetEquipmentState();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void InitializeSlots()
    {
        foreach (LoadoutSlotType slotType in Enum.GetValues(typeof(LoadoutSlotType)))
        {
            if (slotType == LoadoutSlotType.None)
                continue;

            _equippedItems[slotType] = null;
        }
    }

    public ItemSO GetEquippedItem(LoadoutSlotType slotType)
    {
        return _equippedItems.TryGetValue(slotType, out ItemSO item) ? item : null;
    }

    public void ResetEquipmentState()
    {
        List<LoadoutSlotType> slotTypes = new List<LoadoutSlotType>(_equippedItems.Keys);
        for (int i = 0; i < slotTypes.Count; i++)
            _equippedItems[slotTypes[i]] = null;

        _isLoadoutLockedForCurrentRoom = false;
        NotifyAllSlotsChanged();
        OnLoadoutLockChanged?.Invoke(false);
        ApplyEquippedStatsToCurrentPlayer();
    }

    public void BeginRoomLoadoutPhase()
    {
        _isLoadoutLockedForCurrentRoom = false;
        OnLoadoutLockChanged?.Invoke(false);
        ApplyEquippedStatsToCurrentPlayer();
    }

    public void LockLoadoutForCurrentRoom()
    {
        _isLoadoutLockedForCurrentRoom = true;
        OnLoadoutLockChanged?.Invoke(true);
        ApplyEquippedStatsToCurrentPlayer();
    }

    public bool TryStoreRewardInSpare(ItemSO item)
    {
        return TryStoreRewardInSpare(item, false);
    }

    public bool CanAddPurchasedItem(ItemSO item)
    {
        if (item == null)
            return false;

        LoadoutSlotType defaultSlot = item.GetDefaultLoadoutSlot();
        if (defaultSlot == LoadoutSlotType.None)
            return false;

        if (GetEquippedItem(defaultSlot) == null && CanAssignItemToSlot(item, defaultSlot))
            return true;

        return GetEquippedItem(LoadoutSlotType.Spare) == null && CanAssignItemToSlot(item, LoadoutSlotType.Spare);
    }

    public bool TryAddPurchasedItem(ItemSO item)
    {
        if (item == null)
            return false;

        LoadoutSlotType defaultSlot = item.GetDefaultLoadoutSlot();
        if (defaultSlot == LoadoutSlotType.None)
            return false;

        if (GetEquippedItem(defaultSlot) == null && TryAssignItemToSlot(item, defaultSlot))
            return true;

        return GetEquippedItem(LoadoutSlotType.Spare) == null && TryAssignItemToSlot(item, LoadoutSlotType.Spare);
    }

    public bool TryGrantRandomReward()
    {
        if (rewardItemPool == null || rewardItemPool.Count == 0)
            return false;

        List<ItemSO> validRewardItems = new List<ItemSO>();
        for (int i = 0; i < rewardItemPool.Count; i++)
        {
            if (rewardItemPool[i] != null)
                validRewardItems.Add(rewardItemPool[i]);
        }

        if (validRewardItems.Count == 0)
            return false;

        ItemSO randomRewardItem = validRewardItems[UnityEngine.Random.Range(0, validRewardItems.Count)];
        return TryStoreRewardInSpare(randomRewardItem, true);
    }

    bool TryStoreRewardInSpare(ItemSO item, bool ignoreLoadoutLock)
    {
        if (item == null || (_isLoadoutLockedForCurrentRoom && !ignoreLoadoutLock))
            return false;

        if (GetEquippedItem(LoadoutSlotType.Spare) != null)
            return false;

        _equippedItems[LoadoutSlotType.Spare] = item;
        NotifySlotChanged(LoadoutSlotType.Spare);
        return true;
    }

    public bool TryMoveOrSwap(LoadoutSlotType fromSlot, LoadoutSlotType targetSlot)
    {
        if (_isLoadoutLockedForCurrentRoom || fromSlot == targetSlot)
            return false;

        if (!_equippedItems.ContainsKey(fromSlot) || !_equippedItems.ContainsKey(targetSlot))
            return false;

        ItemSO movingItem = GetEquippedItem(fromSlot);
        if (movingItem == null)
            return false;

        if (!CanPlaceItemInSlot(movingItem, targetSlot))
            return false;

        ItemSO targetItem = GetEquippedItem(targetSlot);
        if (targetItem != null && !CanPlaceItemInSlot(targetItem, fromSlot))
            return false;

        if (targetSlot == LoadoutSlotType.Shield && IsTwoHandedWeaponEquipped() && fromSlot != LoadoutSlotType.Weapon)
            return false;

        if (movingItem.type == ItemType.Weapon
            && movingItem.weaponHandedness == WeaponHandedness.TwoHanded
            && targetSlot == LoadoutSlotType.Weapon)
        {
            if (targetItem != null && targetItem.type == ItemType.Equipment && targetItem.equipmentSubtype == EquipmentSubtype.Shield)
                return false;

            if (fromSlot != LoadoutSlotType.Shield)
                DeleteItemInSlot(LoadoutSlotType.Shield);
        }

        _equippedItems[fromSlot] = targetItem;
        _equippedItems[targetSlot] = movingItem;

        NotifySlotChanged(fromSlot);
        NotifySlotChanged(targetSlot);

        if (targetSlot == LoadoutSlotType.Weapon || targetSlot == LoadoutSlotType.Shield)
        {
            NotifySlotChanged(LoadoutSlotType.Weapon);
            NotifySlotChanged(LoadoutSlotType.Shield);
        }

        ApplyEquippedStatsToCurrentPlayer();
        Debug.Log($"Moved {movingItem.itemName} from {fromSlot} to {targetSlot}.");
        return true;
    }

    public bool DeleteItemInSlot(LoadoutSlotType slotType)
    {
        if (_isLoadoutLockedForCurrentRoom || !_equippedItems.ContainsKey(slotType))
            return false;

        if (_equippedItems[slotType] == null)
            return false;

        _equippedItems[slotType] = null;
        NotifySlotChanged(slotType);
        ApplyEquippedStatsToCurrentPlayer();
        return true;
    }

    public void ApplyEquippedStatsToCurrentPlayer()
    {
        PlayerUnit playerUnit = CombatManager.I != null ? CombatManager.I.PlayerUnit : null;
        if (playerUnit == null)
            return;

        ApplyEquippedStatsToPlayer(playerUnit);
    }

    public void ApplyEquippedStatsToPlayer(PlayerUnit playerUnit)
    {
        if (playerUnit == null)
            return;

        List<StatModifierData> modifiers = new List<StatModifierData>();
        foreach (KeyValuePair<LoadoutSlotType, ItemSO> pair in _equippedItems)
        {
            if (pair.Key == LoadoutSlotType.Spare
                || pair.Key == LoadoutSlotType.Consumable
                || pair.Value == null
                || pair.Value.stats == null)
                continue;

            // Weapon attack overrides the player's damage die, so it should not stack as a flat stat modifier.
            bool includeAttackModifier = pair.Key != LoadoutSlotType.Weapon;
            modifiers.AddRange(pair.Value.stats.ToModifiers(includeAttackModifier));
        }

        playerUnit.SetModifiers(modifiers);
    }

    public bool IsTwoHandedWeaponEquipped()
    {
        ItemSO weapon = GetEquippedItem(LoadoutSlotType.Weapon);
        return weapon != null
            && weapon.type == ItemType.Weapon
            && weapon.weaponHandedness == WeaponHandedness.TwoHanded;
    }

    public ItemSO GetEquippedConsumable()
    {
        ItemSO consumable = GetEquippedItem(LoadoutSlotType.Consumable);
        return consumable != null && consumable.type == ItemType.Consumables ? consumable : null;
    }

    public bool ConsumeEquippedConsumable()
    {
        ItemSO consumable = GetEquippedConsumable();
        if (consumable == null)
            return false;

        _equippedItems[LoadoutSlotType.Consumable] = null;
        NotifySlotChanged(LoadoutSlotType.Consumable);
        ApplyEquippedStatsToCurrentPlayer();
        return true;
    }

    public int GetEquippedWeaponAttackOverride()
    {
        ItemSO weapon = GetEquippedItem(LoadoutSlotType.Weapon);
        if (weapon == null || weapon.type != ItemType.Weapon || weapon.stats == null)
            return 0;

        return Mathf.Max(0, weapon.stats.attack);
    }

    public ItemSO GetEquippedWeapon()
    {
        ItemSO weapon = GetEquippedItem(LoadoutSlotType.Weapon);
        return weapon != null && weapon.type == ItemType.Weapon ? weapon : null;
    }

    public IReadOnlyList<ItemSO> GetCombatActiveEquippedItems()
    {
        List<ItemSO> activeItems = new List<ItemSO>();
        foreach (KeyValuePair<LoadoutSlotType, ItemSO> pair in _equippedItems)
        {
            if (pair.Value == null)
                continue;

            if (pair.Key == LoadoutSlotType.Spare || pair.Key == LoadoutSlotType.Consumable)
                continue;

            activeItems.Add(pair.Value);
        }

        return activeItems;
    }

    public ItemSO GetEquippedWeaponThatGrantsHealAction()
    {
        ItemSO weapon = GetEquippedWeapon();
        return weapon != null && weapon.grantsHealAction ? weapon : null;
    }

    public WeaponAttackStyle GetEquippedWeaponAttackStyle()
    {
        ItemSO weapon = GetEquippedItem(LoadoutSlotType.Weapon);
        if (weapon == null || weapon.type != ItemType.Weapon)
            return WeaponAttackStyle.Melee;

        return weapon.weaponAttackStyle;
    }

    public GameObject GetEquippedWeaponProjectilePrefab()
    {
        ItemSO weapon = GetEquippedItem(LoadoutSlotType.Weapon);
        if (weapon == null || weapon.type != ItemType.Weapon || weapon.weaponAttackStyle != WeaponAttackStyle.Ranged)
            return null;

        return weapon.projectilePrefab;
    }

    bool CanPlaceItemInSlot(ItemSO item, LoadoutSlotType slotType)
    {
        if (item == null)
            return false;

        if (slotType == LoadoutSlotType.Spare)
            return true;

        return item.CanEquipToSlot(slotType);
    }

    bool CanAssignItemToSlot(ItemSO item, LoadoutSlotType slotType)
    {
        if (item == null || !_equippedItems.ContainsKey(slotType))
            return false;

        if (!CanPlaceItemInSlot(item, slotType))
            return false;

        if (slotType == LoadoutSlotType.Shield && IsTwoHandedWeaponEquipped())
            return false;

        return true;
    }

    bool TryAssignItemToSlot(ItemSO item, LoadoutSlotType slotType)
    {
        if (!CanAssignItemToSlot(item, slotType))
            return false;

        bool removedShield = false;
        if (item.type == ItemType.Weapon
            && item.weaponHandedness == WeaponHandedness.TwoHanded
            && slotType == LoadoutSlotType.Weapon
            && GetEquippedItem(LoadoutSlotType.Shield) != null)
        {
            _equippedItems[LoadoutSlotType.Shield] = null;
            removedShield = true;
        }

        _equippedItems[slotType] = item;
        NotifySlotChanged(slotType);

        if (removedShield)
            NotifySlotChanged(LoadoutSlotType.Shield);

        ApplyEquippedStatsToCurrentPlayer();
        return true;
    }

    void NotifyAllSlotsChanged()
    {
        foreach (LoadoutSlotType slotType in _equippedItems.Keys)
            NotifySlotChanged(slotType);
    }

    void NotifySlotChanged(LoadoutSlotType slotType)
    {
        OnLoadoutSlotChanged?.Invoke(slotType);
    }
}
