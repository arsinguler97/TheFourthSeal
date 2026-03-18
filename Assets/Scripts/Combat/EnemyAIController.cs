using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(EnemyUnit))]
public class EnemyAIController : MonoBehaviour
{
    [SerializeField] float delayBetweenMoveSteps = 1f;

    EnemyUnit _enemyUnit;
    Coroutine _activeTurnRoutine;

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

        if (CombatManager.I.IsStraightLineTargetInRange(_enemyUnit.GridPosition, playerUnit.GridPosition, _enemyUnit.Stats.Range))
        {
            int dealtDamage = _enemyUnit.GetAttackDamage();
            Debug.Log($"{_enemyUnit.DisplayName} attacked {playerUnit.DisplayName} for {dealtDamage} rolled damage.");
            playerUnit.ReceiveDamage(dealtDamage);
        }

        _activeTurnRoutine = null;
        onTurnComplete?.Invoke();
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

        if (horizontalStep != Vector2Int.zero)
            return horizontalStep;

        return verticalStep;
    }

    bool CanEnemyStepTo(Vector2Int targetGridPosition)
    {
        if (GridManager.I == null || !GridManager.I.IsWalkable(targetGridPosition))
            return false;

        return CombatManager.I.PlayerUnit == null
            || !CombatManager.I.PlayerUnit.IsAlive
            || CombatManager.I.PlayerUnit.GridPosition != targetGridPosition;
    }

    bool CanEnemyTakeStep(Vector2Int targetGridPosition)
    {
        if (!CanEnemyStepTo(targetGridPosition))
            return false;

        return CombatManager.I == null || !CombatManager.I.IsTileOccupiedByAnotherUnit(targetGridPosition, _enemyUnit);
    }
}
