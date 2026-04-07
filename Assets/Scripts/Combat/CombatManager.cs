using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager I { get; private set; }

    // Room clear uses a simple fill overlay before returning to the floor map.
    [SerializeField] GameObject roomClearFillRoot;
    [SerializeField] Image roomClearFillImage;
    [SerializeField] float roomClearFillDuration = 2f;

    [SerializeField] private AudioCue roomClearSFX;
    [SerializeField] int lavaDamage = 1;

    [Header("Player Defeat")]
    // Defeat UI is enabled when the player dies and stays open until restart.
    [SerializeField] GameObject playerDefeatRoot;
    [SerializeField] Button playAgainButton;
    [SerializeField] float playAgainEnableDelay = 1f;

    [SerializeField] private AudioCue gameLossSFX;


    public PlayerUnit PlayerUnit { get; private set; }
    readonly List<EnemyUnit> _enemyUnits = new List<EnemyUnit>();
    private EnemyUnit _currentTarget;
    Vector2Int _pendingPlayerAttackTargetGrid;
    public IReadOnlyList<EnemyUnit> EnemyUnits => _enemyUnits;
    bool _isRoomClearTransitionRunning;
    bool _isPlayerDefeatSequenceRunning;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (roomClearFillRoot != null)
            roomClearFillRoot.SetActive(false);

        if (roomClearFillImage != null)
        {
            roomClearFillImage.fillAmount = 0f;
            roomClearFillImage.raycastTarget = false;
        }

        if (playerDefeatRoot != null)
            playerDefeatRoot.SetActive(false);

        if (playAgainButton != null)
            playAgainButton.interactable = false;
    }

    void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    void Update()
    {
        // Auto-exit the room once all enemies are dead, unless another transition is already running.
        if (_isRoomClearTransitionRunning || _isPlayerDefeatSequenceRunning || _enemyUnits.Count == 0 || HasLivingEnemies())
            return;

        BeginReturnToFloorSceneTransition();
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

        // Player attacks are grid-based and only work on a living enemy in straight-line range.
        _currentTarget = GetEnemyAt(targetGridPosition);
        if (_currentTarget == null)
            return false;

        if (!IsStraightLineTargetInRange(PlayerUnit.GridPosition, _currentTarget.GridPosition, PlayerUnit.Stats.Range))
            return false;

        _pendingPlayerAttackTargetGrid = targetGridPosition;


        DiceManager.Instance.OnDiceRollCompleted += DealDamageOfficially;
        PlayerUnit.ShowDice();
        DiceManager.Instance.RollDice(PlayerUnit.AttackDieSize, PlayerUnit.DiceCanvas);

        return true;
    }

    private void DealDamageOfficially(int amount)
    {
        PlayerUnit.HideDice();
        DiceManager.Instance.OnDiceRollCompleted -= DealDamageOfficially;

        if (_currentTarget == null)
            return;

        if (PlayerUnit != null && PlayerUnit.IsUsingRangedWeapon())
        {
            ResolvePlayerRangedAttack(amount);
            return;
        }

        if (_currentTarget)
        {
            int finalDamageBeforeDefence = amount + PlayerUnit.Stats.Strength;
            _currentTarget.ReceiveAttackRoll(PlayerUnit, amount);
            Debug.Log($"{PlayerUnit.DisplayName} attacked {_currentTarget.DisplayName} with a roll of {amount} and {finalDamageBeforeDefence} raw damage before defence.");
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

    public bool HasLivingEnemies()
    {
        for (int i = 0; i < _enemyUnits.Count; i++)
        {
            EnemyUnit enemyUnit = _enemyUnits[i];
            if (enemyUnit != null && enemyUnit.IsAlive)
                return true;
        }

        return false;
    }

    public void ApplyLavaDamageIfNeeded(CombatUnit unit)
    {
        if (unit == null || !unit.IsAlive || GridManager.I == null)
            return;

        if (!GridManager.I.IsLavaTile(unit.GridPosition))
            return;

        unit.ReceiveDamage(Mathf.Max(1, lavaDamage));
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

    public void ResolveEnemyAttack(EnemyUnit attacker, CombatUnit target, int attackRoll, Action onResolved)
    {
        if (attacker == null || target == null)
        {
            onResolved?.Invoke();
            return;
        }

        if (attacker.AttackStyle != EnemyAttackStyle.Ranged || attacker.ProjectilePrefab == null)
        {
            target.ReceiveAttackRoll(attacker, attackRoll);
            onResolved?.Invoke();
            return;
        }

        Vector2Int direction = GetAttackDirection(attacker.GridPosition, target.GridPosition);
        ProjectileResolution resolution = ResolveProjectilePath(attacker.GridPosition, direction, GetLineDistance(attacker.GridPosition, target.GridPosition));
        SpawnProjectileVisual(
            attacker.ProjectilePrefab,
            attacker.transform.position,
            resolution.impactWorldPosition,
            attacker.GetComponentInChildren<SpriteRenderer>(),
            () =>
            {
                if (resolution.hitPlayer && PlayerUnit != null && PlayerUnit.IsAlive)
                    PlayerUnit.ReceiveAttackRoll(attacker, attackRoll);

                onResolved?.Invoke();
            });
    }

    void ResolvePlayerRangedAttack(int attackRoll)
    {
        GameObject projectilePrefab = PlayerUnit != null ? PlayerUnit.GetEquippedProjectilePrefab() : null;
        if (projectilePrefab == null)
        {
            int finalDamageBeforeDefence = attackRoll + PlayerUnit.Stats.Strength;
            _currentTarget.ReceiveAttackRoll(PlayerUnit, attackRoll);
            Debug.Log($"{PlayerUnit.DisplayName} attacked {_currentTarget.DisplayName} with a roll of {attackRoll} and {finalDamageBeforeDefence} raw damage before defence.");
            return;
        }

        Vector2Int direction = GetAttackDirection(PlayerUnit.GridPosition, _pendingPlayerAttackTargetGrid);
        ProjectileResolution resolution = ResolveProjectilePath(PlayerUnit.GridPosition, direction, GetLineDistance(PlayerUnit.GridPosition, _pendingPlayerAttackTargetGrid));
        SpawnProjectileVisual(
            projectilePrefab,
            PlayerUnit.transform.position,
            resolution.impactWorldPosition,
            PlayerUnit.GetComponentInChildren<SpriteRenderer>(),
            () =>
            {
                if (resolution.hitEnemy != null && resolution.hitEnemy.IsAlive)
                {
                    int finalDamageBeforeDefence = attackRoll + PlayerUnit.Stats.Strength;
                    resolution.hitEnemy.ReceiveAttackRoll(PlayerUnit, attackRoll);
                    Debug.Log($"{PlayerUnit.DisplayName} attacked {resolution.hitEnemy.DisplayName} with a roll of {attackRoll} and {finalDamageBeforeDefence} raw damage before defence.");
                }
            });
    }

    ProjectileResolution ResolveProjectilePath(Vector2Int origin, Vector2Int direction, int maxDistance)
    {
        ProjectileResolution resolution = new ProjectileResolution
        {
            impactWorldPosition = GridManager.I != null ? GridManager.I.GridToWorld(origin) : Vector3.zero
        };

        if (GridManager.I == null || direction == Vector2Int.zero || maxDistance <= 0)
            return resolution;

        Vector2Int currentGridPosition = origin;
        for (int step = 1; step <= maxDistance; step++)
        {
            currentGridPosition += direction;

            if (!GridManager.I.InBounds(currentGridPosition))
                break;

            resolution.impactWorldPosition = GridManager.I.GridToWorld(currentGridPosition);

            if (GridManager.I.IsTileBlocked(currentGridPosition))
                break;

            EnemyUnit enemy = GetEnemyAt(currentGridPosition);
            if (enemy != null)
            {
                resolution.hitEnemy = enemy;
                break;
            }

            if (PlayerUnit != null && PlayerUnit.IsAlive && PlayerUnit.GridPosition == currentGridPosition)
            {
                resolution.hitPlayer = true;
                break;
            }
        }

        return resolution;
    }

    void SpawnProjectileVisual(GameObject projectilePrefab, Vector3 startWorldPosition, Vector3 endWorldPosition, SpriteRenderer sourceRenderer, Action onArrived)
    {
        if (projectilePrefab == null)
        {
            onArrived?.Invoke();
            return;
        }

        GameObject projectileInstance = Instantiate(projectilePrefab, startWorldPosition, Quaternion.identity);
        MatchProjectileSorting(projectileInstance, sourceRenderer);

        ProjectileVisual projectileVisual = projectileInstance.GetComponent<ProjectileVisual>();
        if (projectileVisual == null)
            projectileVisual = projectileInstance.AddComponent<ProjectileVisual>();

        projectileVisual.Initialize(startWorldPosition, endWorldPosition, onArrived);
    }

    void MatchProjectileSorting(GameObject projectileInstance, SpriteRenderer sourceRenderer)
    {
        if (projectileInstance == null || sourceRenderer == null)
            return;

        ParticleSystemRenderer[] particleRenderers = projectileInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            particleRenderers[i].sortingLayerID = sourceRenderer.sortingLayerID;
            particleRenderers[i].sortingOrder = sourceRenderer.sortingOrder + 1;
        }

        SpriteRenderer[] spriteRenderers = projectileInstance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sortingLayerID = sourceRenderer.sortingLayerID;
            spriteRenderers[i].sortingOrder = sourceRenderer.sortingOrder + 1;
        }
    }

    Vector2Int GetAttackDirection(Vector2Int origin, Vector2Int target)
    {
        if (origin.x == target.x)
            return target.y > origin.y ? Vector2Int.up : Vector2Int.down;

        if (origin.y == target.y)
            return target.x > origin.x ? Vector2Int.right : Vector2Int.left;

        return Vector2Int.zero;
    }

    int GetLineDistance(Vector2Int origin, Vector2Int target)
    {
        return Mathf.Abs(origin.x - target.x) + Mathf.Abs(origin.y - target.y);
    }

    struct ProjectileResolution
    {
        public EnemyUnit hitEnemy;
        public bool hitPlayer;
        public Vector3 impactWorldPosition;
    }

    public void BeginReturnToFloorSceneTransition()
    {
        if (_isRoomClearTransitionRunning || _isPlayerDefeatSequenceRunning)
            return;

        StartCoroutine(FillRoomClearAndReturnToFloorScene());
        AudioManager.Instance.PlaySound(roomClearSFX);
    }

    public void HandlePlayerDeath(PlayerUnit playerUnit)
    {
        if (_isPlayerDefeatSequenceRunning)
            return;

        AudioManager.Instance.PauseMusic();
        AudioManager.Instance.PlaySound(gameLossSFX);
        StartCoroutine(ShowPlayerDefeatSequence(playerUnit));
    }

    public void RestartRunFromDefeat()
    {
        if (!_isPlayerDefeatSequenceRunning)
            return;

        if (RunManager.I != null)
            RunManager.I.ResetRunState();

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.ResetEquipmentState();

        SceneManager.LoadScene("FloorScene");
    }

    IEnumerator FillRoomClearAndReturnToFloorScene()
    {
        _isRoomClearTransitionRunning = true;

        if (RunManager.I != null && PlayerUnit != null)
            RunManager.I.SavePlayerHealth(PlayerUnit.CurrentHealth);

        TryGrantRoomRewardBeforeLeaving();

        // Advancing floor state happens before the scene swap so the next FloorScene can resolve the new node.
        if (RunManager.I != null)
            RunManager.I.MarkPendingRoomClearedAndAdvanceFloorPosition();

        if (roomClearFillImage == null)
        {
            SceneManager.LoadScene("FloorScene");
            yield break;
        }

        if (roomClearFillRoot != null)
            roomClearFillRoot.SetActive(true);
        else
            roomClearFillImage.gameObject.SetActive(true);

        roomClearFillImage.raycastTarget = false;
        roomClearFillImage.fillAmount = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < roomClearFillDuration)
        {
            elapsedTime += Time.deltaTime;
            roomClearFillImage.fillAmount = Mathf.Clamp01(elapsedTime / roomClearFillDuration);
            yield return null;
        }

        roomClearFillImage.fillAmount = 1f;
        SceneManager.LoadScene("FloorScene");
    }

    void TryGrantRoomRewardBeforeLeaving()
    {
        if (RunManager.I == null || EquipmentManager.Instance == null)
            return;

        RoomConfig activeRoomConfig = RunManager.I.CurrentRoomConfig;
        if (activeRoomConfig == null || activeRoomConfig.hasGrantedExitReward)
            return;

        bool shouldGrantReward = activeRoomConfig.isRewardOpened || !HasLivingEnemies();
        if (!shouldGrantReward)
            return;

        if (EquipmentManager.Instance.TryGrantRandomReward())
            activeRoomConfig.hasGrantedExitReward = true;
    }

    IEnumerator ShowPlayerDefeatSequence(PlayerUnit playerUnit)
    {
        _isPlayerDefeatSequenceRunning = true;

        // Stop the turn loop first so no more AI or turn changes happen under the defeat UI.
        if (TurnManager.I != null)
            TurnManager.I.StopCombatFlow();

        if (playerDefeatRoot != null)
            playerDefeatRoot.SetActive(true);

        if (playAgainButton != null)
            playAgainButton.interactable = false;

        if (playAgainEnableDelay > 0f)
            yield return new WaitForSeconds(playAgainEnableDelay);

        if (playAgainButton != null)
            playAgainButton.interactable = true;
    }
}
