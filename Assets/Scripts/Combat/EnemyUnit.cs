using UnityEngine;
using System.Collections;

public class EnemyUnit : CombatUnit
{
    [SerializeField] private AudioCue footstepSFX;
    [SerializeField] private AudioCue spawnSFX;

    [Header("Enemy General")]
    [SerializeField] float moveSpeedUnitsPerSecond = 8f;
    [SerializeField] EnemyDefinitionSO enemyDefinition;


    Vector2Int _currentGridPosition;
    EnemyAIController _enemyAI;

    public override Vector2Int GridPosition => _currentGridPosition;
    public EnemyAIController EnemyAI => _enemyAI;
    public EnemyDefinitionSO EnemyDefinition => enemyDefinition;

    protected override void Awake()
    {
        if (enemyDefinition != null)
        {
            displayName = string.IsNullOrWhiteSpace(enemyDefinition.displayName) ? displayName : enemyDefinition.displayName;
            baseStats = enemyDefinition.baseStats != null
                ? enemyDefinition.baseStats.ToStatBlockData()
                : baseStats;
        }

        base.Awake();

        _enemyAI = GetComponent<EnemyAIController>();
        if (_enemyAI == null)
            _enemyAI = gameObject.AddComponent<EnemyAIController>();

        if (CombatManager.I != null)
            CombatManager.I.RegisterEnemy(this);

        AudioManager.Instance.PlaySound(spawnSFX);
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
        if (!GridMovementUtility.IsSingleCardinalStep(step))
            return false;

        Vector2Int requestedGridPosition = _currentGridPosition + step;
        if (!GridMovementUtility.CanUnitEnterTile(requestedGridPosition, this))
            return false;

        SetGridPosition(requestedGridPosition);
        return true;
    }

    public IEnumerator MoveOneStepAnimated(Vector2Int step)
    {
        if (!GridMovementUtility.IsSingleCardinalStep(step) || GridManager.I == null)
            yield break;

        Vector2Int requestedGridPosition = _currentGridPosition + step;
        if (!GridMovementUtility.CanUnitEnterTile(requestedGridPosition, this))
            yield break;

        AudioManager.Instance.PlaySound(footstepSFX);
        _currentGridPosition = requestedGridPosition;

        Vector3 destinationWorldPosition = GridManager.I.GridToWorld(requestedGridPosition);
        while (Vector3.Distance(transform.position, destinationWorldPosition) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destinationWorldPosition,
                moveSpeedUnitsPerSecond * Time.deltaTime);
            yield return null;
        }

        transform.position = destinationWorldPosition;
    }

    public override Sprite GetTurnOrderSprite()
    {
        if (enemyDefinition != null && enemyDefinition.turnOrderIcon != null)
            return enemyDefinition.turnOrderIcon;

        return base.GetTurnOrderSprite();
    }
}
