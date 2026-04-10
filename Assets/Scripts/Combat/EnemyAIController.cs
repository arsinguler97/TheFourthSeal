using UnityEngine;
using System;
using System.Collections;
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
            if (nextStep == Vector2Int.zero || !CanEnemyTakeStep(nextStep))
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
        DiceManager.Instance.RollDice(_enemyUnit.AttackDieSize, _enemyUnit.DiceCanvas, _enemyUnit.DiceRollHistory);
    }

    private void DealDamageOfficially(int amount)
    {
        _enemyUnit.HideDice();
        DiceManager.Instance.OnDiceRollCompleted -= DealDamageOfficially;

        if (_enemyUnit.AttackStyle == EnemyAttackStyle.Ranged)
        {
            if (CombatManager.I != null)
                CombatManager.I.ResolveEnemyAttack(_enemyUnit, CombatManager.I.PlayerUnit, amount, OnDamageDealtCompleted);
            else
                OnDamageDealtCompleted?.Invoke();

            return;
        }

        int finalDamageBeforeDefence = amount + _enemyUnit.Stats.Strength;
        CombatManager.I.PlayerUnit.ReceiveAttackRoll(_enemyUnit, amount);
        Debug.Log($"{_enemyUnit.DisplayName} attacked {CombatManager.I.PlayerUnit.DisplayName} with a roll of {amount} and {finalDamageBeforeDefence} raw damage before defence.");

        OnDamageDealtCompleted?.Invoke();
    }

    Vector2Int GetStepTowardTarget(Vector2Int currentPosition, Vector2Int targetPosition)
    {
        PriorityQueue<Vector2Int, int> tilesQueued = new PriorityQueue<Vector2Int, int>();
        tilesQueued.Enqueue(currentPosition, 0);

        Dictionary<Vector2Int, Vector2Int> tilesVisited = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costTotal = new Dictionary<Vector2Int, int>();

        tilesVisited[currentPosition] = currentPosition;
        costTotal[currentPosition] = 0;

        
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        Vector2Int currentTile = currentPosition;

        while (tilesQueued.Count != 0)
        {
            currentTile = tilesQueued.Dequeue();

            if (currentTile == targetPosition)
                break;

            foreach (Vector2Int newDir in directions)
            {
                Vector2Int nextTile = currentTile + newDir;

                if (GridManager.I.IsTileBlocked(nextTile))
                    continue;

                if (!CanEnemyStepTo(nextTile))
                    if (CombatManager.I.GetEnemyAt(nextTile) != null)
                        continue;

                int newCost = costTotal[currentTile] + GridManager.I.GetTileCost(nextTile);

                if (!(costTotal.ContainsKey(nextTile)) || newCost < costTotal[nextTile])
                {
                    costTotal[nextTile] = newCost;

                    int manhattanDistance = Mathf.Abs(targetPosition.x - nextTile.x) + Mathf.Abs(targetPosition.y - nextTile.y);

                    tilesQueued.Enqueue(nextTile, newCost + manhattanDistance);
                    tilesVisited[nextTile] = currentTile;
                }
            }
        }

        List<Vector2Int> pathToPlayer = new List<Vector2Int>();

        while (currentTile != currentPosition)
        {
            pathToPlayer.Add(currentTile);
            currentTile = tilesVisited[currentTile];
        }

        pathToPlayer.Reverse();

        return pathToPlayer[0];


        /* Old Version
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
        */
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






public class PriorityQueue<TElement, TPriority>
{
    private List<Tuple<TElement, TPriority>> elements = new List<Tuple<TElement, TPriority>>();

    public int Count
    {
        get { return elements.Count; }
    }

    public void Enqueue(TElement item, TPriority priority)
    {
        elements.Add(Tuple.Create(item, priority));
    }

    public TElement Dequeue()
    {
        Comparer<TPriority> comparer = Comparer<TPriority>.Default;
        int bestIndex = 0;

        for (int i = 0; i < elements.Count; i++)
        {
            if (comparer.Compare(elements[i].Item2, elements[bestIndex].Item2) < 0)
            {
                bestIndex = i;
            }
        }

        TElement bestItem = elements[bestIndex].Item1;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }

    public void Clear()
    {
        elements.Clear();
    }
}
