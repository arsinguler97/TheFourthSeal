using UnityEngine;
using System.Collections.Generic;

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

    const string StartFloorNodeId = "start";
    const string DefaultFloorSceneName = "FloorScene";

    // Stores the room selected on the floor map so the next scene knows what to generate.
    public RoomTemplateSO SelectedRoomTemplate { get; private set; }
    public int SelectedRoomEnemyCount { get; private set; }
    public IReadOnlyList<EnemyDefinitionSO> SelectedEnemyOverrides => _selectedEnemyOverrides;
    public string CurrentFloorNodeId { get; private set; } = StartFloorNodeId;
    public string PendingFloorNodeId { get; private set; }
    public string CurrentFloorSceneName { get; private set; } = DefaultFloorSceneName;
    public int SavedPlayerHealth { get; private set; } = -1;
    public bool HasFloorKey { get; private set; }

    // Holds the concrete positions generated for the currently active room.
    public RoomConfig CurrentRoomConfig;
    readonly HashSet<string> _clearedFloorNodeIds = new HashSet<string>();
    readonly List<EnemyDefinitionSO> _selectedEnemyOverrides = new List<EnemyDefinitionSO>();



    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectRoom(RoomTemplateSO roomTemplate, int enemyCountOverride = -1, IReadOnlyList<EnemyDefinitionSO> enemyOverrides = null)
    {
        SelectedRoomTemplate = roomTemplate;
        SelectedRoomEnemyCount = enemyCountOverride >= 0 ? enemyCountOverride : roomTemplate.enemyCount;
        _selectedEnemyOverrides.Clear();

        if (enemyOverrides != null)
        {
            for (int i = 0; i < enemyOverrides.Count; i++)
            {
                if (enemyOverrides[i] != null)
                    _selectedEnemyOverrides.Add(enemyOverrides[i]);
            }
        }
    }

    public void PrepareRoomSelection(string floorNodeId, RoomTemplateSO roomTemplate, int enemyCountOverride = -1, IReadOnlyList<EnemyDefinitionSO> enemyOverrides = null)
    {
        // PendingFloorNodeId is committed only after the room is actually cleared.
        PendingFloorNodeId = floorNodeId;
        SelectRoom(roomTemplate, enemyCountOverride, enemyOverrides);
    }

    public void MarkPendingRoomClearedAndAdvanceFloorPosition()
    {
        if (string.IsNullOrEmpty(PendingFloorNodeId))
            return;

        _clearedFloorNodeIds.Add(PendingFloorNodeId);
        CurrentFloorNodeId = PendingFloorNodeId;
        PendingFloorNodeId = null;
    }

    public bool IsFloorNodeCleared(string floorNodeId)
    {
        return !string.IsNullOrEmpty(floorNodeId) && _clearedFloorNodeIds.Contains(floorNodeId);
    }

    public void SetCurrentFloorNodePosition(string floorNodeId)
    {
        if (string.IsNullOrWhiteSpace(floorNodeId))
            return;

        CurrentFloorNodeId = floorNodeId;
    }

    public void ResetRunState()
    {
        // Used by defeat/restart to bring the floor run back to its initial state.
        SelectedRoomTemplate = null;
        SelectedRoomEnemyCount = 0;
        _selectedEnemyOverrides.Clear();
        CurrentFloorNodeId = StartFloorNodeId;
        PendingFloorNodeId = null;
        CurrentFloorSceneName = DefaultFloorSceneName;
        SavedPlayerHealth = -1;
        HasFloorKey = false;
        CurrentRoomConfig = null;
        _clearedFloorNodeIds.Clear();

        PlayerWallet.I.ResetWallet();

        AudioManager.Instance.UnPauseMusic();
    }

    public void SavePlayerHealth(int currentHealth)
    {
        SavedPlayerHealth = Mathf.Max(0, currentHealth);
    }

    public int GetPlayerHealthForNextRoom(int maxHealth)
    {
        if (SavedPlayerHealth < 0)
            return Mathf.Max(1, maxHealth);

        return Mathf.Clamp(SavedPlayerHealth + 2, 1, Mathf.Max(1, maxHealth));
    }

    public void BeginNextFloor()
    {
        BeginNextFloor(CurrentFloorSceneName);
    }

    public void BeginNextFloor(string floorSceneName)
    {
        SelectedRoomTemplate = null;
        SelectedRoomEnemyCount = 0;
        _selectedEnemyOverrides.Clear();
        CurrentFloorNodeId = StartFloorNodeId;
        PendingFloorNodeId = null;
        if (!string.IsNullOrWhiteSpace(floorSceneName))
            CurrentFloorSceneName = floorSceneName;
        HasFloorKey = false;
        CurrentRoomConfig = null;
        _clearedFloorNodeIds.Clear();
    }

    public string GetCurrentFloorSceneName()
    {
        return string.IsNullOrWhiteSpace(CurrentFloorSceneName)
            ? DefaultFloorSceneName
            : CurrentFloorSceneName;
    }

    public void AcquireFloorKey()
    {
        HasFloorKey = true;
    }

    public void ClearFloorKey()
    {
        HasFloorKey = false;
    }
}
