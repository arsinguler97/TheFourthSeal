using UnityEngine;
using System.Collections;

public class EnemyUnit : CombatUnit
{
    [SerializeField] private AudioCue footstepSFX;
    [SerializeField] private AudioCue spawnSFX;

    [Header("Enemy General")]
    [SerializeField] float moveSpeedUnitsPerSecond = 8f;
    [SerializeField] EnemyDefinitionSO enemyDefinition;
    [SerializeField] SpriteRenderer visualSpriteRenderer;


    Vector2Int _currentGridPosition;
    EnemyAIController _enemyAI;

    public override Vector2Int GridPosition => _currentGridPosition;
    public EnemyAIController EnemyAI => _enemyAI;
    public EnemyDefinitionSO EnemyDefinition => enemyDefinition;

    protected override void Awake()
    {
        ApplyDefinitionData();

        base.Awake();

        if (visualSpriteRenderer == null)
            visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualSpriteRenderer != null && enemyDefinition != null && enemyDefinition.worldSprite != null)
            visualSpriteRenderer.sprite = enemyDefinition.worldSprite;

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

    public void ConfigureFromDefinition(EnemyDefinitionSO definition)
    {
        enemyDefinition = definition;
        ApplyDefinitionData();

        if (isActiveAndEnabled)
            ResetBaseStats(enemyDefinition != null && enemyDefinition.baseStats != null
                ? enemyDefinition.baseStats.ToStatBlockData()
                : baseStats);

        if (visualSpriteRenderer == null)
            visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualSpriteRenderer != null && enemyDefinition != null && enemyDefinition.worldSprite != null)
            visualSpriteRenderer.sprite = enemyDefinition.worldSprite;
    }

    public override void ReceiveAttackRoll(CombatUnit attacker, int attackRoll)
    {
        bool isPlayerAttack = attacker is PlayerUnit;

        if (isPlayerAttack
            && enemyDefinition != null
            && enemyDefinition.immuneToOddDamage
            && Mathf.Abs(attackRoll) % 2 == 1)
        {
            PlayMissedHitVfx();
            Debug.Log($"{DisplayName} ignored odd player attack roll {attackRoll}.");
            return;
        }

        if (isPlayerAttack
            && enemyDefinition != null
            && enemyDefinition.immuneToDamageAtOrBelow > 0
            && attackRoll < enemyDefinition.immuneToDamageAtOrBelow)
        {
            PlayMissedHitVfx();
            Debug.Log($"{DisplayName} ignored player attack roll {attackRoll} because it is below {enemyDefinition.immuneToDamageAtOrBelow}.");
            return;
        }

        base.ReceiveAttackRoll(attacker, attackRoll);
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
        //if (!GridMovementUtility.IsSingleCardinalStep(step) || GridManager.I == null)
        //    yield break;

        Vector2Int requestedGridPosition = step; //_currentGridPosition + 
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

        if (CombatManager.I != null)
            CombatManager.I.ApplyLavaDamageIfNeeded(this);
    }

    public override Sprite GetTurnOrderSprite()
    {
        if (enemyDefinition != null && enemyDefinition.turnOrderIcon != null)
            return enemyDefinition.turnOrderIcon;

        return base.GetTurnOrderSprite();
    }

    void ApplyDefinitionData()
    {
        if (enemyDefinition == null)
            return;

        displayName = string.IsNullOrWhiteSpace(enemyDefinition.displayName) ? displayName : enemyDefinition.displayName;
        baseStats = enemyDefinition.baseStats != null
            ? enemyDefinition.baseStats.ToStatBlockData()
            : baseStats;
    }

    void PlayMissedHitVfx()
    {
        if (enemyDefinition == null || enemyDefinition.missedHitVfxPrefab == null)
            return;

        GameObject missedVfxInstance = Instantiate(
            enemyDefinition.missedHitVfxPrefab,
            transform.position + enemyDefinition.missedHitVfxOffset,
            Quaternion.identity,
            transform);

        missedVfxInstance.transform.localPosition = enemyDefinition.missedHitVfxOffset;
        MatchMissedVfxSorting(missedVfxInstance);
    }

    void MatchMissedVfxSorting(GameObject missedVfxInstance)
    {
        if (missedVfxInstance == null || visualSpriteRenderer == null)
            return;

        ParticleSystemRenderer[] particleRenderers = missedVfxInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            particleRenderers[i].sortingLayerID = visualSpriteRenderer.sortingLayerID;
            particleRenderers[i].sortingOrder = visualSpriteRenderer.sortingOrder + 1;
        }

        SpriteRenderer[] spriteRenderers = missedVfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sortingLayerID = visualSpriteRenderer.sortingLayerID;
            spriteRenderers[i].sortingOrder = visualSpriteRenderer.sortingOrder + 1;
        }
    }
}
