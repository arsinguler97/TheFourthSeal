using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager I { get; private set; }

    // Actions are still data-driven even though turn resolution is handled here.
    [SerializeField] ActionDefinitionSO moveActionDefinition;
    [SerializeField] ActionDefinitionSO attackActionDefinition;
    [SerializeField] ActionDefinitionSO skipActionDefinition;
    [SerializeField] ActionDefinitionSO consumableActionDefinition;
    [SerializeField] ActionDefinitionSO healActionDefinition;
    [SerializeField] ButtonAutoDisable[] buttonsForAutoDisable;
    [SerializeField] TMP_Text whoStartsText;
    [SerializeField] TMP_Text actionPointText;

    readonly List<CombatUnit> _turnOrder = new List<CombatUnit>();
    int _currentTurnIndex = -1;
    bool _playerUsedMoveThisTurn;
    bool _playerUsedAttackThisTurn;
    bool _isResolvingEnemyTurn;
    bool _isWaitingForLoadoutSelection;
    readonly List<Button> _runtimeItemActionButtons = new List<Button>();
    readonly List<Image> _runtimeItemActionIcons = new List<Image>();
    readonly List<TMP_Text> _runtimeItemActionCostTexts = new List<TMP_Text>();

    public bool IsPlayerTurn => CurrentUnit is PlayerUnit;
    public ActionType SelectedPlayerActionType { get; private set; } = ActionType.None;
    public int RemainingMoveSteps { get; set; }
    public int CurrentActionPoints { get; private set; }
    public CombatUnit CurrentUnit => _currentTurnIndex >= 0 && _currentTurnIndex < _turnOrder.Count ? _turnOrder[_currentTurnIndex] : null;
    public event Action<IReadOnlyList<CombatUnit>, int> TurnOrderChanged;
    public int DelayBeforeHidingDice = 5;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutSlotChanged += HandleLoadoutSlotChanged;
    }

    void Start()
    {
        EnsureRuntimeItemActionButtonsBound();
        UpdateRuntimeItemActionAvailability();
        UpdateActionPointUI();

        whoStartsText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnLoadoutSlotChanged -= HandleLoadoutSlotChanged;

        if (I == this)
            I = null;
    }


    public bool IsPlayerMoveModeActive => IsPlayerTurn && SelectedPlayerActionType == ActionType.Move;
    public bool IsPlayerAttackModeActive => IsPlayerTurn && SelectedPlayerActionType == ActionType.Attack;

    public void InitializeTurnOrder()
    {
        BeginInitiativeOrder();
    }

    public void SelectMoveAction()
    {
        GridManager.I.ResetAttackGrids();
        ResetButtons();


        if (!IsPlayerTurn)
        {
            Debug.Log("Move action ignored because it is not currently the player's turn.");
            return;
        }

        if (CombatManager.I == null || CombatManager.I.PlayerUnit == null)
        {
            Debug.Log("Move action ignored because no player unit is registered in CombatManager.");
            return;
        }

        if (moveActionDefinition == null)
        {
            Debug.LogWarning("TurnManager needs a Move ActionDefinitionSO assigned.");
            return;
        }

        if (_playerUsedMoveThisTurn)
        {
            Debug.Log("Move action ignored because move was already used this turn.");
            return;
        }

        if (CurrentUnit is PlayerUnit && CurrentUnit.StatusEffectManager.IsStunned)
        {
            Debug.Log("Move action ignored because player is stunned.");
            ExecuteSkipAction();
            return;
        }

        if (CurrentActionPoints < moveActionDefinition.actionCost)
        {
            Debug.Log("Move action ignored because there are not enough action points.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        SelectedPlayerActionType = moveActionDefinition.actionType;
        RemainingMoveSteps = Mathf.Max(0, CombatManager.I.PlayerUnit.Stats.Speed);
        Debug.Log($"Move mode selected. Remaining move steps: {RemainingMoveSteps}. Action points: {CurrentActionPoints}.");
        


        GridManager.I.SetWalkGrids(CurrentUnit.GridPosition, CombatManager.I.PlayerUnit.Stats.Speed, CombatManager.I.PlayerUnit.Stats.Range);
    }

    public void SelectAttackAction()
    {
        GridManager.I.ResetWalkGrids();
        ResetButtons();


        if (!IsPlayerTurn)
        {
            Debug.Log("Attack action ignored because it is not currently the player's turn.");
            return;
        }

        if (attackActionDefinition == null)
        {
            Debug.LogWarning("TurnManager needs an Attack ActionDefinitionSO assigned.");
            return;
        }

        if (_playerUsedAttackThisTurn)
        {
            Debug.Log("Attack action ignored because attack was already used this turn.");
            return;
        }

        if (CurrentActionPoints < attackActionDefinition.actionCost)
        {
            Debug.Log("Attack action ignored because there are not enough action points.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        SelectedPlayerActionType = attackActionDefinition.actionType;
        RemainingMoveSteps = 0;
        Debug.Log($"Attack mode selected. Action points: {CurrentActionPoints}.");


        GridManager.I.SetAttackGrids(CurrentUnit.GridPosition, CombatManager.I.PlayerUnit.Stats.Range);
    }

    public bool CanPlayerSpendMoveStep(int numberOfMoveSteps)
    {
        return IsPlayerMoveModeActive && RemainingMoveSteps - numberOfMoveSteps >= 0;
    }

    public void NotifyPlayerStartedMoveAction()
    {
        if (!IsPlayerMoveModeActive || _playerUsedMoveThisTurn || moveActionDefinition == null)
            return;


        if (CurrentUnit is PlayerUnit && CurrentUnit.StatusEffectManager.IsStunned)
        {
            Debug.Log("Move action ignored because player is stunned.");
            ExecuteSkipAction();
            return;
        }

        // Move AP is spent only once, when the first valid step actually starts.
        _playerUsedMoveThisTurn = true;
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - moveActionDefinition.actionCost);
        Debug.Log($"Move action spent {moveActionDefinition.actionCost} AP. Remaining AP: {CurrentActionPoints}.");
        UpdateActionPointUI();
        UpdateRuntimeItemActionAvailability();
    }

    public void NotifyPlayerMovedStep(int numberOfMoveSteps)
    {
        if (!CanPlayerSpendMoveStep(numberOfMoveSteps))
            return;

        RemainingMoveSteps -= numberOfMoveSteps;
        if (RemainingMoveSteps <= 0)
            SelectedPlayerActionType = ActionType.None;
    }

    public void NotifyPlayerAttackResolved()
    {
        if (!IsPlayerAttackModeActive)
            return;

        // Attacks no longer auto-end the turn; Skip is the explicit end-turn action.
        if (attackActionDefinition != null)
        {
            _playerUsedAttackThisTurn = true;
            CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - attackActionDefinition.actionCost);
            Debug.Log($"Attack action spent {attackActionDefinition.actionCost} AP. Remaining AP: {CurrentActionPoints}.");
            UpdateActionPointUI();
        }

        SelectedPlayerActionType = ActionType.None;
        UpdateRuntimeItemActionAvailability();
    }

    public void ExecuteSkipAction()
    {
        GridManager.I.ResetWalkGrids();
        GridManager.I.ResetAttackGrids();


        if (!IsPlayerTurn)
            return;

        if (skipActionDefinition == null)
        {
            Debug.LogWarning("TurnManager needs a Skip ActionDefinitionSO assigned.");
            return;
        }

        if (skipActionDefinition.actionType != ActionType.Skip)
        {
            Debug.LogWarning($"Skip action asset '{skipActionDefinition.name}' is configured as {skipActionDefinition.actionType}.");
            return;
        }

        Debug.Log($"{CurrentUnit.DisplayName} skipped the turn.");
        DisableActionButtons();
        EndCurrentTurn();
    }

    public void ExecuteConsumableAction()
    {
        GridManager.I.ResetWalkGrids();
        GridManager.I.ResetAttackGrids();

        if (!IsPlayerTurn)
            return;

        if (consumableActionDefinition == null)
        {
            Debug.LogWarning("TurnManager needs a HealthPotion ActionDefinitionSO assigned.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (consumableActionDefinition.actionType != ActionType.HealthPotion)
        {
            Debug.LogWarning($"Consumable action asset '{consumableActionDefinition.name}' is configured as {consumableActionDefinition.actionType}.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (CurrentActionPoints < consumableActionDefinition.actionCost)
        {
            Debug.Log("Consumable action ignored because there are not enough action points.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (CombatManager.I == null || CombatManager.I.PlayerUnit == null || EquipmentManager.Instance == null)
        {
            UpdateRuntimeItemActionAvailability();
            return;
        }

        ItemSO consumableItem = EquipmentManager.Instance.GetEquippedConsumable();
        if (consumableItem == null)
        {
            UpdateRuntimeItemActionAvailability();
            return;
        }

        int healAmount = Mathf.Max(0, consumableItem.consumableHealAmount);
        if (healAmount <= 0)
        {
            Debug.LogWarning($"Consumable '{consumableItem.itemName}' has no heal amount configured.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        CombatManager.I.PlayerUnit.Heal(healAmount);
        CombatManager.I.PlayerUnit.PlayConsumableUseVfx();
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - consumableActionDefinition.actionCost);
        UpdateActionPointUI();

        EquipmentManager.Instance.ConsumeEquippedConsumable();
        SelectedPlayerActionType = ActionType.None;
        Debug.Log($"Used consumable '{consumableItem.itemName}' for {healAmount} healing. Remaining AP: {CurrentActionPoints}.");
        UpdateRuntimeItemActionAvailability();
    }

    public void ExecuteHealAction()
    {
        GridManager.I.ResetWalkGrids();
        GridManager.I.ResetAttackGrids();

        if (!IsPlayerTurn)
            return;

        if (healActionDefinition == null)
        {
            Debug.LogWarning("TurnManager needs a Heal ActionDefinitionSO assigned.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (healActionDefinition.actionType != ActionType.Heal)
        {
            Debug.LogWarning($"Heal action asset '{healActionDefinition.name}' is configured as {healActionDefinition.actionType}.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (CurrentActionPoints < healActionDefinition.actionCost)
        {
            Debug.Log("Heal action ignored because there are not enough action points.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        if (CombatManager.I == null || CombatManager.I.PlayerUnit == null || EquipmentManager.Instance == null)
        {
            UpdateRuntimeItemActionAvailability();
            return;
        }

        ItemSO healingWeapon = EquipmentManager.Instance.GetEquippedWeaponThatGrantsHealAction();
        if (healingWeapon == null)
        {
            UpdateRuntimeItemActionAvailability();
            return;
        }

        int weaponHealAmount = Mathf.Max(0, EquipmentManager.Instance.GetEquippedWeaponAttackOverride());
        int strengthBonus = CombatManager.I.PlayerUnit != null ? Mathf.Max(0, CombatManager.I.PlayerUnit.Stats.Strength) : 0;
        int healAmount = weaponHealAmount + strengthBonus;
        if (healAmount <= 0)
        {
            Debug.LogWarning($"Healing weapon '{healingWeapon.itemName}' has no attack value to convert into healing.");
            UpdateRuntimeItemActionAvailability();
            return;
        }

        CombatManager.I.PlayerUnit.Heal(healAmount);
        CombatManager.I.PlayerUnit.PlayConsumableUseVfx();
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - healActionDefinition.actionCost);
        UpdateActionPointUI();

        SelectedPlayerActionType = ActionType.None;
        Debug.Log($"Used healing weapon '{healingWeapon.itemName}' for {healAmount} healing. Remaining AP: {CurrentActionPoints}.");
        UpdateRuntimeItemActionAvailability();
    }

    void BeginCurrentTurn()
    {
        SelectedPlayerActionType = ActionType.None;
        RemainingMoveSteps = 0;
        UpdateTurnIndicators();

        if (CurrentUnit == null)
            return;

        if (EquipmentManager.Instance != null
            && !EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom
            && EquipmentManager.Instance.HasPendingLoadoutChoice)
        {
            _isWaitingForLoadoutSelection = true;
            DisableActionButtons();
            UpdateActionPointUI();
            return;
        }

        _isWaitingForLoadoutSelection = false;

        Debug.Log($"{CurrentUnit.DisplayName} started the turn.");

        CurrentUnit.StatusEffectManager.UpdateEffects();

        if (CurrentUnit == null || !CurrentUnit.IsAlive || !CurrentUnit.gameObject.activeInHierarchy)
        {
            EndCurrentTurn();
            return;
        }

        if (CurrentUnit is PlayerUnit playerUnit)
        {
            CurrentActionPoints = Mathf.Max(0, playerUnit.Stats.ActionPoints);
            _playerUsedMoveThisTurn = false;
            _playerUsedAttackThisTurn = false;
            Debug.Log($"Player turn started with {CurrentActionPoints} action points.");
            UpdateActionPointUI();
            UpdateRuntimeItemActionAvailability();



            Invoke("ResetActionBarButtons", 0.2f);



        }

        if (CurrentUnit is EnemyUnit enemyUnit)
        {
            if (_isResolvingEnemyTurn)
                return;

            if (!enemyUnit.gameObject.activeInHierarchy || !enemyUnit.IsAlive)
            {
                EndCurrentTurn();
                return;
            }

            // Enemy turns are coroutine-driven and call back into FinishEnemyTurn when done.
            _isResolvingEnemyTurn = true;

            if (enemyUnit.EnemyAI != null && enemyUnit.EnemyAI.isActiveAndEnabled)
                enemyUnit.EnemyAI.ExecuteTurn(FinishEnemyTurn);
            else
                FinishEnemyTurn();
        }
    }



    private void DisableActionButtons()
    {
        foreach (ButtonAutoDisable button in buttonsForAutoDisable)
            button.DisableButton();

        for (int i = 0; i < _runtimeItemActionButtons.Count; i++)
        {
            if (_runtimeItemActionButtons[i] != null)
                _runtimeItemActionButtons[i].interactable = false;
        }
    }

    private void ResetActionBarButtons()
    {
        foreach (ButtonAutoDisable button in buttonsForAutoDisable)
            button.EnableButton();

        UpdateRuntimeItemActionAvailability();
    }




    void EndCurrentTurn()
    {
        _isResolvingEnemyTurn = false;

        if (CombatManager.I != null && CurrentUnit != null && CurrentUnit.IsAlive)
            CombatManager.I.ApplyLavaDamageIfNeeded(CurrentUnit);

        RemoveDeadUnitsFromTurnOrder();

        if (CombatManager.I != null && (CombatManager.I.PlayerUnit == null || !CombatManager.I.PlayerUnit.IsAlive))
        {
            ClearTurnState();
            return;
        }

        if (_turnOrder.Count == 0)
        {
            ClearTurnState();
            return;
        }

        if (_currentTurnIndex >= _turnOrder.Count)
            _currentTurnIndex = 0;
        else
            _currentTurnIndex++;

        if (_currentTurnIndex >= _turnOrder.Count)
            _currentTurnIndex = 0;

        SelectedPlayerActionType = ActionType.None;
        RemainingMoveSteps = 0;
        CurrentActionPoints = 0;
        UpdateActionPointUI();
        UpdateRuntimeItemActionAvailability();
        UpdateTurnIndicators();
        NotifyTurnOrderChanged();
        BeginCurrentTurn();
    }

    public void StopCombatFlow()
    {
        _isResolvingEnemyTurn = false;
        _isWaitingForLoadoutSelection = false;
        ClearTurnState();
    }

    public void ResumeCurrentTurnAfterLoadoutSelection()
    {
        if (!_isWaitingForLoadoutSelection || CurrentUnit == null)
            return;

        _isWaitingForLoadoutSelection = false;
        BeginCurrentTurn();
    }

    void RemoveDeadUnitsFromTurnOrder()
    {
        CombatUnit currentUnit = CurrentUnit;
        _turnOrder.RemoveAll(unit => unit == null || !unit.IsAlive);

        if (_turnOrder.Count == 0)
            return;

        int currentUnitIndex = _turnOrder.IndexOf(currentUnit);
        _currentTurnIndex = currentUnitIndex;
    }

    void DebugTurnOrder()
    {
        for (int i = 0; i < _turnOrder.Count; i++)
            Debug.Log($"Turn {i + 1}: {_turnOrder[i].DisplayName} with initiative {_turnOrder[i].LastInitiativeRoll}.");
    }


    public void BeginInitiativeOrder()
    {
        if (CombatManager.I == null)
            return;

        DisableActionButtons();

        EquipmentUIController.Instance.OnInventoryClosed -= RebuildInitiativeOrder;
        EquipmentUIController.Instance.OnInventoryClosed += RebuildInitiativeOrder;

        EquipmentUIController.Instance.OpenEquipmentInventory();
    }

    public void RebuildInitiativeOrder()
    {
        EquipmentUIController.Instance.OnInventoryClosed -= RebuildInitiativeOrder;

        List<(int, DiceCanvas)> diceList = new();

        _turnOrder.Clear();

        List<CombatUnit> livingUnits = CombatManager.I.GetLivingUnits();
        for (int i = 0; i < livingUnits.Count; i++)
        {
            diceList.Add((6, livingUnits[i].DiceCanvas));
            _turnOrder.Add(livingUnits[i]);
            livingUnits[i].ShowDice();
        }

        DiceManager.Instance.OnMultiDiceRollCompleted += SortInitiativeResults;
        DiceManager.Instance.RollMultiDice(diceList);
    }

    
    private void SortInitiativeResults(List<int> diceResults)
    {
        DiceManager.Instance.OnMultiDiceRollCompleted -= SortInitiativeResults;

        for (int i = 0; i < _turnOrder.Count; i++)
        {
            _turnOrder[i].LastInitiativeRoll = diceResults[i];
        }

        _turnOrder.Sort((left, right) => right.LastInitiativeRoll.CompareTo(left.LastInitiativeRoll));

        whoStartsText.gameObject.SetActive(true);
        whoStartsText.text = _turnOrder[0].DisplayName + whoStartsText.text;

        StartCoroutine(FinishInitiativeOrder(_turnOrder));
    }

    IEnumerator FinishInitiativeOrder(List<CombatUnit> livingUnits)
    {
        yield return new WaitForSeconds(DelayBeforeHidingDice);

        whoStartsText.gameObject.SetActive(false);

        for (int i = 0; i < livingUnits.Count; i++)
            livingUnits[i].HideDice();


        if (_turnOrder.Count == 0)
        {
            _currentTurnIndex = -1;
            UpdateTurnIndicators();
            NotifyTurnOrderChanged();
        }
        else
        {
            _currentTurnIndex = 0;
            SelectedPlayerActionType = ActionType.None;
            RemainingMoveSteps = 0;
            CurrentActionPoints = 0;
            UpdateActionPointUI();

            DebugTurnOrder();
            UpdateTurnIndicators();
            NotifyTurnOrderChanged();
            BeginCurrentTurn();
        }
    }


    public IReadOnlyList<CombatUnit> GetTurnOrder()
    {
        return _turnOrder;
    }

    public int GetCurrentTurnIndex()
    {
        return _currentTurnIndex;
    }

    void NotifyTurnOrderChanged()
    {
        TurnOrderChanged?.Invoke(_turnOrder, _currentTurnIndex);
    }

    void ClearTurnState()
    {
        // Used when combat is interrupted entirely, for example by player death.
        _turnOrder.Clear();
        _currentTurnIndex = -1;
        SelectedPlayerActionType = ActionType.None;
        RemainingMoveSteps = 0;
        CurrentActionPoints = 0;
        UpdateActionPointUI();
        UpdateRuntimeItemActionAvailability();
        UpdateTurnIndicators();
        NotifyTurnOrderChanged();
    }

    void FinishEnemyTurn()
    {
        _isResolvingEnemyTurn = false;
        EndCurrentTurn();
    }

    void UpdateTurnIndicators()
    {
        if (CombatManager.I == null)
            return;

        List<CombatUnit> livingUnits = CombatManager.I.GetLivingUnits();
        for (int i = 0; i < livingUnits.Count; i++)
            livingUnits[i].SetTurnIndicatorActive(livingUnits[i] == CurrentUnit);
    }


    private void ResetButtons()
    {
        if (!_playerUsedAttackThisTurn) buttonsForAutoDisable[0].EnableButton();
        if (!_playerUsedMoveThisTurn) buttonsForAutoDisable[1].EnableButton();
        UpdateRuntimeItemActionAvailability();
    }

    void HandleLoadoutSlotChanged(LoadoutSlotType slotType)
    {
        if (slotType != LoadoutSlotType.Consumable && slotType != LoadoutSlotType.Weapon)
            return;

        UpdateRuntimeItemActionAvailability();
    }

    void UpdateRuntimeItemActionAvailability()
    {
        EnsureRuntimeItemActionButtonsBound();

        if (_runtimeItemActionButtons.Count == 0)
            return;

        ItemSO consumableItem = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEquippedConsumable() : null;
        ItemSO healingWeapon = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEquippedWeaponThatGrantsHealAction() : null;
        bool hasConsumable = consumableItem != null;
        bool hasHealingWeapon = healingWeapon != null;
        int runtimeSlotIndex = 0;

        if (hasConsumable)
        {
            bool canUseConsumable = IsPlayerTurn
                && !_isWaitingForLoadoutSelection
                && consumableActionDefinition != null
                && CurrentActionPoints >= consumableActionDefinition.actionCost;
            BindRuntimeItemActionButton(runtimeSlotIndex++, consumableItem.card, ExecuteConsumableAction, canUseConsumable, consumableActionDefinition);
        }

        if (hasHealingWeapon)
        {
            bool canUseHeal = IsPlayerTurn
                && !_isWaitingForLoadoutSelection
                && healActionDefinition != null
                && CurrentActionPoints >= healActionDefinition.actionCost;
            BindRuntimeItemActionButton(runtimeSlotIndex++, healingWeapon.card, ExecuteHealAction, canUseHeal, healActionDefinition);
        }

        for (int i = runtimeSlotIndex; i < _runtimeItemActionButtons.Count; i++)
            BindRuntimeItemActionButton(i, null, null, false);
    }

    void UpdateActionPointUI()
    {
        if (actionPointText != null)
            actionPointText.text = $"Action Points: {CurrentActionPoints}";
    }

    void EnsureRuntimeItemActionButtonsBound()
    {
        if (_runtimeItemActionButtons.Count > 0)
            return;

        GameObject actionBarObject = GameObject.Find("ActionBar");
        if (actionBarObject == null)
            return;

        Transform actionBarTransform = actionBarObject.transform;
        for (int i = 0; i < actionBarTransform.childCount; i++)
        {
            Button button = actionBarTransform.GetChild(i).GetComponent<Button>();
            if (button == null)
                continue;

            if (button.onClick.GetPersistentEventCount() != 0)
                continue;

            _runtimeItemActionButtons.Add(button);
            _runtimeItemActionIcons.Add(ResolveActionButtonIcon(button));
            _runtimeItemActionCostTexts.Add(ResolveActionButtonCostText(button));
        }
    }

    void BindRuntimeItemActionButton(int index, Sprite actionIcon, UnityEngine.Events.UnityAction onClickAction, bool isInteractable)
    {
        BindRuntimeItemActionButton(index, actionIcon, onClickAction, isInteractable, null);
    }

    void BindRuntimeItemActionButton(int index, Sprite actionIcon, UnityEngine.Events.UnityAction onClickAction, bool isInteractable, ActionDefinitionSO actionDefinition)
    {
        if (index < 0 || index >= _runtimeItemActionButtons.Count)
            return;

        Button button = _runtimeItemActionButtons[index];
        Image iconImage = index < _runtimeItemActionIcons.Count ? _runtimeItemActionIcons[index] : null;
        TMP_Text costText = index < _runtimeItemActionCostTexts.Count ? _runtimeItemActionCostTexts[index] : null;
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (onClickAction != null)
            button.onClick.AddListener(onClickAction);

        button.interactable = actionIcon != null && isInteractable;

        if (iconImage != null)
        {
            iconImage.sprite = actionIcon;
            iconImage.enabled = actionIcon != null;
            iconImage.preserveAspect = true;
        }

        if (costText != null)
            costText.text = actionIcon != null && actionDefinition != null ? $"AC: {actionDefinition.actionCost}" : string.Empty;
    }

    Image ResolveActionButtonIcon(Button button)
    {
        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].gameObject == button.gameObject)
                continue;

            return images[i];
        }

        return button.targetGraphic as Image;
    }

    TMP_Text ResolveActionButtonCostText(Button button)
    {
        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                return texts[i];
        }

        return null;
    }
}
