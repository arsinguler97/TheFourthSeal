using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] Button button;
    [SerializeField] Transform cardContainer;
    [SerializeField] TMP_Text costText;
    [SerializeField] Image fallbackCardImage;
    [SerializeField] GameObject weaponCardPrefab;
    [SerializeField] GameObject bodyArmorCardPrefab;
    [SerializeField] GameObject helmetCardPrefab;
    [SerializeField] GameObject shieldCardPrefab;
    [SerializeField] GameObject glovesCardPrefab;
    [SerializeField] GameObject bootsCardPrefab;
    [SerializeField] GameObject consumableCardPrefab;
    [SerializeField] Color affordableCostColor = new Color(1f, 0.81f, 0.2f, 1f);
    [SerializeField] Color unavailableCostColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] float previewScaleMultiplier = 1.3f;

    GameObject _spawnedCard;
    RectTransform _spawnedCardRectTransform;
    ItemSO _boundItem;
    Canvas _rootCanvas;
    int _defaultSiblingIndex;
    Vector3 _defaultPreviewScale = Vector3.one;
    Vector2 _defaultAnchoredPosition;
    Vector2 _defaultSizeDelta;
    Vector2 _defaultAnchorMin;
    Vector2 _defaultAnchorMax;
    Vector2 _defaultPivot;
    Quaternion _defaultLocalRotation = Quaternion.identity;
    bool _isPreviewing;

    public event Action<ShopItemSlotUI> Clicked;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);

        _rootCanvas = GetComponentInParent<Canvas>();
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    public ItemSO GetBoundItem()
    {
        return _boundItem;
    }

    public void Bind(ItemSO item, bool isInteractable)
    {
        _boundItem = item;

        RebuildCardVisual(item);

        if (costText != null)
        {
            costText.text = item != null ? item.cost.ToString() : string.Empty;
            costText.color = isInteractable ? affordableCostColor : unavailableCostColor;
        }

        if (button != null)
            button.interactable = item != null && isInteractable;
    }

    public void Clear()
    {
        Bind(null, false);
    }

    void HandleClicked()
    {
        if (_boundItem == null)
            return;

        Clicked?.Invoke(this);
    }

    void RebuildCardVisual(ItemSO item)
    {
        if (_spawnedCard != null)
            Destroy(_spawnedCard);

        _spawnedCard = null;
        _spawnedCardRectTransform = null;
        _isPreviewing = false;

        if (fallbackCardImage != null)
        {
            fallbackCardImage.sprite = null;
            fallbackCardImage.enabled = false;
        }

        if (item == null)
            return;

        GameObject cardPrefab = GetCardPrefabForItem(item);
        if (cardPrefab != null && cardContainer != null)
        {
            _spawnedCard = Instantiate(cardPrefab, cardContainer);
            _spawnedCardRectTransform = _spawnedCard.GetComponent<RectTransform>();
            CacheDefaultPreviewState();
            ItemCardUI cardUI = _spawnedCard.GetComponent<ItemCardUI>();
            if (cardUI != null)
                cardUI.Bind(item);
            return;
        }

        if (fallbackCardImage != null)
        {
            fallbackCardImage.sprite = item.card;
            fallbackCardImage.enabled = item.card != null;
        }
    }

    GameObject GetCardPrefabForItem(ItemSO item)
    {
        if (item == null)
            return null;

        switch (item.type)
        {
            case ItemType.Weapon:
                return weaponCardPrefab;
            case ItemType.Consumables:
                return consumableCardPrefab;
            case ItemType.Equipment:
                switch (item.equipmentSubtype)
                {
                    case EquipmentSubtype.Helmet:
                        return helmetCardPrefab;
                    case EquipmentSubtype.BodyArmor:
                        return bodyArmorCardPrefab;
                    case EquipmentSubtype.Gloves:
                        return glovesCardPrefab;
                    case EquipmentSubtype.Boots:
                        return bootsCardPrefab;
                    case EquipmentSubtype.Shield:
                        return shieldCardPrefab;
                }
                break;
        }

        return null;
    }

    void CacheDefaultPreviewState()
    {
        if (_spawnedCard == null)
            return;

        _defaultPreviewScale = _spawnedCard.transform.localScale;
        _defaultLocalRotation = _spawnedCard.transform.localRotation;
        _defaultSiblingIndex = _spawnedCard.transform.GetSiblingIndex();

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
        if (_isPreviewing || _spawnedCard == null)
            return;

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        Canvas previewCanvas = _rootCanvas != null ? _rootCanvas.rootCanvas : null;
        if (previewCanvas == null)
            return;

        _isPreviewing = true;
        _spawnedCard.transform.SetParent(previewCanvas.transform, true);
        _spawnedCard.transform.SetAsLastSibling();
        _spawnedCard.transform.localScale = _defaultPreviewScale * previewScaleMultiplier;
    }

    void EndPreview()
    {
        if (!_isPreviewing || _spawnedCard == null || cardContainer == null)
            return;

        _isPreviewing = false;
        _spawnedCard.transform.SetParent(cardContainer, false);
        _spawnedCard.transform.SetSiblingIndex(Mathf.Clamp(_defaultSiblingIndex, 0, cardContainer.childCount - 1));
        _spawnedCard.transform.localScale = _defaultPreviewScale;
        _spawnedCard.transform.localRotation = _defaultLocalRotation;

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
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCard == null)
            return;

        BeginPreview();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || _spawnedCard == null)
            return;

        EndPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_spawnedCard == null)
            return;

        EndPreview();
    }
}
