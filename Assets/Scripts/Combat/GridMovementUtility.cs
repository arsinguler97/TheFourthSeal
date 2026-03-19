using UnityEngine;

public static class GridMovementUtility
{
    public static bool IsSingleCardinalStep(Vector2Int step)
    {
        return Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;
    }

    public static bool CanUnitEnterTile(Vector2Int targetGridPosition, CombatUnit requester)
    {
        if (GridManager.I == null || !GridManager.I.IsWalkable(targetGridPosition))
            return false;

        return CombatManager.I == null || !CombatManager.I.IsTileOccupiedByAnotherUnit(targetGridPosition, requester);
    }

    public static bool CanEnemyApproachTile(Vector2Int targetGridPosition, EnemyUnit requester)
    {
        if (!CanUnitEnterTile(targetGridPosition, requester))
            return false;

        return CombatManager.I == null
            || CombatManager.I.PlayerUnit == null
            || !CombatManager.I.PlayerUnit.IsAlive
            || CombatManager.I.PlayerUnit.GridPosition != targetGridPosition;
    }
}
