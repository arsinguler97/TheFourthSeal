using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager I { get; private set; }

    // Actions are still data-driven even though turn resolution is handled here.
    [SerializeField] ActionDefinitionSO moveActionDefinition;
    [SerializeField] ActionDefinitionSO attackActionDefinition;
    [SerializeField] ActionDefinitionSO skipActionDefinition;
    [SerializeField] ActionDefinitionSO consumableActionDefinition;
    [SerializeField] ButtonAutoDisable[] buttonsForAutoDisable;
    [SerializeField] TMP_Text whoStartsText;
    [SerializeField] TMP_Text actionPointText;

    readonly List<CombatUnit> _turnOrder = new List<CombatUnit>();
    int _currentTurnIndex = -1;
    bool _playerUsedMoveThisTurn;
    bool _playerUsedAttackThisTurn;
    bool _isResolvingEnemyTurn;
    bool _isWaitingForLoadoutSelection;
    Button _runtimeConsumableActionButton;
    Image _runtimeConsumableActionIconImage;

    public bool IsPlayerTurn => CurrentUnit is PlayerUnit;
    public ActionType SelectedPlayerActionType { get; private set; } = ActionType.None;
    public int RemainingMoveSteps { get; private set; }
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
        EnsureConsumableActionButtonBound();
        UpdateConsumableActionAvailability();
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

        if (CurrentActionPoints < moveActionDefinition.actionCost)
        {
            Debug.Log("Move action ignored because there are not enough action points.");
            UpdateConsumableActionAvailability();
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
            UpdateConsumableActionAvailability();
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

        // Move AP is spent only once, when the first valid step actually starts.
        _playerUsedMoveThisTurn = true;
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - moveActionDefinition.actionCost);
        Debug.Log($"Move action spent {moveActionDefinition.actionCost} AP. Remaining AP: {CurrentActionPoints}.");
        UpdateActionPointUI();
        UpdateConsumableActionAvailability();
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
        UpdateConsumableActionAvailability();
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
            UpdateConsumableActionAvailability();
            return;
        }

        if (consumableActionDefinition.actionType != ActionType.HealthPotion)
        {
            Debug.LogWarning($"Consumable action asset '{consumableActionDefinition.name}' is configured as {consumableActionDefinition.actionType}.");
            UpdateConsumableActionAvailability();
            return;
        }

        if (CurrentActionPoints < consumableActionDefinition.actionCost)
        {
            Debug.Log("Consumable action ignored because there are not enough action points.");
            UpdateConsumableActionAvailability();
            return;
        }

        if (CombatManager.I == null || CombatManager.I.PlayerUnit == null || EquipmentManager.Instance == null)
        {
            UpdateConsumableActionAvailability();
            return;
        }

        ItemSO consumableItem = EquipmentManager.Instance.GetEquippedConsumable();
        if (consumableItem == null)
        {
            UpdateConsumableActionAvailability();
            return;
        }

        int healAmount = Mathf.Max(0, consumableItem.consumableHealAmount);
        if (healAmount <= 0)
        {
            Debug.LogWarning($"Consumable '{consumableItem.itemName}' has no heal amount configured.");
            UpdateConsumableActionAvailability();
            return;
        }

        CombatManager.I.PlayerUnit.Heal(healAmount);
        CombatManager.I.PlayerUnit.PlayConsumableUseVfx();
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - consumableActionDefinition.actionCost);
        UpdateActionPointUI();

        EquipmentManager.Instance.ConsumeEquippedConsumable();
        SelectedPlayerActionType = ActionType.None;
        Debug.Log($"Used consumable '{consumableItem.itemName}' for {healAmount} healing. Remaining AP: {CurrentActionPoints}.");
        UpdateConsumableActionAvailability();
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

        if (CurrentUnit is PlayerUnit playerUnit)
        {
            CurrentActionPoints = Mathf.Max(0, playerUnit.Stats.ActionPoints);
            _playerUsedMoveThisTurn = false;
            _playerUsedAttackThisTurn = false;
            Debug.Log($"Player turn started with {CurrentActionPoints} action points.");
            UpdateActionPointUI();
            UpdateConsumableActionAvailability();



            Invoke("ResetActionBarButtons", 0.2f);



        }

        if (CurrentUnit is EnemyUnit enemyUnit)
        {
            if (_isResolvingEnemyTurn)
                return;

            // Enemy turns are coroutine-driven and call back into FinishEnemyTurn when done.
            _isResolvingEnemyTurn = true;

            if (enemyUnit.EnemyAI != null)
                enemyUnit.EnemyAI.ExecuteTurn(FinishEnemyTurn);
            else
                FinishEnemyTurn();
        }
    }



    private void DisableActionButtons()
    {
        foreach (ButtonAutoDisable button in buttonsForAutoDisable)
            button.DisableButton();

        if (_runtimeConsumableActionButton != null)
            _runtimeConsumableActionButton.interactable = false;
    }

    private void ResetActionBarButtons()
    {
        foreach (ButtonAutoDisable button in buttonsForAutoDisable)
            button.EnableButton();

        UpdateConsumableActionAvailability();
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
        UpdateConsumableActionAvailability();
    }

    void HandleLoadoutSlotChanged(LoadoutSlotType slotType)
    {
        if (slotType != LoadoutSlotType.Consumable)
            return;

        UpdateConsumableActionAvailability();
    }

    void UpdateConsumableActionAvailability()
    {
        EnsureConsumableActionButtonBound();

        if (_runtimeConsumableActionButton == null)
            return;

        ItemSO consumableItem = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEquippedConsumable() : null;
        bool hasConsumable = consumableItem != null;

        if (_runtimeConsumableActionIconImage != null)
        {
            Sprite actionIcon = hasConsumable
                ? (consumableItem.icon != null ? consumableItem.icon : consumableItem.card)
                : null;
            _runtimeConsumableActionIconImage.sprite = actionIcon;
            _runtimeConsumableActionIconImage.enabled = actionIcon != null;
            _runtimeConsumableActionIconImage.preserveAspect = true;
        }

        if (!hasConsumable)
        {
            _runtimeConsumableActionButton.interactable = false;
            return;
        }

        bool canUseConsumable = IsPlayerTurn
            && !_isWaitingForLoadoutSelection
            && consumableActionDefinition != null
            && CurrentActionPoints >= consumableActionDefinition.actionCost;

        if (canUseConsumable)
            _runtimeConsumableActionButton.interactable = true;
        else
            _runtimeConsumableActionButton.interactable = false;
    }

    void UpdateActionPointUI()
    {
        if (actionPointText != null)
            actionPointText.text = $"Action Points: {CurrentActionPoints}";
    }

    void EnsureConsumableActionButtonBound()
    {
        if (_runtimeConsumableActionButton != null)
            return;

        GameObject actionBarObject = GameObject.Find("ActionBar");
        if (actionBarObject == null)
            return;

        Button[] actionButtons = actionBarObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < actionButtons.Length; i++)
        {
            Button button = actionButtons[i];
            if (button == null)
                continue;

            if (button.onClick.GetPersistentEventCount() != 0)
                continue;

            _runtimeConsumableActionButton = button;
            _runtimeConsumableActionButton.onClick.RemoveListener(ExecuteConsumableAction);
            _runtimeConsumableActionButton.onClick.AddListener(ExecuteConsumableAction);
            _runtimeConsumableActionIconImage = ResolveActionButtonIcon(button);
            break;
        }
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
}
