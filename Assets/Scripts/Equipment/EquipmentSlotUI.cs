using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private ItemType type;
    [SerializeField] private Image icon;


    private void Start()
    {
        EquipmentManager.Instance.OnSuccessfulEquipmentChange += UpdateEquipmentSlot;
    }

    private void UpdateEquipmentSlot(ItemType itemType)
    {
        if (itemType != type) return;

        ItemSO item = EquipmentManager.Instance.GetItem(type);

        if (item != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
        }
        else
        {
            icon.enabled = false;
        }
    }

    private void OnDestroy()
    {
        EquipmentManager.Instance.OnSuccessfulEquipmentChange -= UpdateEquipmentSlot;
    }
}
