using UnityEngine;
using System.Collections.Generic;

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

    const string StartFloorNodeId = "start";

    // Stores the room selected on the floor map so the next scene knows what to generate.
    public RoomTemplateSO SelectedRoomTemplate { get; private set; }
    public int SelectedRoomEnemyCount { get; private set; }
    public string CurrentFloorNodeId { get; private set; } = StartFloorNodeId;
    public string PendingFloorNodeId { get; private set; }
    public int SavedPlayerHealth { get; private set; } = -1;

    // Holds the concrete positions generated for the currently active room.
    public RoomConfig CurrentRoomConfig;
    readonly HashSet<string> _clearedFloorNodeIds = new HashSet<string>();



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

    public void SelectRoom(RoomTemplateSO roomTemplate, int enemyCountOverride = -1)
    {
        SelectedRoomTemplate = roomTemplate;
        SelectedRoomEnemyCount = enemyCountOverride >= 0 ? enemyCountOverride : roomTemplate.enemyCount;
    }

    public void PrepareRoomSelection(string floorNodeId, RoomTemplateSO roomTemplate, int enemyCountOverride = -1)
    {
        // PendingFloorNodeId is committed only after the room is actually cleared.
        PendingFloorNodeId = floorNodeId;
        SelectRoom(roomTemplate, enemyCountOverride);
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

    public void ResetRunState()
    {
        // Used by defeat/restart to bring the floor run back to its initial state.
        SelectedRoomTemplate = null;
        SelectedRoomEnemyCount = 0;
        CurrentFloorNodeId = StartFloorNodeId;
        PendingFloorNodeId = null;
        SavedPlayerHealth = -1;
        CurrentRoomConfig = null;
        _clearedFloorNodeIds.Clear();

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
}
