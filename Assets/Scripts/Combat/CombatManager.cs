using System.Collections.Generic;
using System.Collections;
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

    [Header("Player Defeat")]
    // Defeat UI is enabled when the player dies and stays open until restart.
    [SerializeField] GameObject playerDefeatRoot;
    [SerializeField] Button playAgainButton;
    [SerializeField] float playAgainEnableDelay = 1f;

    [SerializeField] private AudioCue gameLossSFX;


    public PlayerUnit PlayerUnit { get; private set; }
    readonly List<EnemyUnit> _enemyUnits = new List<EnemyUnit>();
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

        SceneManager.LoadScene("FloorScene");
    }

    IEnumerator FillRoomClearAndReturnToFloorScene()
    {
        _isRoomClearTransitionRunning = true;

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
