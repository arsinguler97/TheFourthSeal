using System;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentUIController : MonoBehaviour
{
    public static EquipmentUIController Instance;
    public event Action OnInventoryClosed;

    enum InventoryInteractionMode
    {
        None,
        DeleteSelection
    }

    [SerializeField] GameObject equipmentInventoryUI;
    [SerializeField] ConfirmationUI roomLoadoutConfirmationUI;
    [SerializeField] Button confirmLoadoutButton;
    [SerializeField] Button deleteSelectedItemButton;

    LoadoutSlotType _selectedSlotType = LoadoutSlotType.None;
    InventoryInteractionMode _interactionMode = InventoryInteractionMode.None;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        equipmentInventoryUI.SetActive(false);
    }

    public void OpenEquipmentInventory()
    {
        if (EquipmentManager.Instance == null)
            return;

        EquipmentManager.Instance.OnLoadoutLockChanged += HandleLoadoutLockChanged;
        EquipmentManager.Instance.BeginRoomLoadoutPhase();

        if (confirmLoadoutButton != null)
            confirmLoadoutButton.onClick.AddListener(ConfirmCurrentRoomLoadout);

        if (deleteSelectedItemButton != null)
            deleteSelectedItemButton.onClick.AddListener(DeleteSelectedItem);

        bool shouldOpenLoadoutPanel = EquipmentManager.Instance.HasPendingLoadoutChoice;
        if (!shouldOpenLoadoutPanel)
            EquipmentManager.Instance.LockLoadoutForCurrentRoom();

        ShowEquipmentInventory(shouldOpenLoadoutPanel);
        HandleLoadoutLockChanged(EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom);
    }

    public void ShowEquipmentInventory(bool setActive)
    {
        if (equipmentInventoryUI != null)
            equipmentInventoryUI.SetActive(setActive);

        if (!setActive)
            OnInventoryClosed?.Invoke();
    }

    public void SelectLoadoutSlot(LoadoutSlotType slotType)
    {
        if (EquipmentManager.Instance == null)
            return;

        if (_interactionMode == InventoryInteractionMode.DeleteSelection)
        {
            TryDeleteSlotWithConfirmation(slotType);
            return;
        }

        _selectedSlotType = slotType;
        RefreshDeleteButtonState();
    }

    public void TryMoveSelectedItemToSlot(LoadoutSlotType slotType)
    {
        if (EquipmentManager.Instance == null || _selectedSlotType == LoadoutSlotType.None)
            return;

        if (EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        if (slotType == _selectedSlotType)
        {
            RefreshDeleteButtonState();
            return;
        }

        bool moved = EquipmentManager.Instance.TryMoveOrSwap(_selectedSlotType, slotType);
        if (!moved)
            return;

        _selectedSlotType = slotType;
        RefreshDeleteButtonState();
    }

    public void ConfirmCurrentRoomLoadout()
    {
        if (EquipmentManager.Instance == null || EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        if (roomLoadoutConfirmationUI == null)
        {
            EquipmentManager.Instance.LockLoadoutForCurrentRoom();
            ShowEquipmentInventory(false);
            if (TurnManager.I != null)
                TurnManager.I.ResumeCurrentTurnAfterLoadoutSelection();
            return;
        }

        roomLoadoutConfirmationUI.Show(accepted =>
        {
            if (!accepted)
                return;

            EquipmentManager.Instance.LockLoadoutForCurrentRoom();
            ShowEquipmentInventory(false);
            if (TurnManager.I != null)
                TurnManager.I.ResumeCurrentTurnAfterLoadoutSelection();
        });
    }

    public void StartRun()
    {
        ConfirmCurrentRoomLoadout();
    }

    public void DeleteSelectedItem()
    {
        if (EquipmentManager.Instance == null || EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        _interactionMode = _interactionMode == InventoryInteractionMode.DeleteSelection
            ? InventoryInteractionMode.None
            : InventoryInteractionMode.DeleteSelection;
        RefreshDeleteButtonState();
    }

    void HandleLoadoutLockChanged(bool isLocked)
    {
        if (confirmLoadoutButton != null)
            confirmLoadoutButton.interactable = !isLocked;

        if (isLocked)
            _interactionMode = InventoryInteractionMode.None;

        RefreshDeleteButtonState();
    }

    void RefreshDeleteButtonState()
    {
        if (deleteSelectedItemButton == null || EquipmentManager.Instance == null)
            return;

        deleteSelectedItemButton.interactable = !EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom;
        deleteSelectedItemButton.image.color = _interactionMode == InventoryInteractionMode.DeleteSelection
            ? new Color(1f, 0.55f, 0.55f, 1f)
            : Color.white;
    }

    void TryDeleteSlotWithConfirmation(LoadoutSlotType slotType)
    {
        if (EquipmentManager.Instance == null || EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        ItemSO item = EquipmentManager.Instance.GetEquippedItem(slotType);
        if (item == null)
        {
            _interactionMode = InventoryInteractionMode.None;
            RefreshDeleteButtonState();
            return;
        }

        if (roomLoadoutConfirmationUI == null)
        {
            EquipmentManager.Instance.DeleteItemInSlot(slotType);
            FinishDeleteSelection(slotType);
            return;
        }

        roomLoadoutConfirmationUI.Show(accepted =>
        {
            if (accepted)
                EquipmentManager.Instance.DeleteItemInSlot(slotType);

            FinishDeleteSelection(slotType);
        });
    }

    void FinishDeleteSelection(LoadoutSlotType slotType)
    {
        _interactionMode = InventoryInteractionMode.None;
        _selectedSlotType = slotType;
        RefreshDeleteButtonState();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutLockChanged -= HandleLoadoutLockChanged;

        if (confirmLoadoutButton != null)
            confirmLoadoutButton.onClick.RemoveListener(ConfirmCurrentRoomLoadout);

        if (deleteSelectedItemButton != null)
            deleteSelectedItemButton.onClick.RemoveListener(DeleteSelectedItem);
    }
}
