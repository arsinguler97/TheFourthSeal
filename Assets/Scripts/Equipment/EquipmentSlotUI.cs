using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] LoadoutSlotType slotType;
    [SerializeField] Transform cardContainer;
    [SerializeField] GameObject slotCardPrefab;
    [SerializeField] GameObject spareWeaponCardPrefab;
    [SerializeField] GameObject spareBodyArmorCardPrefab;
    [SerializeField] GameObject spareHelmetCardPrefab;
    [SerializeField] GameObject spareShieldCardPrefab;
    [SerializeField] GameObject spareGlovesCardPrefab;
    [SerializeField] GameObject spareBootsCardPrefab;
    [SerializeField] GameObject spareConsumableCardPrefab;
    [SerializeField] Image cardImage;
    [SerializeField] Button button;
    [SerializeField] float previewScaleMultiplier = 1.8f;

    GameObject _spawnedCardInstance;
    Vector3 _defaultPreviewScale = Vector3.one;


    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleSlotClicked);

        if (EquipmentManager.Instance == null)
            return;

        EquipmentManager.Instance.OnLoadoutSlotChanged += UpdateEquipmentSlot;
        UpdateEquipmentSlot(slotType);
    }

    void UpdateEquipmentSlot(LoadoutSlotType changedSlotType)
    {
        if (changedSlotType != slotType || EquipmentManager.Instance == null)
            return;

        ItemSO item = EquipmentManager.Instance.GetEquippedItem(slotType);

        RebuildCardVisual(item);
    }

    void HandleSlotClicked()
    {
        if (EquipmentUIController.Instance == null)
            return;

        EquipmentUIController.Instance.TryMoveSelectedItemToSlot(slotType);
        EquipmentUIController.Instance.SelectLoadoutSlot(slotType);
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutSlotChanged -= UpdateEquipmentSlot;

        if (button != null)
            button.onClick.RemoveListener(HandleSlotClicked);
    }

    void RebuildCardVisual(ItemSO item)
    {
        if (_spawnedCardInstance != null)
            Destroy(_spawnedCardInstance);

        _spawnedCardInstance = null;

        if (item == null)
        {
            if (cardImage != null)
                cardImage.enabled = false;
            return;
        }

        GameObject cardPrefab = GetCardPrefabForItem(item);
        if (cardPrefab != null && cardContainer != null)
        {
            _spawnedCardInstance = Instantiate(cardPrefab, cardContainer);
            _defaultPreviewScale = _spawnedCardInstance.transform.localScale;
            ItemCardUI cardUI = _spawnedCardInstance.GetComponent<ItemCardUI>();
            if (cardUI != null)
                cardUI.Bind(item);

            if (cardImage != null)
                cardImage.enabled = false;
            return;
        }

        if (cardImage != null)
        {
            cardImage.sprite = item.card != null ? item.card : item.icon;
            cardImage.enabled = true;
        }
    }

    GameObject GetCardPrefabForItem(ItemSO item)
    {
        if (item == null)
            return null;

        if (slotType != LoadoutSlotType.Spare)
            return slotCardPrefab;

        switch (item.type)
        {
            case ItemType.Weapon:
                return spareWeaponCardPrefab;
            case ItemType.Consumables:
                return spareConsumableCardPrefab;
            case ItemType.Equipment:
                switch (item.equipmentSubtype)
                {
                    case EquipmentSubtype.Helmet:
                        return spareHelmetCardPrefab;
                    case EquipmentSubtype.BodyArmor:
                        return spareBodyArmorCardPrefab;
                    case EquipmentSubtype.Gloves:
                        return spareGlovesCardPrefab;
                    case EquipmentSubtype.Boots:
                        return spareBootsCardPrefab;
                    case EquipmentSubtype.Shield:
                        return spareShieldCardPrefab;
                }
                break;
        }

        return null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCardInstance == null)
            return;

        _spawnedCardInstance.transform.localScale = _defaultPreviewScale * previewScaleMultiplier;
        _spawnedCardInstance.transform.SetAsLastSibling();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCardInstance == null)
            return;

        _spawnedCardInstance.transform.localScale = _defaultPreviewScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_spawnedCardInstance == null)
            return;

        _spawnedCardInstance.transform.localScale = _defaultPreviewScale;
    }
}
