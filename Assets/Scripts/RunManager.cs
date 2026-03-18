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
        Debug.Log($"RunManager.PrepareRoomSelection -> pending floor node '{floorNodeId}'.");
        PendingFloorNodeId = floorNodeId;
        SelectRoom(roomTemplate, enemyCountOverride);
    }

    public void MarkPendingRoomClearedAndAdvanceFloorPosition()
    {
        if (string.IsNullOrEmpty(PendingFloorNodeId))
        {
            Debug.LogWarning($"RunManager.MarkPendingRoomClearedAndAdvanceFloorPosition skipped because PendingFloorNodeId is empty. CurrentFloorNodeId remains '{CurrentFloorNodeId}'.");
            return;
        }

        _clearedFloorNodeIds.Add(PendingFloorNodeId);
        CurrentFloorNodeId = PendingFloorNodeId;
        Debug.Log($"RunManager.MarkPendingRoomClearedAndAdvanceFloorPosition -> advanced to '{CurrentFloorNodeId}'.");
        PendingFloorNodeId = null;
    }

    public bool IsFloorNodeCleared(string floorNodeId)
    {
        return !string.IsNullOrEmpty(floorNodeId) && _clearedFloorNodeIds.Contains(floorNodeId);
    }
}
