using UnityEngine;
using UnityEngine.InputSystem;

public class TEMPFORTEST : MonoBehaviour
{
    [SerializeField] private ItemSO item;


    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (EquipmentUIController.Instance)
            {
                EquipmentUIController.Instance.ShowEquipmentInventory(true);
            }
        }
        else if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (EquipmentUIController.Instance)
            {
                EquipmentUIController.Instance.ShowEquipmentInventory(false);
            }
        }
        else if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (EquipmentManager.Instance)
            {
                EquipmentManager.Instance.TryStoreRewardInSpare(item);
            }
        }
    }
}
