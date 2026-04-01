using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyUnit))]
public class EnemyAIController : MonoBehaviour
{
    [SerializeField] float delayBetweenMoveSteps = 1f;

    EnemyUnit _enemyUnit;
    Coroutine _activeTurnRoutine;

    Action OnDamageDealtCompleted;


    void Awake()
    {
        _enemyUnit = GetComponent<EnemyUnit>();
    }

    public void ExecuteTurn(Action onTurnComplete)
    {
        if (_activeTurnRoutine != null)
            StopCoroutine(_activeTurnRoutine);

        _activeTurnRoutine = StartCoroutine(ExecuteTurnRoutine(onTurnComplete));
    }

    IEnumerator ExecuteTurnRoutine(Action onTurnComplete)
    {
        if (CombatManager.I == null || CombatManager.I.PlayerUnit == null)
        {
            onTurnComplete?.Invoke();
            yield break;
        }

        PlayerUnit playerUnit = CombatManager.I.PlayerUnit;
        if (!_enemyUnit.IsAlive || !playerUnit.IsAlive)
        {
            onTurnComplete?.Invoke();
            yield break;
        }

        Debug.Log($"{_enemyUnit.DisplayName} started its turn.");

        for (int stepIndex = 0; stepIndex < _enemyUnit.Stats.Speed; stepIndex++)
        {
            if (CombatManager.I.IsStraightLineTargetInRange(_enemyUnit.GridPosition, playerUnit.GridPosition, _enemyUnit.Stats.Range))
                break;

            Vector2Int nextStep = GetStepTowardTarget(_enemyUnit.GridPosition, playerUnit.GridPosition);
            if (nextStep == Vector2Int.zero || !CanEnemyTakeStep(_enemyUnit.GridPosition + nextStep))
                break;

            yield return _enemyUnit.MoveOneStepAnimated(nextStep);

            if (stepIndex < _enemyUnit.Stats.Speed - 1)
                yield return new WaitForSeconds(delayBetweenMoveSteps);
        }

        _activeTurnRoutine = null;

        if (CombatManager.I.IsStraightLineTargetInRange(_enemyUnit.GridPosition, playerUnit.GridPosition, _enemyUnit.Stats.Range))
        {
            OnDamageDealtCompleted = onTurnComplete;
            DealDamage();
        }
        else
        {
            onTurnComplete?.Invoke();
        }
    }

    private void DealDamage()
    {
        DiceManager.Instance.OnDiceRollCompleted += DealDamageOfficially;
        _enemyUnit.ShowDice();
        DiceManager.Instance.RollDice(_enemyUnit.AttackDieSize, _enemyUnit.DiceCanvas);
    }

    private void DealDamageOfficially(int amount)
    {
        _enemyUnit.HideDice();
        DiceManager.Instance.OnDiceRollCompleted -= DealDamageOfficially;

        int finalDamageBeforeDefence = amount + _enemyUnit.Stats.Strength;
        CombatManager.I.PlayerUnit.ReceiveAttackRoll(_enemyUnit, amount);
        Debug.Log($"{_enemyUnit.DisplayName} attacked {CombatManager.I.PlayerUnit.DisplayName} with a roll of {amount} and {finalDamageBeforeDefence} raw damage before defence.");

        OnDamageDealtCompleted?.Invoke();
    }

    Vector2Int GetStepTowardTarget(Vector2Int currentPosition, Vector2Int targetPosition)
    {
        Vector2Int horizontalStep = Vector2Int.zero;
        if (targetPosition.x > currentPosition.x)
            horizontalStep = Vector2Int.right;
        else if (targetPosition.x < currentPosition.x)
            horizontalStep = Vector2Int.left;

        Vector2Int verticalStep = Vector2Int.zero;
        if (targetPosition.y > currentPosition.y)
            verticalStep = Vector2Int.up;
        else if (targetPosition.y < currentPosition.y)
            verticalStep = Vector2Int.down;

        if (horizontalStep != Vector2Int.zero && CanEnemyStepTo(currentPosition + horizontalStep))
            return horizontalStep;

        if (verticalStep != Vector2Int.zero && CanEnemyStepTo(currentPosition + verticalStep))
            return verticalStep;

        return Vector2Int.zero;
    }

    bool CanEnemyStepTo(Vector2Int targetGridPosition)
    {
        return GridMovementUtility.CanEnemyApproachTile(targetGridPosition, _enemyUnit);
    }

    bool CanEnemyTakeStep(Vector2Int targetGridPosition)
    {
        return CanEnemyStepTo(targetGridPosition);
    }
}
