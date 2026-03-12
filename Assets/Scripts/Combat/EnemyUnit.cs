using UnityEngine;

public class EnemyUnit : CombatUnit
{
    Vector2Int _currentGridPosition;

    public override Vector2Int GridPosition => _currentGridPosition;

    protected override void Awake()
    {
        base.Awake();

        if (CombatManager.I != null)
            CombatManager.I.RegisterEnemy(this);
    }

    void Start()
    {
        transform.position = GridManager.I.GridToWorld(_currentGridPosition);
    }

    public void SetGridPosition(Vector2Int gridPosition)
    {
        _currentGridPosition = gridPosition;

        if (GridManager.I != null)
            transform.position = GridManager.I.GridToWorld(gridPosition);
    }

    public bool TryMoveOneStep(Vector2Int step)
    {
        if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1)
            return false;

        Vector2Int requestedGridPosition = _currentGridPosition + step;
        if (!GridManager.I.IsWalkable(requestedGridPosition))
            return false;

        if (CombatManager.I != null && CombatManager.I.IsTileOccupiedByAnotherUnit(requestedGridPosition, this))
            return false;

        SetGridPosition(requestedGridPosition);
        return true;
    }
}
