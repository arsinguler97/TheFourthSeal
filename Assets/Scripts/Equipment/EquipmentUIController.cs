using UnityEngine;
using UnityEngine.UI;

public class EquipmentUIController : MonoBehaviour
{
    public static EquipmentUIController Instance;

    [SerializeField] private GameObject equipmentInventoryUI;

    [SerializeField] private ConfirmationUI keepUI;
    [SerializeField] private ConfirmationUI replaceUI;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EquipmentManager.Instance.OnRequestEquipConfirmation += ShowKeepPrompt;
        EquipmentManager.Instance.OnRequestReplaceConfirmation += ShowReplacePrompt;
    }

    public void ShowEquipmentInventory(bool setActive)
    {
        equipmentInventoryUI.SetActive(setActive);
    }

    private void ShowKeepPrompt(ItemSO item, System.Action<bool> callback)
    {
        keepUI.Show(callback);
    }

    private void ShowReplacePrompt(ItemSO item, System.Action<bool> callback)
    {
        replaceUI.Show(callback);
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance)
        {
            EquipmentManager.Instance.OnRequestEquipConfirmation -= ShowKeepPrompt;
            EquipmentManager.Instance.OnRequestReplaceConfirmation -= ShowReplacePrompt;
        }
    }
}