using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager I { get; private set; }

    [SerializeField] ActionDefinitionSO moveActionDefinition;
    [SerializeField] ActionDefinitionSO attackActionDefinition;
    [SerializeField] ActionDefinitionSO skipActionDefinition;

    readonly List<CombatUnit> _turnOrder = new List<CombatUnit>();
    int _currentTurnIndex = -1;
    bool _playerUsedMoveThisTurn;
    bool _playerUsedAttackThisTurn;

    public bool IsPlayerTurn => CurrentUnit is PlayerUnit;
    public ActionType SelectedPlayerActionType { get; private set; } = ActionType.None;
    public int RemainingMoveSteps { get; private set; }
    public int CurrentActionPoints { get; private set; }
    public CombatUnit CurrentUnit => _currentTurnIndex >= 0 && _currentTurnIndex < _turnOrder.Count ? _turnOrder[_currentTurnIndex] : null;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    public bool IsPlayerMoveModeActive => IsPlayerTurn && SelectedPlayerActionType == ActionType.Move;
    public bool IsPlayerAttackModeActive => IsPlayerTurn && SelectedPlayerActionType == ActionType.Attack;

    public void InitializeTurnOrder()
    {
        RebuildInitiativeOrder();
    }

    public void SelectMoveAction()
    {
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
            return;
        }

        SelectedPlayerActionType = moveActionDefinition.actionType;
        RemainingMoveSteps = Mathf.Max(0, CombatManager.I.PlayerUnit.Stats.Speed);
        Debug.Log($"Move mode selected. Remaining move steps: {RemainingMoveSteps}. Action points: {CurrentActionPoints}.");
    }

    public void SelectAttackAction()
    {
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
            return;
        }

        SelectedPlayerActionType = attackActionDefinition.actionType;
        RemainingMoveSteps = 0;
        Debug.Log($"Attack mode selected. Action points: {CurrentActionPoints}.");
    }

    public bool CanPlayerSpendMoveStep()
    {
        return IsPlayerMoveModeActive && RemainingMoveSteps > 0;
    }

    public void NotifyPlayerStartedMoveAction()
    {
        if (!IsPlayerMoveModeActive || _playerUsedMoveThisTurn || moveActionDefinition == null)
            return;

        _playerUsedMoveThisTurn = true;
        CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - moveActionDefinition.actionCost);
        Debug.Log($"Move action spent {moveActionDefinition.actionCost} AP. Remaining AP: {CurrentActionPoints}.");
    }

    public void NotifyPlayerMovedOneStep()
    {
        if (!CanPlayerSpendMoveStep())
            return;

        RemainingMoveSteps--;
        if (RemainingMoveSteps <= 0)
        {
            SelectedPlayerActionType = ActionType.None;

            if (CurrentActionPoints <= 0 || (_playerUsedMoveThisTurn && _playerUsedAttackThisTurn))
                EndCurrentTurn();
        }
    }

    public void NotifyPlayerAttackResolved()
    {
        if (!IsPlayerAttackModeActive)
            return;

        if (attackActionDefinition != null)
        {
            _playerUsedAttackThisTurn = true;
            CurrentActionPoints = Mathf.Max(0, CurrentActionPoints - attackActionDefinition.actionCost);
            Debug.Log($"Attack action spent {attackActionDefinition.actionCost} AP. Remaining AP: {CurrentActionPoints}.");
        }

        SelectedPlayerActionType = ActionType.None;

        if (CurrentActionPoints <= 0 || (_playerUsedMoveThisTurn && _playerUsedAttackThisTurn))
            EndCurrentTurn();
    }

    public void ExecuteSkipAction()
    {
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
        EndCurrentTurn();
    }

    void BeginCurrentTurn()
    {
        SelectedPlayerActionType = ActionType.None;
        RemainingMoveSteps = 0;

        if (CurrentUnit == null)
            return;

        Debug.Log($"{CurrentUnit.DisplayName} started the turn.");

        if (CurrentUnit is PlayerUnit playerUnit)
        {
            CurrentActionPoints = Mathf.Max(0, playerUnit.Stats.ActionPoints);
            _playerUsedMoveThisTurn = false;
            _playerUsedAttackThisTurn = false;
            Debug.Log($"Player turn started with {CurrentActionPoints} action points.");
        }

        if (CurrentUnit is EnemyUnit enemyUnit && CombatManager.I != null)
        {
            CombatManager.I.ExecuteEnemyTurn(enemyUnit);
            EndCurrentTurn();
        }
    }

    void EndCurrentTurn()
    {
        RemoveDeadUnitsFromTurnOrder();

        if (_turnOrder.Count == 0)
        {
            _currentTurnIndex = -1;
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

    public void RebuildInitiativeOrder()
    {
        if (CombatManager.I == null)
            return;

        List<CombatUnit> livingUnits = CombatManager.I.GetLivingUnits();
        for (int i = 0; i < livingUnits.Count; i++)
            livingUnits[i].RollInitiative();

        _turnOrder.Clear();
        _turnOrder.AddRange(livingUnits);
        _turnOrder.Sort((left, right) => right.LastInitiativeRoll.CompareTo(left.LastInitiativeRoll));

        if (_turnOrder.Count == 0)
        {
            _currentTurnIndex = -1;
            return;
        }

        _currentTurnIndex = 0;
        SelectedPlayerActionType = ActionType.None;
        RemainingMoveSteps = 0;
        CurrentActionPoints = 0;

        DebugTurnOrder();
        BeginCurrentTurn();
    }
}
