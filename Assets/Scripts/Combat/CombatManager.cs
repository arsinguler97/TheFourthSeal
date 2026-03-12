using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager I { get; private set; }

    public PlayerUnit PlayerUnit { get; private set; }
    readonly List<EnemyUnit> _enemyUnits = new List<EnemyUnit>();
    public IReadOnlyList<EnemyUnit> EnemyUnits => _enemyUnits;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    public void RegisterPlayer(PlayerUnit playerUnit)
    {
        PlayerUnit = playerUnit;
    }

    public void RegisterEnemy(EnemyUnit enemyUnit)
    {
        if (enemyUnit != null && !_enemyUnits.Contains(enemyUnit))
            _enemyUnits.Add(enemyUnit);
    }

    public bool IsTileOccupiedByAnotherUnit(Vector2Int gridPosition, CombatUnit requester)
    {
        if (PlayerUnit != null && PlayerUnit != requester && PlayerUnit.IsAlive && PlayerUnit.GridPosition == gridPosition)
            return true;

        for (int i = 0; i < _enemyUnits.Count; i++)
        {
            EnemyUnit enemyUnit = _enemyUnits[i];
            if (enemyUnit != null && enemyUnit != requester && enemyUnit.IsAlive && enemyUnit.GridPosition == gridPosition)
                return true;
        }

        return false;
    }

    public EnemyUnit GetEnemyAt(Vector2Int gridPosition)
    {
        for (int i = 0; i < _enemyUnits.Count; i++)
        {
            EnemyUnit enemyUnit = _enemyUnits[i];
            if (enemyUnit != null && enemyUnit.IsAlive && enemyUnit.GridPosition == gridPosition)
                return enemyUnit;
        }

        return null;
    }

    public bool TryPlayerAttackGrid(Vector2Int targetGridPosition)
    {
        if (PlayerUnit == null || !PlayerUnit.IsAlive)
            return false;

        EnemyUnit targetEnemy = GetEnemyAt(targetGridPosition);
        if (targetEnemy == null)
            return false;

        if (!IsStraightLineTargetInRange(PlayerUnit.GridPosition, targetEnemy.GridPosition, PlayerUnit.Stats.Range))
            return false;

        int dealtDamage = PlayerUnit.GetAttackDamage();
        Debug.Log($"{PlayerUnit.DisplayName} attacked {targetEnemy.DisplayName} for {dealtDamage} rolled damage.");
        targetEnemy.ReceiveDamage(dealtDamage);
        return true;
    }

    public void ExecuteEnemyTurn(EnemyUnit enemyUnit)
    {
        if (PlayerUnit == null || !PlayerUnit.IsAlive || enemyUnit == null || !enemyUnit.IsAlive)
            return;

        Debug.Log($"{enemyUnit.DisplayName} started its turn.");

        for (int stepIndex = 0; stepIndex < enemyUnit.Stats.Speed; stepIndex++)
        {
            if (IsStraightLineTargetInRange(enemyUnit.GridPosition, PlayerUnit.GridPosition, enemyUnit.Stats.Range))
                break;

            Vector2Int nextStep = GetStepTowardTarget(enemyUnit.GridPosition, PlayerUnit.GridPosition);
            if (nextStep == Vector2Int.zero || !enemyUnit.TryMoveOneStep(nextStep))
                break;
        }

        if (IsStraightLineTargetInRange(enemyUnit.GridPosition, PlayerUnit.GridPosition, enemyUnit.Stats.Range))
        {
            int dealtDamage = enemyUnit.GetAttackDamage();
            Debug.Log($"{enemyUnit.DisplayName} attacked {PlayerUnit.DisplayName} for {dealtDamage} rolled damage.");
            PlayerUnit.ReceiveDamage(dealtDamage);
        }
    }

    public List<CombatUnit> GetLivingUnits()
    {
        List<CombatUnit> livingUnits = new List<CombatUnit>();

        if (PlayerUnit != null && PlayerUnit.IsAlive)
            livingUnits.Add(PlayerUnit);

        for (int i = 0; i < _enemyUnits.Count; i++)
        {
            EnemyUnit enemyUnit = _enemyUnits[i];
            if (enemyUnit != null && enemyUnit.IsAlive)
                livingUnits.Add(enemyUnit);
        }

        return livingUnits;
    }

    public bool IsStraightLineTargetInRange(Vector2Int origin, Vector2Int target, int range)
    {
        bool sameColumn = origin.x == target.x;
        bool sameRow = origin.y == target.y;
        if (!sameColumn && !sameRow)
            return false;

        int manhattanDistance = Mathf.Abs(origin.x - target.x) + Mathf.Abs(origin.y - target.y);
        return manhattanDistance <= range;
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
        if (!GridManager.I.IsWalkable(targetGridPosition))
            return false;

        if (PlayerUnit != null && PlayerUnit.IsAlive && PlayerUnit.GridPosition == targetGridPosition)
            return false;

        return true;
    }
}
