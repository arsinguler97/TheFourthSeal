using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public static CombatManager I { get; private set; }

    [SerializeField] GameObject roomClearFillRoot;
    [SerializeField] Image roomClearFillImage;
    [SerializeField] float roomClearFillDuration = 2f;

    public PlayerUnit PlayerUnit { get; private set; }
    readonly List<EnemyUnit> _enemyUnits = new List<EnemyUnit>();
    public IReadOnlyList<EnemyUnit> EnemyUnits => _enemyUnits;
    bool _isRoomClearTransitionRunning;

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
    }

    void Update()
    {
        if (_isRoomClearTransitionRunning || _enemyUnits.Count == 0 || HasLivingEnemies())
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

    public bool IsStraightLineTargetInRange(Vector2Int origin, Vector2Int target, int range)
    {
        bool sameColumn = origin.x == target.x;
        bool sameRow = origin.y == target.y;
        if (!sameColumn && !sameRow)
            return false;

        int manhattanDistance = Mathf.Abs(origin.x - target.x) + Mathf.Abs(origin.y - target.y);
        return manhattanDistance <= range;
    }

    public void BeginReturnToFloorSceneTransition()
    {
        if (_isRoomClearTransitionRunning)
            return;

        Debug.Log($"CombatManager.BeginReturnToFloorSceneTransition -> pending floor node '{(RunManager.I != null ? RunManager.I.PendingFloorNodeId : "null")}', current floor node '{(RunManager.I != null ? RunManager.I.CurrentFloorNodeId : "null")}'.");
        StartCoroutine(FillRoomClearAndReturnToFloorScene());
    }

    IEnumerator FillRoomClearAndReturnToFloorScene()
    {
        _isRoomClearTransitionRunning = true;

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
}
