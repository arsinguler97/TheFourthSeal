using TMPro;
using UnityEngine;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] GameObject cardRoot;
    [SerializeField] TMP_Text attackText;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text strengthText;
    [SerializeField] TMP_Text rangeText;
    [SerializeField] TMP_Text speedText;
    [SerializeField] TMP_Text defenceText;

    PlayerUnit _playerUnit;

    void OnEnable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutSlotChanged += HandleLoadoutSlotChanged;
    }

    void Start()
    {
        RefreshCard();
    }

    void Update()
    {
        // Polling is enough here because the card is small and the scene has a single player unit.
        RefreshCard();
    }

    void OnDisable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutSlotChanged -= HandleLoadoutSlotChanged;
    }

    void HandleLoadoutSlotChanged(LoadoutSlotType _)
    {
        RefreshCard();
    }

    void RefreshCard()
    {
        if (_playerUnit == null || !_playerUnit)
            _playerUnit = CombatManager.I != null ? CombatManager.I.PlayerUnit : FindFirstObjectByType<PlayerUnit>();

        bool hasPlayer = _playerUnit != null;
        if (!hasPlayer)
            return;

        if (cardRoot != null && !cardRoot.activeSelf)
            cardRoot.SetActive(true);

        if (attackText != null)
            attackText.text = _playerUnit.AttackDieSize.ToString();

        if (healthText != null)
            healthText.text = _playerUnit.CurrentHealth.ToString();

        if (strengthText != null)
            strengthText.text = _playerUnit.Stats.Strength.ToString();

        if (rangeText != null)
            rangeText.text = _playerUnit.Stats.Range.ToString();

        if (speedText != null)
            speedText.text = _playerUnit.Stats.Speed.ToString();

        if (defenceText != null)
            defenceText.text = _playerUnit.Stats.Defence.ToString();
    }
}
