using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] List<ItemSO> shopItemPool = new List<ItemSO>();
    [SerializeField] List<ShopItemSlotUI> shopSlots = new List<ShopItemSlotUI>();
    [SerializeField] TMP_Text goldText;
    [SerializeField] ConfirmationUI confirmationUI;
    [SerializeField] Button exitButton;
    [SerializeField] string nextSceneName = "FloorScene2";
    [SerializeField] string goldLabelPrefix = "Gold: ";

    readonly List<ItemSO> _currentOffers = new List<ItemSO>();
    ShopItemSlotUI _pendingPurchaseSlot;

    void Awake()
    {
        if (shopSlots.Count == 0)
            shopSlots.AddRange(GetComponentsInChildren<ShopItemSlotUI>(true));
    }

    void OnEnable()
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] != null)
                shopSlots[i].Clicked += HandleShopSlotClicked;
        }

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitShop);
    }

    void Start()
    {
        EnsureInventoryVisible();
        GenerateOffers();
        RefreshGoldUI();
        RefreshShopUI();
    }

    void OnDisable()
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (shopSlots[i] != null)
                shopSlots[i].Clicked -= HandleShopSlotClicked;
        }

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitShop);
    }

    void GenerateOffers()
    {
        _currentOffers.Clear();

        List<ItemSO> availableItems = new List<ItemSO>();
        HashSet<ItemSO> seenItems = new HashSet<ItemSO>();
        for (int i = 0; i < shopItemPool.Count; i++)
        {
            ItemSO item = shopItemPool[i];
            if (item == null || !seenItems.Add(item))
                continue;

            availableItems.Add(item);
        }

        for (int i = availableItems.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (availableItems[i], availableItems[randomIndex]) = (availableItems[randomIndex], availableItems[i]);
        }

        int offerCount = Mathf.Min(shopSlots.Count, availableItems.Count);
        for (int i = 0; i < offerCount; i++)
            _currentOffers.Add(availableItems[i]);
    }

    void RefreshShopUI()
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            ShopItemSlotUI slot = shopSlots[i];
            if (slot == null)
                continue;

            ItemSO item = i < _currentOffers.Count ? _currentOffers[i] : null;
            bool canBuy = item != null
                && PlayerWallet.I != null
                && PlayerWallet.I.SufficientFunds(item.cost)
                && EquipmentManager.Instance != null
                && EquipmentManager.Instance.CanAddPurchasedItem(item);

            if (item == null)
                slot.Clear();
            else
                slot.Bind(item, canBuy);
        }
    }

    void RefreshGoldUI()
    {
        if (goldText == null)
            return;

        int currentGold = PlayerWallet.I != null ? PlayerWallet.I.CurrentGold : 0;
        goldText.text = $"{goldLabelPrefix}{currentGold}";
    }

    void HandleShopSlotClicked(ShopItemSlotUI slot)
    {
        if (slot == null)
            return;

        ItemSO item = slot.GetBoundItem();
        if (item == null || PlayerWallet.I == null || EquipmentManager.Instance == null)
            return;

        if (!PlayerWallet.I.SufficientFunds(item.cost))
            return;

        if (!EquipmentManager.Instance.CanAddPurchasedItem(item))
            return;

        _pendingPurchaseSlot = slot;

        if (confirmationUI == null)
        {
            ConfirmPurchase(true);
            return;
        }

        confirmationUI.Show(ConfirmPurchase);
    }

    void ConfirmPurchase(bool accepted)
    {
        if (!accepted)
        {
            _pendingPurchaseSlot = null;
            return;
        }

        if (_pendingPurchaseSlot == null)
            return;

        ItemSO item = _pendingPurchaseSlot.GetBoundItem();
        if (item == null || PlayerWallet.I == null || EquipmentManager.Instance == null)
        {
            _pendingPurchaseSlot = null;
            RefreshShopUI();
            return;
        }

        bool canAfford = PlayerWallet.I.SufficientFunds(item.cost);
        bool canStore = EquipmentManager.Instance.CanAddPurchasedItem(item);
        if (!canAfford || !canStore)
        {
            _pendingPurchaseSlot = null;
            RefreshShopUI();
            return;
        }

        bool stored = EquipmentManager.Instance.TryAddPurchasedItem(item);
        if (!stored)
        {
            _pendingPurchaseSlot = null;
            RefreshShopUI();
            return;
        }

        if (!PlayerWallet.I.MakeTransaction(item.cost))
        {
            Debug.LogWarning($"Purchase of '{item.itemName}' was stored but wallet transaction failed.");
            _pendingPurchaseSlot = null;
            RefreshGoldUI();
            RefreshShopUI();
            return;
        }

        RemoveOffer(_pendingPurchaseSlot);
        _pendingPurchaseSlot = null;
        RefreshGoldUI();
        RefreshShopUI();
    }

    void RemoveOffer(ShopItemSlotUI slot)
    {
        if (slot == null)
            return;

        int slotIndex = shopSlots.IndexOf(slot);
        if (slotIndex < 0 || slotIndex >= _currentOffers.Count)
        {
            slot.Clear();
            return;
        }

        _currentOffers[slotIndex] = null;
        slot.Clear();
    }

    void ExitShop()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("ShopManager cannot exit because no next scene name is configured.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    void EnsureInventoryVisible()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.BeginRoomLoadoutPhase();

        if (EquipmentUIController.Instance != null)
            EquipmentUIController.Instance.ShowEquipmentInventory(true);
    }
}
