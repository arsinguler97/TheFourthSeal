using UnityEngine;

public enum ItemType
{
    Weapon,
    Equipment,
    Consumables
}

[CreateAssetMenu(fileName = "NewItem", menuName = "ScriptableObjects/ItemDefinition")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public ItemType type;
    public ItemStatBlockData stats;
    public Sprite icon;
    public Sprite card;
}