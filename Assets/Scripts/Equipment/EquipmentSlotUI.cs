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
    [SerializeField] float previewScaleMultiplier = 1.3f;

    GameObject _spawnedCardInstance;
    Vector3 _defaultPreviewScale = Vector3.one;
    RectTransform _spawnedCardRectTransform;
    Canvas _rootCanvas;
    int _defaultSiblingIndex;
    Vector2 _defaultAnchoredPosition;
    Vector2 _defaultSizeDelta;
    Vector2 _defaultAnchorMin;
    Vector2 _defaultAnchorMax;
    Vector2 _defaultPivot;
    Quaternion _defaultLocalRotation = Quaternion.identity;
    bool _isPreviewing;


    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleSlotClicked);

        _rootCanvas = GetComponentInParent<Canvas>();

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
        _spawnedCardRectTransform = null;
        _isPreviewing = false;

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
            _spawnedCardRectTransform = _spawnedCardInstance.GetComponent<RectTransform>();
            CacheDefaultPreviewState();
            ItemCardUI cardUI = _spawnedCardInstance.GetComponent<ItemCardUI>();
            if (cardUI != null)
                cardUI.Bind(item);

            if (cardImage != null)
                cardImage.enabled = false;
            return;
        }

        if (cardImage != null)
        {
            cardImage.sprite = item.card;
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

    void CacheDefaultPreviewState()
    {
        if (_spawnedCardInstance == null)
            return;

        _defaultPreviewScale = _spawnedCardInstance.transform.localScale;
        _defaultLocalRotation = _spawnedCardInstance.transform.localRotation;
        _defaultSiblingIndex = _spawnedCardInstance.transform.GetSiblingIndex();

        if (_spawnedCardRectTransform == null)
            return;

        _defaultAnchoredPosition = _spawnedCardRectTransform.anchoredPosition;
        _defaultSizeDelta = _spawnedCardRectTransform.sizeDelta;
        _defaultAnchorMin = _spawnedCardRectTransform.anchorMin;
        _defaultAnchorMax = _spawnedCardRectTransform.anchorMax;
        _defaultPivot = _spawnedCardRectTransform.pivot;
    }

    void BeginPreview()
    {
        if (_isPreviewing || _spawnedCardInstance == null)
            return;

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        Canvas previewCanvas = _rootCanvas != null ? _rootCanvas.rootCanvas : null;
        if (previewCanvas == null)
            return;

        _isPreviewing = true;
        _spawnedCardInstance.transform.SetParent(previewCanvas.transform, true);
        _spawnedCardInstance.transform.SetAsLastSibling();
        _spawnedCardInstance.transform.localScale = _defaultPreviewScale * previewScaleMultiplier;
    }

    void EndPreview()
    {
        if (!_isPreviewing || _spawnedCardInstance == null || cardContainer == null)
            return;

        _isPreviewing = false;
        _spawnedCardInstance.transform.SetParent(cardContainer, false);
        _spawnedCardInstance.transform.SetSiblingIndex(Mathf.Clamp(_defaultSiblingIndex, 0, cardContainer.childCount - 1));
        _spawnedCardInstance.transform.localScale = _defaultPreviewScale;
        _spawnedCardInstance.transform.localRotation = _defaultLocalRotation;

        if (_spawnedCardRectTransform == null)
            return;

        _spawnedCardRectTransform.anchorMin = _defaultAnchorMin;
        _spawnedCardRectTransform.anchorMax = _defaultAnchorMax;
        _spawnedCardRectTransform.pivot = _defaultPivot;
        _spawnedCardRectTransform.anchoredPosition = _defaultAnchoredPosition;
        _spawnedCardRectTransform.sizeDelta = _defaultSizeDelta;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCardInstance == null)
            return;

        BeginPreview();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCardInstance == null)
            return;

        EndPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_spawnedCardInstance == null)
            return;

        EndPreview();
    }
}
