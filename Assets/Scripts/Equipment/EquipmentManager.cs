using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    // Vars
    public static EquipmentManager Instance;

    private Dictionary<ItemType, ItemSO> _equippedItems = new Dictionary<ItemType, ItemSO>();

    public Action<ItemSO, Action<bool>> OnRequestEquipConfirmation;
    public Action<ItemSO, Action<bool>> OnRequestReplaceConfirmation;
    public Action<ItemType> OnSuccessfulEquipmentChange;


    // Funcs
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            _equippedItems[type] = null;
        }
    }

    public ItemSO GetItem(ItemType type)
    {
        return _equippedItems[type];
    }

    public void TryEquip(ItemSO newItem)
    {
        ItemSO currentItem = _equippedItems[newItem.type];

        if (currentItem == null)
        {
            // Ask - keep or discard
            OnRequestEquipConfirmation?.Invoke(newItem, (accepted) =>
            {
                if (accepted)
                    Equip(newItem);
            });
        }
        else
        {
            // Ask - replace
            OnRequestReplaceConfirmation?.Invoke(newItem, (accepted) =>
            {
                if (accepted)
                    Equip(newItem);
            });
        }
    }

    private void Equip(ItemSO item)
    {
        _equippedItems[item.type] = item;
        OnSuccessfulEquipmentChange?.Invoke(item.type);
        Debug.Log($"Equipped {item.itemName}");
    }
}